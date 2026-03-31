using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Models;
using Praxis.Application.Services;
using Praxis.Domain.Billing;
using Praxis.Domain.Catalog;
using Praxis.Domain.Inventory;
using Praxis.Domain.Operations;
using Praxis.Domain.Purchasing;
using Praxis.Domain.Sales;
using Praxis.Infrastructure.Persistence;

namespace Praxis.IntegrationTests;

public sealed class DashboardServiceIntegrationTests
{
    [Fact]
    public async Task GetAsync_ShouldBuildSnapshotAndStoreItInCache()
    {
        var now = new DateTime(2026, 3, 29, 12, 0, 0, DateTimeKind.Utc);
        var options = new DbContextOptionsBuilder<PraxisDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new PraxisDbContext(options);

        var branch = new Branch("HQ", "Matriz", "Praxis LTDA", "12.345.678/0001-10", "Sao Paulo", "SP", true, true);
        branch.SetCreatedAt(now);

        var costCenter = new CostCenter(branch.Id, "OMS-HQ", "Operacoes Matriz", "Main operations", true);
        costCenter.SetCreatedAt(now);

        var category = new Category("ELEC", "Electronics", "Electronics");
        category.SetCreatedAt(now);

        var product = new Product("SKU-100", "Notebook", "Corporate notebook", 5000m, 3200m, 10, category.Id, null);
        product.SetCreatedAt(now);

        var customer = new Customer("CUS-1", "Studio Orion", "12.345.678/0001-99", "ops@orion.local", "11999999999", CustomerStatus.Active);
        customer.SetCreatedAt(now);

        var warehouse = new WarehouseLocation(branch.Id, "MAIN", "Main Warehouse", "Primary stock", true);
        warehouse.SetCreatedAt(now);

        var balance = new InventoryBalance(product.Id, warehouse.Id);
        balance.SetCreatedAt(now);
        balance.AddStock(7, now);

        var order = new SalesOrder("SO-0001", customer.Id, warehouse.Id, branch.Id, costCenter.Id, "priority");
        order.SetCreatedAt(now);
        order.AddItem(product.Id, product.Sku, product.Name, 2, product.UnitPrice, product.StandardCost);
        order.Approve(now);

        var purchaseOrder = new PurchaseOrder("PO-0001", Guid.NewGuid(), warehouse.Id, branch.Id, costCenter.Id, now.AddDays(5), "inbound");
        purchaseOrder.SetCreatedAt(now);
        purchaseOrder.AddItem(product.Id, product.Sku, product.Name, 5, 3000m);
        purchaseOrder.Approve(now);

        var invoice = new Invoice("INV-0001", order.Id, customer.Id, branch.Id, costCenter.Id, now, now.AddDays(30), "invoice");
        invoice.SetCreatedAt(now);
        invoice.AddItem(product.Id, product.Sku, product.Name, 2, product.UnitPrice, product.StandardCost);

        var receivable = new Receivable(customer.Id, invoice.Id, branch.Id, costCenter.Id, invoice.InvoiceNumber, "invoice receivable", now, now.AddDays(-2), invoice.TotalAmount);
        receivable.SetCreatedAt(now);

        var payable = new Payable(Guid.NewGuid(), purchaseOrder.Id, null, branch.Id, costCenter.Id, "PAY-0001", "supplier payable", now, now.AddDays(15), 15000m);
        payable.SetCreatedAt(now);

        var movement = new StockMovement(product.Id, warehouse.Id, StockMovementType.Adjustment, 7, "Initial load", "seed", null, null);
        movement.SetCreatedAt(now);

        var alert = new OperationalAlert("LOW-STOCK-TEST", "Low stock", "Notebook below reorder level.", "inventory", branch.Id, product.Id.ToString(), AlertSeverity.Warning);
        alert.SetCreatedAt(now);

        dbContext.Branches.Add(branch);
        dbContext.CostCenters.Add(costCenter);
        dbContext.Categories.Add(category);
        dbContext.Products.Add(product);
        dbContext.Customers.Add(customer);
        dbContext.WarehouseLocations.Add(warehouse);
        dbContext.InventoryBalances.Add(balance);
        dbContext.SalesOrders.Add(order);
        dbContext.PurchaseOrders.Add(purchaseOrder);
        dbContext.Invoices.Add(invoice);
        dbContext.Receivables.Add(receivable);
        dbContext.Payables.Add(payable);
        dbContext.StockMovements.Add(movement);
        dbContext.OperationalAlerts.Add(alert);

        await dbContext.SaveChangesAsync();

        var cache = new InMemoryDashboardCache();
        var service = new DashboardService(dbContext, cache, new FixedClock(now), new PlatformPolicyService(dbContext));

        var snapshot = await service.GetAsync();

        Assert.Equal(1, snapshot.ActiveCustomers);
        Assert.Equal(1, snapshot.ActiveProducts);
        Assert.Equal(0, snapshot.DraftOrders);
        Assert.Equal(1, snapshot.ApprovedOrders);
        Assert.Equal(1, snapshot.ApprovedPurchaseOrders);
        Assert.Equal(1, snapshot.IssuedInvoices);
        Assert.Equal(1, snapshot.LowStockProducts);
        Assert.Equal(invoice.TotalAmount, snapshot.OpenReceivablesAmount);
        Assert.Equal(invoice.TotalAmount, snapshot.OverdueReceivablesAmount);
        Assert.Equal(payable.OriginalAmount, snapshot.OpenPayablesAmount);
        Assert.Equal(0, snapshot.PendingApprovals);
        Assert.Equal(0, snapshot.UnreadNotifications);
        Assert.Single(snapshot.RecentOrders);
        Assert.Single(snapshot.RecentInvoices);
        Assert.Single(snapshot.OpenAlerts);
        Assert.Single(snapshot.Branches);
        Assert.NotNull(await cache.GetAsync());
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow => utcNow;
    }

    private sealed class InMemoryDashboardCache : IDashboardCache
    {
        private DashboardSnapshotResponse? _snapshot;

        public Task<DashboardSnapshotResponse?> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_snapshot);
        }

        public Task SetAsync(DashboardSnapshotResponse snapshot, CancellationToken cancellationToken = default)
        {
            _snapshot = snapshot;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(CancellationToken cancellationToken = default)
        {
            _snapshot = null;
            return Task.CompletedTask;
        }
    }
}
