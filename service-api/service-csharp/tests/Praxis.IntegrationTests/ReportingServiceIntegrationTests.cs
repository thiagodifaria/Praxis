using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Services;
using Praxis.Domain.Billing;
using Praxis.Domain.Catalog;
using Praxis.Domain.Inventory;
using Praxis.Domain.Operations;
using Praxis.Domain.Sales;
using Praxis.Infrastructure.Persistence;

namespace Praxis.IntegrationTests;

public sealed class ReportingServiceIntegrationTests
{
    [Fact]
    public async Task GetOverviewAsync_ShouldReturnMarginAndFinancialIndicators()
    {
        var now = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc);
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
        var customer = new Customer("CUS-100", "Studio Orion", "12.345.678/0001-99", "ops@orion.local", "11999999999", CustomerStatus.Active);
        customer.SetCreatedAt(now);
        var supplier = new Supplier("SUP-100", "ACME Supply", "Maria", "acme@vendors.local", "11999999998");
        supplier.SetCreatedAt(now);
        var product = new Product("SKU-100", "Notebook", "Corporate notebook", 5000m, 3000m, 10, category.Id, supplier.Id);
        product.SetCreatedAt(now);
        var warehouse = new WarehouseLocation(branch.Id, "MAIN", "Main Warehouse", "Primary stock", true);
        warehouse.SetCreatedAt(now);
        var balance = new InventoryBalance(product.Id, warehouse.Id);
        balance.SetCreatedAt(now);
        balance.AddStock(8, now);
        var movement = new StockMovement(product.Id, warehouse.Id, StockMovementType.Outbound, 2, "Dispatch", "seed", null, null);
        movement.SetCreatedAt(now);

        var invoice = new Invoice("INV-100", Guid.NewGuid(), customer.Id, branch.Id, costCenter.Id, now.AddDays(-1), now.AddDays(10), "invoice");
        invoice.SetCreatedAt(now.AddDays(-1));
        invoice.AddItem(product.Id, product.Sku, product.Name, 2, 5000m, 3000m);

        var receivable = new Receivable(customer.Id, invoice.Id, branch.Id, costCenter.Id, invoice.InvoiceNumber, "invoice receivable", now.AddDays(-1), now.AddDays(-1), invoice.TotalAmount);
        receivable.SetCreatedAt(now.AddDays(-1));

        var payable = new Payable(supplier.Id, null, null, branch.Id, costCenter.Id, "PAY-100", "supplier payable", now.AddDays(-1), now.AddDays(15), 4000m);
        payable.SetCreatedAt(now.AddDays(-1));

        dbContext.Branches.Add(branch);
        dbContext.CostCenters.Add(costCenter);
        dbContext.Categories.Add(category);
        dbContext.Customers.Add(customer);
        dbContext.Suppliers.Add(supplier);
        dbContext.Products.Add(product);
        dbContext.WarehouseLocations.Add(warehouse);
        dbContext.InventoryBalances.Add(balance);
        dbContext.StockMovements.Add(movement);
        dbContext.Invoices.Add(invoice);
        dbContext.Receivables.Add(receivable);
        dbContext.Payables.Add(payable);

        await dbContext.SaveChangesAsync();

        var service = new ReportingService(dbContext, new FixedClock(now), new PlatformPolicyService(dbContext));

        var overview = await service.GetOverviewAsync(now.AddDays(-30), now.AddDays(1));
        var overdue = await service.GetOverdueReceivablesAsync();
        var turnover = await service.GetInventoryTurnoverAsync(now.AddDays(-30), now.AddDays(1));

        Assert.Equal(10000m, overview.GrossRevenue);
        Assert.Equal(6000m, overview.GrossCost);
        Assert.Equal(4000m, overview.GrossMargin);
        Assert.Equal(40m, overview.GrossMarginPercentage);
        Assert.Equal(invoice.TotalAmount, overview.OverdueReceivablesAmount);
        Assert.Single(overview.BranchPerformance);
        Assert.Single(overview.CostCenterPerformance);
        Assert.Single(overdue.Items);
        Assert.Equal(2, turnover.OutboundQuantity);
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow => utcNow;
    }
}
