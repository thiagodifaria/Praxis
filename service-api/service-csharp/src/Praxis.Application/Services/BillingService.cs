using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Common;
using Praxis.Application.Models;
using Praxis.Application.Persistence;
using Praxis.Domain.Billing;
using Praxis.Domain.Operations;
using Praxis.Domain.Sales;

namespace Praxis.Application.Services;

public sealed class BillingService(
    IPraxisDbContext dbContext,
    ICurrentUserService currentUserService,
    IEventBus eventBus,
    IDashboardCache dashboardCache,
    PlatformPolicyService platformPolicyService)
{
    public async Task<IReadOnlyCollection<InvoiceResponse>> ListInvoicesAsync(Guid? branchId = null, Guid? costCenterId = null, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Billing, branchId, cancellationToken);

        var query = dbContext.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Items)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(invoice => invoice.BranchId == branchId);
        }

        if (costCenterId.HasValue)
        {
            query = query.Where(invoice => invoice.CostCenterId == costCenterId);
        }

        var invoices = await query
            .OrderByDescending(invoice => invoice.IssuedAtUtc)
            .ToListAsync(cancellationToken);

        return await MapInvoicesAsync(invoices, cancellationToken);
    }

    public async Task<InvoiceResponse> GetInvoiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .Include(current => current.Items)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Invoice not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Billing, invoice.BranchId, cancellationToken);

        return await MapInvoiceAsync(invoice, cancellationToken);
    }

    public async Task<InvoiceResponse> IssueInvoiceAsync(IssueInvoiceRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var salesOrder = await dbContext.SalesOrders
            .Include(order => order.Customer)
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == request.SalesOrderId, cancellationToken)
            ?? throw new NotFoundException("Sales order not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Billing, salesOrder.BranchId, cancellationToken);

        if (salesOrder.Status != SalesOrderStatus.Dispatched)
        {
            throw new ConflictException("Only dispatched orders can be invoiced.");
        }

        var hasActiveInvoice = await dbContext.Invoices.AnyAsync(
            invoice => invoice.SalesOrderId == salesOrder.Id && invoice.Status != InvoiceStatus.Cancelled,
            cancellationToken);

        if (hasActiveInvoice)
        {
            throw new ConflictException("This sales order already has an active invoice.");
        }

        var dueDateUtc = request.DueDateUtc ?? utcNow.AddDays(30);
        var invoiceNumber = $"INV-{utcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        var invoice = new Invoice(invoiceNumber, salesOrder.Id, salesOrder.CustomerId, salesOrder.BranchId, salesOrder.CostCenterId, utcNow, dueDateUtc, request.Notes);
        invoice.SetCreatedAt(utcNow);

        foreach (var item in salesOrder.Items)
        {
            invoice.AddItem(item.ProductId, item.Sku, item.ProductName, item.Quantity, item.UnitPrice, item.UnitCost);
        }

        dbContext.Invoices.Add(invoice);

        var receivable = new Receivable(
            salesOrder.CustomerId,
            invoice.Id,
            salesOrder.BranchId,
            salesOrder.CostCenterId,
            invoice.InvoiceNumber,
            $"Receivable generated from order {salesOrder.OrderNumber}",
            utcNow,
            dueDateUtc,
            invoice.TotalAmount);

        receivable.SetCreatedAt(utcNow);
        dbContext.Receivables.Add(receivable);

        var actor = currentUserService.GetCurrentUser();
        dbContext.AuditEntries.Add(new AuditEntry(
            "billing.invoice.issued",
            nameof(Invoice),
            invoice.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new
            {
                invoice.InvoiceNumber,
                invoice.SalesOrderId,
                invoice.CustomerId,
                invoice.BranchId,
                invoice.CostCenterId,
                invoice.TotalAmount,
                invoice.DueDateUtc
            })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        var branchName = invoice.BranchId.HasValue
            ? await dbContext.Branches.Where(branch => branch.Id == invoice.BranchId.Value).Select(branch => branch.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        var costCenterName = invoice.CostCenterId.HasValue
            ? await dbContext.CostCenters.Where(costCenter => costCenter.Id == invoice.CostCenterId.Value).Select(costCenter => costCenter.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        await eventBus.PublishAsync(
            "billing.invoice.issued",
            new
            {
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.BranchId,
                invoice.CostCenterId,
                salesOrder.OrderNumber,
                CustomerName = salesOrder.Customer.Name,
                BranchName = branchName,
                CostCenterName = costCenterName,
                invoice.TotalAmount
            },
            cancellationToken);

        return ToResponse(invoice, salesOrder.OrderNumber, salesOrder.Customer.Name, branchName, costCenterName);
    }

    public async Task<InvoiceResponse> CancelInvoiceAsync(Guid id, string? notes, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .Include(current => current.Items)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Invoice not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Billing, invoice.BranchId, cancellationToken);

        var receivable = await dbContext.Receivables
            .FirstOrDefaultAsync(current => current.InvoiceId == invoice.Id, cancellationToken)
            ?? throw new NotFoundException("Receivable for invoice not found.");

        var salesOrder = await dbContext.SalesOrders
            .AsNoTracking()
            .FirstAsync(order => order.Id == invoice.SalesOrderId, cancellationToken);

        var customerName = await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == invoice.CustomerId)
            .Select(customer => customer.Name)
            .FirstAsync(cancellationToken);

        var branchName = invoice.BranchId.HasValue
            ? await dbContext.Branches.Where(branch => branch.Id == invoice.BranchId.Value).Select(branch => branch.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        var costCenterName = invoice.CostCenterId.HasValue
            ? await dbContext.CostCenters.Where(costCenter => costCenter.Id == invoice.CostCenterId.Value).Select(costCenter => costCenter.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        invoice.Cancel(notes, utcNow);
        receivable.Cancel(utcNow);

        var actor = currentUserService.GetCurrentUser();
        dbContext.AuditEntries.Add(new AuditEntry(
            "billing.invoice.cancelled",
            nameof(Invoice),
            invoice.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new { invoice.InvoiceNumber, invoice.TotalAmount, notes })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        return ToResponse(invoice, salesOrder.OrderNumber, customerName, branchName, costCenterName);
    }

    private async Task<IReadOnlyCollection<InvoiceResponse>> MapInvoicesAsync(IReadOnlyCollection<Invoice> invoices, CancellationToken cancellationToken)
    {
        var customerIds = invoices.Select(invoice => invoice.CustomerId).Distinct().ToArray();
        var orderIds = invoices.Select(invoice => invoice.SalesOrderId).Distinct().ToArray();
        var branchIds = invoices.Where(invoice => invoice.BranchId.HasValue).Select(invoice => invoice.BranchId!.Value).Distinct().ToArray();
        var costCenterIds = invoices.Where(invoice => invoice.CostCenterId.HasValue).Select(invoice => invoice.CostCenterId!.Value).Distinct().ToArray();

        var customers = await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customerIds.Contains(customer.Id))
            .ToDictionaryAsync(customer => customer.Id, customer => customer.Name, cancellationToken);

        var orders = await dbContext.SalesOrders
            .AsNoTracking()
            .Where(order => orderIds.Contains(order.Id))
            .ToDictionaryAsync(order => order.Id, order => order.OrderNumber, cancellationToken);

        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        var costCenters = await dbContext.CostCenters
            .AsNoTracking()
            .Where(costCenter => costCenterIds.Contains(costCenter.Id))
            .ToDictionaryAsync(costCenter => costCenter.Id, costCenter => costCenter.Name, cancellationToken);

        return invoices
            .Select(invoice => ToResponse(
                invoice,
                orders[invoice.SalesOrderId],
                customers[invoice.CustomerId],
                invoice.BranchId.HasValue && branches.TryGetValue(invoice.BranchId.Value, out var branchName) ? branchName : null,
                invoice.CostCenterId.HasValue && costCenters.TryGetValue(invoice.CostCenterId.Value, out var costCenterName) ? costCenterName : null))
            .ToArray();
    }

    private async Task<InvoiceResponse> MapInvoiceAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        var customerName = await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == invoice.CustomerId)
            .Select(customer => customer.Name)
            .FirstAsync(cancellationToken);

        var orderNumber = await dbContext.SalesOrders
            .AsNoTracking()
            .Where(order => order.Id == invoice.SalesOrderId)
            .Select(order => order.OrderNumber)
            .FirstAsync(cancellationToken);

        var branchName = invoice.BranchId.HasValue
            ? await dbContext.Branches
                .AsNoTracking()
                .Where(branch => branch.Id == invoice.BranchId.Value)
                .Select(branch => branch.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var costCenterName = invoice.CostCenterId.HasValue
            ? await dbContext.CostCenters
                .AsNoTracking()
                .Where(costCenter => costCenter.Id == invoice.CostCenterId.Value)
                .Select(costCenter => costCenter.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        return ToResponse(invoice, orderNumber, customerName, branchName, costCenterName);
    }

    private static InvoiceResponse ToResponse(Invoice invoice, string salesOrderNumber, string customerName, string? branchName, string? costCenterName)
    {
        return new InvoiceResponse(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.SalesOrderId,
            salesOrderNumber,
            invoice.CustomerId,
            customerName,
            invoice.BranchId,
            branchName,
            invoice.CostCenterId,
            costCenterName,
            invoice.Status,
            invoice.IssuedAtUtc,
            invoice.DueDateUtc,
            invoice.TotalAmount,
            invoice.Notes,
            invoice.Items
                .OrderBy(item => item.ProductName)
                .Select(item => new InvoiceItemResponse(
                    item.ProductId,
                    item.Sku,
                    item.ProductName,
                    item.Quantity,
                    item.UnitPrice,
                    item.UnitCost,
                    item.LineTotal,
                    item.LineCost))
                .ToArray());
    }
}
