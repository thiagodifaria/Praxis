using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Common;
using Praxis.Application.Models;
using Praxis.Application.Persistence;
using Praxis.Domain.Billing;
using Praxis.Domain.Operations;

namespace Praxis.Application.Services;

public sealed class FinancialService(
    IPraxisDbContext dbContext,
    ICurrentUserService currentUserService,
    IEventBus eventBus,
    IDashboardCache dashboardCache,
    PlatformPolicyService platformPolicyService)
{
    public async Task<IReadOnlyCollection<ReceivableResponse>> ListReceivablesAsync(Guid? branchId = null, Guid? costCenterId = null, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Billing, branchId, cancellationToken);

        var query = dbContext.Receivables
            .AsNoTracking()
            .Include(receivable => receivable.Settlements)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(receivable => receivable.BranchId == branchId);
        }

        if (costCenterId.HasValue)
        {
            query = query.Where(receivable => receivable.CostCenterId == costCenterId);
        }

        var receivables = await query
            .OrderBy(receivable => receivable.DueDateUtc)
            .ToListAsync(cancellationToken);

        return await MapReceivablesAsync(receivables, cancellationToken);
    }

    public async Task<ReceivableResponse> RegisterReceivableSettlementAsync(Guid id, FinancialSettlementRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("Settlement amount must be greater than zero.");
        }

        var paidAtUtc = request.PaidAtUtc ?? utcNow;
        var receivable = await dbContext.Receivables
            .Include(current => current.Settlements)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Receivable not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Billing, receivable.BranchId, cancellationToken);

        var invoice = await dbContext.Invoices.FirstAsync(current => current.Id == receivable.InvoiceId, cancellationToken);

        var actor = currentUserService.GetCurrentUser();
        var settlement = new ReceivableSettlement(receivable.Id, request.Amount, paidAtUtc, request.PaymentMethod, request.Notes, actor.UserId);
        settlement.SetCreatedAt(paidAtUtc);

        receivable.ApplySettlement(settlement, paidAtUtc);
        if (receivable.OutstandingAmount > 0m && receivable.DueDateUtc < paidAtUtc)
        {
            receivable.MarkOverdue(paidAtUtc);
        }

        if (receivable.Status == FinancialTitleStatus.Paid)
        {
            invoice.MarkPaid(paidAtUtc);
        }

        dbContext.AuditEntries.Add(new AuditEntry(
            "finance.receivable.settled",
            nameof(Receivable),
            receivable.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new
            {
                receivable.DocumentNumber,
                settlement.Amount,
                settlement.PaymentMethod,
                settlement.PaidAtUtc,
                receivable.OutstandingAmount
            })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        await eventBus.PublishAsync(
            "finance.receivable.settled",
            new
            {
                receivable.Id,
                receivable.DocumentNumber,
                receivable.BranchId,
                receivable.CostCenterId,
                settlement.Amount,
                receivable.OutstandingAmount,
                receivable.Status
            },
            cancellationToken);

        return await GetReceivableAsync(receivable.Id, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PayableResponse>> ListPayablesAsync(Guid? branchId = null, Guid? costCenterId = null, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Billing, branchId, cancellationToken);

        var query = dbContext.Payables
            .AsNoTracking()
            .Include(payable => payable.Settlements)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(payable => payable.BranchId == branchId);
        }

        if (costCenterId.HasValue)
        {
            query = query.Where(payable => payable.CostCenterId == costCenterId);
        }

        var payables = await query
            .OrderBy(payable => payable.DueDateUtc)
            .ToListAsync(cancellationToken);

        return await MapPayablesAsync(payables, cancellationToken);
    }

    public async Task<PayableResponse> RegisterPayableSettlementAsync(Guid id, FinancialSettlementRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new ValidationException("Settlement amount must be greater than zero.");
        }

        var paidAtUtc = request.PaidAtUtc ?? utcNow;
        var payable = await dbContext.Payables
            .Include(current => current.Settlements)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Payable not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Billing, payable.BranchId, cancellationToken);

        var actor = currentUserService.GetCurrentUser();
        var settlement = new PayableSettlement(payable.Id, request.Amount, paidAtUtc, request.PaymentMethod, request.Notes, actor.UserId);
        settlement.SetCreatedAt(paidAtUtc);

        payable.ApplySettlement(settlement, paidAtUtc);
        if (payable.OutstandingAmount > 0m && payable.DueDateUtc < paidAtUtc)
        {
            payable.MarkOverdue(paidAtUtc);
        }

        dbContext.AuditEntries.Add(new AuditEntry(
            "finance.payable.settled",
            nameof(Payable),
            payable.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new
            {
                payable.DocumentNumber,
                settlement.Amount,
                settlement.PaymentMethod,
                settlement.PaidAtUtc,
                payable.OutstandingAmount
            })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        await eventBus.PublishAsync(
            "finance.payable.settled",
            new
            {
                payable.Id,
                payable.DocumentNumber,
                payable.BranchId,
                payable.CostCenterId,
                settlement.Amount,
                payable.OutstandingAmount,
                payable.Status
            },
            cancellationToken);

        return await GetPayableAsync(payable.Id, cancellationToken);
    }

    private async Task<ReceivableResponse> GetReceivableAsync(Guid id, CancellationToken cancellationToken)
    {
        var receivable = await dbContext.Receivables
            .AsNoTracking()
            .Include(current => current.Settlements)
            .FirstAsync(current => current.Id == id, cancellationToken);

        return await MapReceivableAsync(receivable, cancellationToken);
    }

    private async Task<PayableResponse> GetPayableAsync(Guid id, CancellationToken cancellationToken)
    {
        var payable = await dbContext.Payables
            .AsNoTracking()
            .Include(current => current.Settlements)
            .FirstAsync(current => current.Id == id, cancellationToken);

        return await MapPayableAsync(payable, cancellationToken);
    }

    private async Task<IReadOnlyCollection<ReceivableResponse>> MapReceivablesAsync(IReadOnlyCollection<Receivable> receivables, CancellationToken cancellationToken)
    {
        var customerIds = receivables.Select(receivable => receivable.CustomerId).Distinct().ToArray();
        var invoiceIds = receivables.Select(receivable => receivable.InvoiceId).Distinct().ToArray();
        var branchIds = receivables.Where(receivable => receivable.BranchId.HasValue).Select(receivable => receivable.BranchId!.Value).Distinct().ToArray();
        var costCenterIds = receivables.Where(receivable => receivable.CostCenterId.HasValue).Select(receivable => receivable.CostCenterId!.Value).Distinct().ToArray();

        var customers = await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customerIds.Contains(customer.Id))
            .ToDictionaryAsync(customer => customer.Id, customer => customer.Name, cancellationToken);

        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoiceIds.Contains(invoice.Id))
            .ToDictionaryAsync(invoice => invoice.Id, invoice => invoice.InvoiceNumber, cancellationToken);

        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        var costCenters = await dbContext.CostCenters
            .AsNoTracking()
            .Where(costCenter => costCenterIds.Contains(costCenter.Id))
            .ToDictionaryAsync(costCenter => costCenter.Id, costCenter => costCenter.Name, cancellationToken);

        return receivables
            .Select(receivable => ToResponse(
                receivable,
                customers[receivable.CustomerId],
                invoices[receivable.InvoiceId],
                receivable.BranchId.HasValue && branches.TryGetValue(receivable.BranchId.Value, out var branchName) ? branchName : null,
                receivable.CostCenterId.HasValue && costCenters.TryGetValue(receivable.CostCenterId.Value, out var costCenterName) ? costCenterName : null))
            .ToArray();
    }

    private async Task<ReceivableResponse> MapReceivableAsync(Receivable receivable, CancellationToken cancellationToken)
    {
        var customerName = await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == receivable.CustomerId)
            .Select(customer => customer.Name)
            .FirstAsync(cancellationToken);

        var invoiceNumber = await dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => invoice.Id == receivable.InvoiceId)
            .Select(invoice => invoice.InvoiceNumber)
            .FirstAsync(cancellationToken);

        var branchName = receivable.BranchId.HasValue
            ? await dbContext.Branches
                .AsNoTracking()
                .Where(branch => branch.Id == receivable.BranchId.Value)
                .Select(branch => branch.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var costCenterName = receivable.CostCenterId.HasValue
            ? await dbContext.CostCenters
                .AsNoTracking()
                .Where(costCenter => costCenter.Id == receivable.CostCenterId.Value)
                .Select(costCenter => costCenter.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return ToResponse(receivable, customerName, invoiceNumber, branchName, costCenterName);
    }

    private static ReceivableResponse ToResponse(Receivable receivable, string customerName, string invoiceNumber, string? branchName, string? costCenterName)
    {
        var currentStatus = CalculateCurrentStatus(receivable.Status, receivable.DueDateUtc, receivable.OutstandingAmount);

        return new ReceivableResponse(
            receivable.Id,
            receivable.DocumentNumber,
            receivable.InvoiceId,
            invoiceNumber,
            receivable.CustomerId,
            customerName,
            receivable.BranchId,
            branchName,
            receivable.CostCenterId,
            costCenterName,
            receivable.Description,
            receivable.IssuedAtUtc,
            receivable.DueDateUtc,
            receivable.OriginalAmount,
            receivable.PaidAmount,
            receivable.OutstandingAmount,
            currentStatus,
            receivable.Settlements
                .OrderByDescending(settlement => settlement.PaidAtUtc)
                .Select(settlement => new ReceivableSettlementResponse(
                    settlement.Id,
                    settlement.Amount,
                    settlement.PaidAtUtc,
                    settlement.PaymentMethod,
                    settlement.Notes))
                .ToArray());
    }

    private async Task<IReadOnlyCollection<PayableResponse>> MapPayablesAsync(IReadOnlyCollection<Payable> payables, CancellationToken cancellationToken)
    {
        var supplierIds = payables.Select(payable => payable.SupplierId).Distinct().ToArray();
        var purchaseOrderIds = payables.Where(payable => payable.PurchaseOrderId.HasValue).Select(payable => payable.PurchaseOrderId!.Value).Distinct().ToArray();
        var purchaseReceiptIds = payables.Where(payable => payable.PurchaseReceiptId.HasValue).Select(payable => payable.PurchaseReceiptId!.Value).Distinct().ToArray();
        var branchIds = payables.Where(payable => payable.BranchId.HasValue).Select(payable => payable.BranchId!.Value).Distinct().ToArray();
        var costCenterIds = payables.Where(payable => payable.CostCenterId.HasValue).Select(payable => payable.CostCenterId!.Value).Distinct().ToArray();

        var suppliers = await dbContext.Suppliers
            .AsNoTracking()
            .Where(supplier => supplierIds.Contains(supplier.Id))
            .ToDictionaryAsync(supplier => supplier.Id, supplier => supplier.Name, cancellationToken);

        var orders = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Where(order => purchaseOrderIds.Contains(order.Id))
            .ToDictionaryAsync(order => order.Id, order => order.OrderNumber, cancellationToken);

        var receipts = await dbContext.PurchaseReceipts
            .AsNoTracking()
            .Where(receipt => purchaseReceiptIds.Contains(receipt.Id))
            .ToDictionaryAsync(receipt => receipt.Id, receipt => receipt.ReceiptNumber, cancellationToken);

        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        var costCenters = await dbContext.CostCenters
            .AsNoTracking()
            .Where(costCenter => costCenterIds.Contains(costCenter.Id))
            .ToDictionaryAsync(costCenter => costCenter.Id, costCenter => costCenter.Name, cancellationToken);

        return payables
            .Select(payable => ToResponse(
                payable,
                suppliers[payable.SupplierId],
                payable.PurchaseOrderId.HasValue && orders.TryGetValue(payable.PurchaseOrderId.Value, out var orderNumber) ? orderNumber : null,
                payable.PurchaseReceiptId.HasValue && receipts.TryGetValue(payable.PurchaseReceiptId.Value, out var receiptNumber) ? receiptNumber : null,
                payable.BranchId.HasValue && branches.TryGetValue(payable.BranchId.Value, out var branchName) ? branchName : null,
                payable.CostCenterId.HasValue && costCenters.TryGetValue(payable.CostCenterId.Value, out var costCenterName) ? costCenterName : null))
            .ToArray();
    }

    private async Task<PayableResponse> MapPayableAsync(Payable payable, CancellationToken cancellationToken)
    {
        var supplierName = await dbContext.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.Id == payable.SupplierId)
            .Select(supplier => supplier.Name)
            .FirstAsync(cancellationToken);

        var purchaseOrderNumber = payable.PurchaseOrderId.HasValue
            ? await dbContext.PurchaseOrders
                .AsNoTracking()
                .Where(order => order.Id == payable.PurchaseOrderId.Value)
                .Select(order => order.OrderNumber)
                .FirstAsync(cancellationToken)
            : null;

        var purchaseReceiptNumber = payable.PurchaseReceiptId.HasValue
            ? await dbContext.PurchaseReceipts
                .AsNoTracking()
                .Where(receipt => receipt.Id == payable.PurchaseReceiptId.Value)
                .Select(receipt => receipt.ReceiptNumber)
                .FirstAsync(cancellationToken)
            : null;

        var branchName = payable.BranchId.HasValue
            ? await dbContext.Branches
                .AsNoTracking()
                .Where(branch => branch.Id == payable.BranchId.Value)
                .Select(branch => branch.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var costCenterName = payable.CostCenterId.HasValue
            ? await dbContext.CostCenters
                .AsNoTracking()
                .Where(costCenter => costCenter.Id == payable.CostCenterId.Value)
                .Select(costCenter => costCenter.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return ToResponse(payable, supplierName, purchaseOrderNumber, purchaseReceiptNumber, branchName, costCenterName);
    }

    private static PayableResponse ToResponse(
        Payable payable,
        string supplierName,
        string? purchaseOrderNumber,
        string? purchaseReceiptNumber,
        string? branchName,
        string? costCenterName)
    {
        var currentStatus = CalculateCurrentStatus(payable.Status, payable.DueDateUtc, payable.OutstandingAmount);

        return new PayableResponse(
            payable.Id,
            payable.DocumentNumber,
            payable.PurchaseOrderId,
            purchaseOrderNumber,
            payable.PurchaseReceiptId,
            purchaseReceiptNumber,
            payable.SupplierId,
            supplierName,
            payable.BranchId,
            branchName,
            payable.CostCenterId,
            costCenterName,
            payable.Description,
            payable.IssuedAtUtc,
            payable.DueDateUtc,
            payable.OriginalAmount,
            payable.PaidAmount,
            payable.OutstandingAmount,
            currentStatus,
            payable.Settlements
                .OrderByDescending(settlement => settlement.PaidAtUtc)
                .Select(settlement => new PayableSettlementResponse(
                    settlement.Id,
                    settlement.Amount,
                    settlement.PaidAtUtc,
                    settlement.PaymentMethod,
                    settlement.Notes))
                .ToArray());
    }

    private static FinancialTitleStatus CalculateCurrentStatus(FinancialTitleStatus status, DateTime dueDateUtc, decimal outstandingAmount)
    {
        if ((status == FinancialTitleStatus.Open || status == FinancialTitleStatus.PartiallyPaid) &&
            outstandingAmount > 0m &&
            dueDateUtc < DateTime.UtcNow)
        {
            return FinancialTitleStatus.Overdue;
        }

        return status;
    }
}
