using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Models;
using Praxis.Application.Persistence;
using Praxis.Domain.Billing;
using Praxis.Domain.Operations;
using Praxis.Domain.Purchasing;
using Praxis.Domain.Sales;

namespace Praxis.Application.Services;

public sealed class DashboardService(
    IPraxisDbContext dbContext,
    IDashboardCache dashboardCache,
    IClock clock,
    PlatformPolicyService platformPolicyService)
{
    public async Task<DashboardSnapshotResponse> GetAsync(Guid? branchId = null, bool forceRefresh = false, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Dashboard, branchId, cancellationToken);

        if (!forceRefresh && !branchId.HasValue)
        {
            var cached = await dashboardCache.GetAsync(cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var snapshot = await BuildSnapshotAsync(branchId, cancellationToken);

        if (!branchId.HasValue)
        {
            await dashboardCache.SetAsync(snapshot, cancellationToken);
        }

        return snapshot;
    }

    public async Task<DashboardSnapshotResponse> BuildSnapshotAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var activeCustomers = branchId.HasValue
            ? await dbContext.SalesOrders
                .AsNoTracking()
                .Where(order => order.BranchId == branchId)
                .Select(order => order.CustomerId)
                .Distinct()
                .CountAsync(cancellationToken)
            : await dbContext.Customers.CountAsync(customer => customer.Status == CustomerStatus.Active, cancellationToken);

        var activeProducts = branchId.HasValue
            ? await dbContext.InventoryBalances
                .AsNoTracking()
                .Join(
                    dbContext.WarehouseLocations.AsNoTracking(),
                    balance => balance.WarehouseLocationId,
                    warehouse => warehouse.Id,
                    (balance, warehouse) => new { balance, warehouse })
                .Where(entry => entry.warehouse.BranchId == branchId)
                .Select(entry => entry.balance.ProductId)
                .Distinct()
                .CountAsync(cancellationToken)
            : await dbContext.Products.CountAsync(product => product.IsActive, cancellationToken);

        var draftOrders = await dbContext.SalesOrders.CountAsync(
            order => order.Status == SalesOrderStatus.Draft && (!branchId.HasValue || order.BranchId == branchId),
            cancellationToken);

        var approvedOrders = await dbContext.SalesOrders.CountAsync(
            order => order.Status == SalesOrderStatus.Approved && (!branchId.HasValue || order.BranchId == branchId),
            cancellationToken);

        var approvedPurchaseOrders = await dbContext.PurchaseOrders.CountAsync(
            order =>
                (order.Status == PurchaseOrderStatus.Approved || order.Status == PurchaseOrderStatus.PartiallyReceived) &&
                (!branchId.HasValue || order.BranchId == branchId),
            cancellationToken);

        var issuedInvoices = await dbContext.Invoices.CountAsync(
            invoice => invoice.Status == InvoiceStatus.Issued && (!branchId.HasValue || invoice.BranchId == branchId),
            cancellationToken);

        var pendingApprovals = await dbContext.ApprovalDecisions.CountAsync(
            decision => decision.Status == ApprovalDecisionStatus.Pending && (!branchId.HasValue || decision.BranchId == branchId),
            cancellationToken);

        var unreadNotifications = await dbContext.RealtimeNotifications.CountAsync(
            notification =>
                !notification.IsRead &&
                (!branchId.HasValue || notification.BranchId == null || notification.BranchId == branchId),
            cancellationToken);

        var orderPipelineAmount = await dbContext.SalesOrders
            .Where(order =>
                (order.Status == SalesOrderStatus.Draft || order.Status == SalesOrderStatus.Approved) &&
                (!branchId.HasValue || order.BranchId == branchId))
            .SumAsync(order => (decimal?)order.TotalAmount, cancellationToken) ?? 0m;

        var openReceivablesAmount = await dbContext.Receivables
            .Where(receivable =>
                receivable.Status != FinancialTitleStatus.Paid &&
                receivable.Status != FinancialTitleStatus.Cancelled &&
                (!branchId.HasValue || receivable.BranchId == branchId))
            .SumAsync(receivable => (decimal?)receivable.OutstandingAmount, cancellationToken) ?? 0m;

        var overdueReceivablesAmount = await dbContext.Receivables
            .Where(receivable =>
                receivable.Status != FinancialTitleStatus.Paid &&
                receivable.Status != FinancialTitleStatus.Cancelled &&
                receivable.OutstandingAmount > 0m &&
                receivable.DueDateUtc < now &&
                (!branchId.HasValue || receivable.BranchId == branchId))
            .SumAsync(receivable => (decimal?)receivable.OutstandingAmount, cancellationToken) ?? 0m;

        var openPayablesAmount = await dbContext.Payables
            .Where(payable =>
                payable.Status != FinancialTitleStatus.Paid &&
                payable.Status != FinancialTitleStatus.Cancelled &&
                (!branchId.HasValue || payable.BranchId == branchId))
            .SumAsync(payable => (decimal?)payable.OutstandingAmount, cancellationToken) ?? 0m;

        var recentOrders = await dbContext.SalesOrders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Where(order => !branchId.HasValue || order.BranchId == branchId)
            .OrderByDescending(order => order.CreatedAtUtc)
            .Take(5)
            .Select(order => new DashboardOrderOverview(
                order.OrderNumber,
                order.Customer.Name,
                order.Status,
                order.TotalAmount,
                order.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var recentInvoiceRows = await dbContext.Invoices
            .AsNoTracking()
            .Where(invoice => !branchId.HasValue || invoice.BranchId == branchId)
            .Join(
                dbContext.Customers.AsNoTracking(),
                invoice => invoice.CustomerId,
                customer => customer.Id,
                (invoice, customer) => new
                {
                    invoice.InvoiceNumber,
                    CustomerName = customer.Name,
                    invoice.Status,
                    invoice.TotalAmount,
                    invoice.IssuedAtUtc
                })
            .OrderByDescending(invoice => invoice.IssuedAtUtc)
            .Take(5)
            .ToListAsync(cancellationToken);

        var recentInvoices = recentInvoiceRows
            .Select(invoice => new DashboardInvoiceOverview(
                invoice.InvoiceNumber,
                invoice.CustomerName,
                invoice.Status,
                invoice.TotalAmount,
                invoice.IssuedAtUtc))
            .ToArray();

        var lowStockItems = await dbContext.InventoryBalances
            .AsNoTracking()
            .Join(
                dbContext.Products.AsNoTracking(),
                balance => balance.ProductId,
                product => product.Id,
                (balance, product) => new { balance, product })
            .Where(entry => entry.balance.OnHandQuantity - entry.balance.ReservedQuantity <= entry.product.ReorderLevel)
            .Join(
                dbContext.WarehouseLocations.AsNoTracking(),
                entry => entry.balance.WarehouseLocationId,
                warehouse => warehouse.Id,
                (entry, warehouse) => new
                {
                    entry.product.Id,
                    entry.product.Name,
                    entry.product.Sku,
                    AvailableQuantity = entry.balance.OnHandQuantity - entry.balance.ReservedQuantity,
                    entry.product.ReorderLevel,
                    WarehouseName = warehouse.Name,
                    warehouse.BranchId
                })
            .Where(entry => !branchId.HasValue || entry.BranchId == branchId)
            .OrderBy(entry => entry.AvailableQuantity)
            .Select(entry => new DashboardLowStockItem(
                entry.Id,
                entry.Name,
                entry.Sku,
                entry.AvailableQuantity,
                entry.ReorderLevel,
                entry.WarehouseName))
            .Take(10)
            .ToListAsync(cancellationToken);

        var recentMovements = await dbContext.StockMovements
            .AsNoTracking()
            .Join(
                dbContext.Products.AsNoTracking(),
                movement => movement.ProductId,
                product => product.Id,
                (movement, product) => new { movement, product })
            .Join(
                dbContext.WarehouseLocations.AsNoTracking(),
                entry => entry.movement.WarehouseLocationId,
                warehouse => warehouse.Id,
                (entry, warehouse) => new
                {
                    ProductName = entry.product.Name,
                    ProductSku = entry.product.Sku,
                    WarehouseName = warehouse.Name,
                    warehouse.BranchId,
                    entry.movement.Reason,
                    entry.movement.Quantity,
                    entry.movement.CreatedAtUtc
                })
            .Where(entry => !branchId.HasValue || entry.BranchId == branchId)
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Select(entry => new DashboardMovementOverview(
                entry.ProductName,
                entry.ProductSku,
                entry.WarehouseName,
                entry.Reason,
                entry.Quantity,
                entry.CreatedAtUtc))
            .Take(8)
            .ToListAsync(cancellationToken);

        var openAlerts = await dbContext.OperationalAlerts
            .AsNoTracking()
            .Where(alert =>
                alert.Status == AlertStatus.Open &&
                (!branchId.HasValue || alert.BranchId == null || alert.BranchId == branchId))
            .OrderByDescending(alert => alert.CreatedAtUtc)
            .Take(8)
            .Select(alert => new DashboardAlertOverview(alert.Id, alert.Code, alert.Title, alert.Severity, alert.Status, alert.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var branchIds = branchId.HasValue
            ? new[] { branchId.Value }
            : await dbContext.Branches
                .AsNoTracking()
                .Where(branch => branch.IsActive)
                .OrderBy(branch => branch.Name)
                .Select(branch => branch.Id)
                .ToArrayAsync(cancellationToken);

        var branchNames = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        var branchOverviews = new List<DashboardBranchOverview>();
        foreach (var currentBranchId in branchIds)
        {
            var activeSales = await dbContext.SalesOrders.CountAsync(
                order =>
                    order.BranchId == currentBranchId &&
                    (order.Status == SalesOrderStatus.Draft || order.Status == SalesOrderStatus.Approved),
                cancellationToken);

            var activePurchasing = await dbContext.PurchaseOrders.CountAsync(
                order =>
                    order.BranchId == currentBranchId &&
                    (order.Status == PurchaseOrderStatus.Draft ||
                     order.Status == PurchaseOrderStatus.Approved ||
                     order.Status == PurchaseOrderStatus.PartiallyReceived),
                cancellationToken);

            var branchReceivables = await dbContext.Receivables
                .Where(receivable =>
                    receivable.BranchId == currentBranchId &&
                    receivable.Status != FinancialTitleStatus.Paid &&
                    receivable.Status != FinancialTitleStatus.Cancelled)
                .SumAsync(receivable => (decimal?)receivable.OutstandingAmount, cancellationToken) ?? 0m;

            var branchAlerts = await dbContext.OperationalAlerts.CountAsync(
                alert => alert.Status == AlertStatus.Open && (alert.BranchId == null || alert.BranchId == currentBranchId),
                cancellationToken);

            branchOverviews.Add(new DashboardBranchOverview(
                currentBranchId,
                branchNames.TryGetValue(currentBranchId, out var branchName) ? branchName : "Branch",
                activeSales,
                activePurchasing,
                branchReceivables,
                branchAlerts));
        }

        return new DashboardSnapshotResponse(
            activeCustomers,
            activeProducts,
            draftOrders,
            approvedOrders,
            approvedPurchaseOrders,
            issuedInvoices,
            lowStockItems.Count,
            pendingApprovals,
            unreadNotifications,
            orderPipelineAmount,
            openReceivablesAmount,
            overdueReceivablesAmount,
            openPayablesAmount,
            recentOrders,
            recentInvoices,
            lowStockItems,
            recentMovements,
            openAlerts,
            branchOverviews,
            now);
    }
}
