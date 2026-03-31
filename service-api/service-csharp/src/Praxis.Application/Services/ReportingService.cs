using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Models;
using Praxis.Application.Persistence;
using Praxis.Domain.Billing;
using Praxis.Domain.Inventory;
using Praxis.Domain.Operations;
using Praxis.Domain.Sales;

namespace Praxis.Application.Services;

public sealed class ReportingService(IPraxisDbContext dbContext, IClock clock, PlatformPolicyService platformPolicyService)
{
    public async Task<ReportingOverviewResponse> GetOverviewAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        Guid? branchId = null,
        Guid? costCenterId = null,
        CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Reporting, branchId, cancellationToken);

        var range = ResolveRange(fromUtc, toUtc);
        var now = clock.UtcNow;

        var invoices = await dbContext.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Items)
            .Where(invoice =>
                invoice.IssuedAtUtc >= range.FromUtc &&
                invoice.IssuedAtUtc <= range.ToUtc &&
                invoice.Status != InvoiceStatus.Cancelled &&
                (!branchId.HasValue || invoice.BranchId == branchId) &&
                (!costCenterId.HasValue || invoice.CostCenterId == costCenterId))
            .ToListAsync(cancellationToken);

        var grossRevenue = invoices.Sum(invoice => invoice.TotalAmount);
        var grossCost = invoices.SelectMany(invoice => invoice.Items).Sum(item => item.LineCost);
        var grossMargin = grossRevenue - grossCost;
        var grossMarginPercentage = grossRevenue == 0m ? 0m : Math.Round((grossMargin / grossRevenue) * 100m, 2);

        var dispatchedOrders = await dbContext.SalesOrders.CountAsync(
            order =>
                order.Status == SalesOrderStatus.Dispatched &&
                order.DispatchedAtUtc >= range.FromUtc &&
                order.DispatchedAtUtc <= range.ToUtc &&
                (!branchId.HasValue || order.BranchId == branchId) &&
                (!costCenterId.HasValue || order.CostCenterId == costCenterId),
            cancellationToken);

        var receivedPurchaseOrders = await dbContext.PurchaseReceipts
            .AsNoTracking()
            .Join(
                dbContext.PurchaseOrders.AsNoTracking(),
                receipt => receipt.PurchaseOrderId,
                order => order.Id,
                (receipt, order) => new { receipt, order })
            .Where(entry =>
                entry.receipt.ReceivedAtUtc >= range.FromUtc &&
                entry.receipt.ReceivedAtUtc <= range.ToUtc &&
                (!branchId.HasValue || entry.order.BranchId == branchId) &&
                (!costCenterId.HasValue || entry.order.CostCenterId == costCenterId))
            .Select(entry => entry.receipt.PurchaseOrderId)
            .Distinct()
            .CountAsync(cancellationToken);

        var openReceivablesAmount = await dbContext.Receivables
            .AsNoTracking()
            .Where(receivable =>
                receivable.Status != FinancialTitleStatus.Paid &&
                receivable.Status != FinancialTitleStatus.Cancelled &&
                (!branchId.HasValue || receivable.BranchId == branchId) &&
                (!costCenterId.HasValue || receivable.CostCenterId == costCenterId))
            .SumAsync(receivable => (decimal?)receivable.OutstandingAmount, cancellationToken) ?? 0m;

        var overdueReceivablesAmount = await dbContext.Receivables
            .AsNoTracking()
            .Where(receivable =>
                receivable.Status != FinancialTitleStatus.Paid &&
                receivable.Status != FinancialTitleStatus.Cancelled &&
                receivable.OutstandingAmount > 0m &&
                receivable.DueDateUtc < now &&
                (!branchId.HasValue || receivable.BranchId == branchId) &&
                (!costCenterId.HasValue || receivable.CostCenterId == costCenterId))
            .SumAsync(receivable => (decimal?)receivable.OutstandingAmount, cancellationToken) ?? 0m;

        var openPayablesAmount = await dbContext.Payables
            .AsNoTracking()
            .Where(payable =>
                payable.Status != FinancialTitleStatus.Paid &&
                payable.Status != FinancialTitleStatus.Cancelled &&
                (!branchId.HasValue || payable.BranchId == branchId) &&
                (!costCenterId.HasValue || payable.CostCenterId == costCenterId))
            .SumAsync(payable => (decimal?)payable.OutstandingAmount, cancellationToken) ?? 0m;

        var customerNames = await dbContext.Customers
            .AsNoTracking()
            .ToDictionaryAsync(customer => customer.Id, customer => customer.Name, cancellationToken);

        var topCustomers = invoices
            .GroupBy(invoice => invoice.CustomerId)
            .Select(group => new ReportingTopCustomerItem(
                group.Key,
                customerNames.TryGetValue(group.Key, out var customerName) ? customerName : "Customer",
                group.Sum(invoice => invoice.TotalAmount),
                group.Sum(invoice => invoice.Items.Sum(item => item.LineTotal - item.LineCost))))
            .OrderByDescending(item => item.Revenue)
            .Take(5)
            .ToArray();

        var supplierNames = await dbContext.Suppliers
            .AsNoTracking()
            .ToDictionaryAsync(supplier => supplier.Id, supplier => supplier.Name, cancellationToken);

        var topSupplierGroups = await dbContext.Payables
            .AsNoTracking()
            .Where(payable =>
                payable.IssuedAtUtc >= range.FromUtc &&
                payable.IssuedAtUtc <= range.ToUtc &&
                (!branchId.HasValue || payable.BranchId == branchId) &&
                (!costCenterId.HasValue || payable.CostCenterId == costCenterId))
            .GroupBy(payable => payable.SupplierId)
            .Select(group => new
            {
                SupplierId = group.Key,
                Spend = group.Sum(payable => payable.OriginalAmount)
            })
            .ToListAsync(cancellationToken);

        var topSuppliers = topSupplierGroups
            .Select(group => new ReportingTopSupplierItem(
                group.SupplierId,
                supplierNames.TryGetValue(group.SupplierId, out var supplierName) ? supplierName : "Supplier",
                group.Spend))
            .OrderByDescending(item => item.Spend)
            .Take(5)
            .ToArray();

        var branchNames = await dbContext.Branches
            .AsNoTracking()
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        var openAlertGroups = await dbContext.OperationalAlerts
            .AsNoTracking()
            .Where(alert => alert.Status == AlertStatus.Open)
            .GroupBy(alert => alert.BranchId)
            .Select(group => new { BranchId = group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var branchPerformance = invoices
            .GroupBy(invoice => invoice.BranchId)
            .Select(group =>
            {
                var currentBranchId = group.Key;
                var openAlerts = openAlertGroups
                    .Where(item => item.BranchId == currentBranchId || item.BranchId == null)
                    .Sum(item => item.Count);

                return new ReportingBranchPerformanceItem(
                    currentBranchId,
                    currentBranchId.HasValue && branchNames.TryGetValue(currentBranchId.Value, out var branchName) ? branchName : "Global",
                    group.Sum(invoice => invoice.TotalAmount),
                    dbContext.Receivables
                        .AsNoTracking()
                        .Where(receivable =>
                            receivable.BranchId == currentBranchId &&
                            receivable.Status != FinancialTitleStatus.Paid &&
                            receivable.Status != FinancialTitleStatus.Cancelled)
                        .Select(receivable => (decimal?)receivable.OutstandingAmount)
                        .Sum() ?? 0m,
                    openAlerts);
            })
            .OrderByDescending(item => item.Revenue)
            .ToArray();

        var costCenterNames = await dbContext.CostCenters
            .AsNoTracking()
            .ToDictionaryAsync(costCenter => costCenter.Id, costCenter => costCenter.Name, cancellationToken);

        var payableGroups = await dbContext.Payables
            .AsNoTracking()
            .Where(payable =>
                payable.IssuedAtUtc >= range.FromUtc &&
                payable.IssuedAtUtc <= range.ToUtc &&
                (!branchId.HasValue || payable.BranchId == branchId) &&
                (!costCenterId.HasValue || payable.CostCenterId == costCenterId))
            .GroupBy(payable => payable.CostCenterId)
            .Select(group => new { CostCenterId = group.Key, Spend = group.Sum(payable => payable.OriginalAmount) })
            .ToListAsync(cancellationToken);

        var costCenterPerformance = invoices
            .GroupBy(invoice => invoice.CostCenterId)
            .Select(group =>
            {
                var currentCostCenterId = group.Key;
                var spend = payableGroups.FirstOrDefault(item => item.CostCenterId == currentCostCenterId)?.Spend ?? 0m;

                return new ReportingCostCenterPerformanceItem(
                    currentCostCenterId,
                    currentCostCenterId.HasValue && costCenterNames.TryGetValue(currentCostCenterId.Value, out var costCenterName) ? costCenterName : "Unassigned",
                    group.Sum(invoice => invoice.TotalAmount),
                    spend);
            })
            .OrderByDescending(item => item.Revenue)
            .ToArray();

        return new ReportingOverviewResponse(
            now,
            range.FromUtc,
            range.ToUtc,
            grossRevenue,
            grossCost,
            grossMargin,
            grossMarginPercentage,
            invoices.Count,
            dispatchedOrders,
            receivedPurchaseOrders,
            openReceivablesAmount,
            overdueReceivablesAmount,
            openPayablesAmount,
            topCustomers,
            topSuppliers,
            branchPerformance,
            costCenterPerformance);
    }

    public async Task<InventoryTurnoverResponse> GetInventoryTurnoverAsync(DateTime? fromUtc, DateTime? toUtc, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Reporting, branchId, cancellationToken);

        var range = ResolveRange(fromUtc, toUtc);
        var now = clock.UtcNow;

        var balances = await dbContext.InventoryBalances
            .AsNoTracking()
            .Join(
                dbContext.WarehouseLocations.AsNoTracking(),
                balance => balance.WarehouseLocationId,
                warehouse => warehouse.Id,
                (balance, warehouse) => new { balance, warehouse })
            .Where(entry => !branchId.HasValue || entry.warehouse.BranchId == branchId)
            .Select(entry => entry.balance)
            .ToListAsync(cancellationToken);

        var products = await dbContext.Products
            .AsNoTracking()
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        var movements = await dbContext.StockMovements
            .AsNoTracking()
            .Join(
                dbContext.WarehouseLocations.AsNoTracking(),
                movement => movement.WarehouseLocationId,
                warehouse => warehouse.Id,
                (movement, warehouse) => new { movement, warehouse })
            .Where(entry =>
                entry.movement.CreatedAtUtc >= range.FromUtc &&
                entry.movement.CreatedAtUtc <= range.ToUtc &&
                (!branchId.HasValue || entry.warehouse.BranchId == branchId))
            .Select(entry => entry.movement)
            .ToListAsync(cancellationToken);

        var outboundQuantity = movements
            .Where(movement => movement.Type == StockMovementType.Outbound)
            .Sum(movement => movement.Quantity);

        var inboundQuantity = movements
            .Where(movement => movement.Type == StockMovementType.Inbound)
            .Sum(movement => movement.Quantity);

        var inventoryValue = balances.Sum(balance =>
            products.TryGetValue(balance.ProductId, out var product)
                ? balance.OnHandQuantity * product.StandardCost
                : 0m);

        var totalOnHand = balances.Sum(balance => balance.OnHandQuantity);
        var stockTurnoverRatio = totalOnHand == 0 ? 0m : Math.Round((decimal)outboundQuantity / totalOnHand, 2);

        var items = balances
            .Select(balance =>
            {
                products.TryGetValue(balance.ProductId, out var product);
                var productOutboundQuantity = movements
                    .Where(movement => movement.ProductId == balance.ProductId && movement.Type == StockMovementType.Outbound)
                    .Sum(movement => movement.Quantity);
                var ratio = balance.OnHandQuantity == 0
                    ? 0m
                    : Math.Round((decimal)productOutboundQuantity / balance.OnHandQuantity, 2);

                return new InventoryTurnoverItem(
                    balance.ProductId,
                    product?.Name ?? "Product",
                    product?.Sku ?? "SKU",
                    balance.OnHandQuantity,
                    productOutboundQuantity,
                    ratio);
            })
            .OrderByDescending(item => item.TurnoverRatio)
            .ThenBy(item => item.ProductName)
            .Take(15)
            .ToArray();

        return new InventoryTurnoverResponse(
            now,
            range.FromUtc,
            range.ToUtc,
            inventoryValue,
            inboundQuantity,
            outboundQuantity,
            stockTurnoverRatio,
            items);
    }

    public async Task<OverdueReceivablesReportResponse> GetOverdueReceivablesAsync(Guid? branchId = null, Guid? costCenterId = null, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Reporting, branchId, cancellationToken);

        var now = clock.UtcNow;
        var allOpenTitles = await dbContext.Receivables
            .AsNoTracking()
            .Where(receivable =>
                receivable.Status != FinancialTitleStatus.Paid &&
                receivable.Status != FinancialTitleStatus.Cancelled &&
                (!branchId.HasValue || receivable.BranchId == branchId) &&
                (!costCenterId.HasValue || receivable.CostCenterId == costCenterId))
            .ToListAsync(cancellationToken);

        var customerNames = await dbContext.Customers
            .AsNoTracking()
            .ToDictionaryAsync(customer => customer.Id, customer => customer.Name, cancellationToken);

        var overdueItems = allOpenTitles
            .Where(receivable => receivable.OutstandingAmount > 0m && receivable.DueDateUtc < now)
            .OrderBy(receivable => receivable.DueDateUtc)
            .Select(receivable => new OverdueReceivableReportItem(
                receivable.Id,
                receivable.DocumentNumber,
                customerNames.TryGetValue(receivable.CustomerId, out var customerName) ? customerName : "Customer",
                receivable.DueDateUtc,
                Math.Max(1, (now.Date - receivable.DueDateUtc.Date).Days),
                receivable.OutstandingAmount))
            .ToArray();

        return new OverdueReceivablesReportResponse(
            now,
            allOpenTitles.Count,
            overdueItems.Length,
            overdueItems.Sum(item => item.OutstandingAmount),
            overdueItems);
    }

    private ReportRange ResolveRange(DateTime? fromUtc, DateTime? toUtc)
    {
        var resolvedTo = toUtc?.ToUniversalTime() ?? clock.UtcNow;
        var resolvedFrom = fromUtc?.ToUniversalTime() ?? resolvedTo.AddDays(-30);

        if (resolvedFrom > resolvedTo)
        {
            resolvedFrom = resolvedTo.AddDays(-30);
        }

        return new ReportRange(resolvedFrom, resolvedTo);
    }
}
