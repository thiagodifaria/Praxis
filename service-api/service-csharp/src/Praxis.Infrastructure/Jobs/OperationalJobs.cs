using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Praxis.Application.Abstractions;
using Praxis.Application.Services;
using Praxis.Domain.Billing;
using Praxis.Infrastructure.Persistence;

namespace Praxis.Infrastructure.Jobs;

public sealed class OperationalJobs(
    DashboardService dashboardService,
    PraxisDbContext dbContext,
    IEventBus eventBus,
    ILogger<OperationalJobs> logger)
{
    public async Task RefreshDashboardSnapshotAsync()
    {
        logger.LogInformation("Refreshing dashboard snapshot from Hangfire job");
        await dashboardService.GetAsync(forceRefresh: true);
    }

    public async Task ScanLowStockAsync()
    {
        logger.LogInformation("Scanning low stock products from Hangfire job");

        var entries = await dbContext.InventoryBalances
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
                    entry.product.Sku,
                    ProductName = entry.product.Name,
                    warehouse.BranchId,
                    WarehouseId = warehouse.Id,
                    WarehouseName = warehouse.Name,
                    AvailableQuantity = entry.balance.OnHandQuantity - entry.balance.ReservedQuantity,
                    entry.product.ReorderLevel
                })
            .ToListAsync();

        foreach (var entry in entries)
        {
            await eventBus.PublishAsync("inventory.low-stock.detected", entry);
        }
    }

    public async Task ScanFinancialTitlesAsync()
    {
        logger.LogInformation("Scanning overdue receivables and payables from Hangfire job");

        var now = DateTime.UtcNow;

        var overdueReceivables = await dbContext.Receivables
            .Where(receivable =>
                receivable.Status != FinancialTitleStatus.Paid &&
                receivable.Status != FinancialTitleStatus.Cancelled &&
                receivable.OutstandingAmount > 0m &&
                receivable.DueDateUtc < now)
            .ToListAsync();

        foreach (var receivable in overdueReceivables)
        {
            receivable.MarkOverdue(now);
            await eventBus.PublishAsync("finance.receivable.overdue.detected", new
            {
                receivable.Id,
                receivable.DocumentNumber,
                receivable.BranchId,
                receivable.CostCenterId,
                receivable.CustomerId,
                receivable.OutstandingAmount,
                receivable.DueDateUtc
            });
        }

        var overduePayables = await dbContext.Payables
            .Where(payable =>
                payable.Status != FinancialTitleStatus.Paid &&
                payable.Status != FinancialTitleStatus.Cancelled &&
                payable.OutstandingAmount > 0m &&
                payable.DueDateUtc < now)
            .ToListAsync();

        foreach (var payable in overduePayables)
        {
            payable.MarkOverdue(now);
            await eventBus.PublishAsync("finance.payable.overdue.detected", new
            {
                payable.Id,
                payable.DocumentNumber,
                payable.BranchId,
                payable.CostCenterId,
                payable.SupplierId,
                payable.OutstandingAmount,
                payable.DueDateUtc
            });
        }

        await dbContext.SaveChangesAsync();
    }
}
