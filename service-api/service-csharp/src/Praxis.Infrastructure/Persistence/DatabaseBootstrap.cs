using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Praxis.Application.Abstractions;
using Praxis.Domain.Billing;
using Praxis.Domain.Catalog;
using Praxis.Domain.Identity;
using Praxis.Domain.Inventory;
using Praxis.Domain.Operations;
using Praxis.Domain.Purchasing;
using Praxis.Domain.Sales;
using Praxis.Infrastructure.Security;

namespace Praxis.Infrastructure.Persistence;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminFullName { get; set; } = "Praxis Administrator";
    public string AdminEmail { get; set; } = "admin@praxis.local";
    public string AdminPassword { get; set; } = "Admin@12345";
}

public static class DatabaseBootstrap
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PraxisDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<AppSeeder>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        await seeder.SeedAsync(cancellationToken);
    }
}

public sealed partial class AppSeeder(
    PraxisDbContext dbContext,
    IPasswordHasher passwordHasher,
    IClock clock,
    IOptions<SeedOptions> options)
{
    private readonly SeedOptions _options = options.Value;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingPermissions = await dbContext.Permissions.ToDictionaryAsync(permission => permission.Code, cancellationToken);
        foreach (var permissionCode in PermissionCatalog.All)
        {
            if (!existingPermissions.ContainsKey(permissionCode))
            {
                dbContext.Permissions.Add(new Permission(permissionCode, $"Permission {permissionCode}"));
            }
        }

        var desiredRoles = new Dictionary<string, string>
        {
            ["administrator"] = "Full access role",
            ["sales-manager"] = "Sales and customer operations",
            ["inventory-manager"] = "Inventory and catalog operations",
            ["finance-manager"] = "Billing and financial operations",
            ["operator"] = "Operational access"
        };

        var existingRoles = await dbContext.Roles.ToDictionaryAsync(role => role.Name, cancellationToken);
        foreach (var desiredRole in desiredRoles)
        {
            if (!existingRoles.ContainsKey(desiredRole.Key))
            {
                dbContext.Roles.Add(new Role(desiredRole.Key, desiredRole.Value));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var permissions = await dbContext.Permissions.AsNoTracking().ToListAsync(cancellationToken);
        var roles = await dbContext.Roles.AsNoTracking().ToDictionaryAsync(role => role.Name, cancellationToken);

        AddRolePermissions(roles["administrator"].Id, permissions.Select(permission => permission.Code).ToArray(), permissions);
        AddRolePermissions(roles["sales-manager"].Id, [PermissionCatalog.CatalogRead, PermissionCatalog.CustomerRead, PermissionCatalog.CustomerWrite, PermissionCatalog.SalesRead, PermissionCatalog.SalesWrite, PermissionCatalog.InventoryRead, PermissionCatalog.BillingRead, PermissionCatalog.BillingWrite, PermissionCatalog.DashboardRead, PermissionCatalog.ReportingRead, PermissionCatalog.NotificationRead, PermissionCatalog.NotificationWrite], permissions);
        AddRolePermissions(roles["inventory-manager"].Id, [PermissionCatalog.CatalogRead, PermissionCatalog.CatalogWrite, PermissionCatalog.PurchasingRead, PermissionCatalog.PurchasingWrite, PermissionCatalog.InventoryRead, PermissionCatalog.InventoryWrite, PermissionCatalog.DashboardRead, PermissionCatalog.ReportingRead, PermissionCatalog.NotificationRead, PermissionCatalog.NotificationWrite], permissions);
        AddRolePermissions(roles["finance-manager"].Id, [PermissionCatalog.BillingRead, PermissionCatalog.BillingWrite, PermissionCatalog.ReportingRead, PermissionCatalog.AuditRead, PermissionCatalog.DashboardRead, PermissionCatalog.NotificationRead, PermissionCatalog.NotificationWrite], permissions);
        AddRolePermissions(roles["operator"].Id, [PermissionCatalog.CatalogRead, PermissionCatalog.CustomerRead, PermissionCatalog.CustomerWrite, PermissionCatalog.SalesRead, PermissionCatalog.SalesWrite, PermissionCatalog.InventoryRead, PermissionCatalog.PurchasingRead, PermissionCatalog.BillingRead, PermissionCatalog.DashboardRead, PermissionCatalog.NotificationRead, PermissionCatalog.NotificationWrite], permissions);

        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedBranchesAsync(cancellationToken);
        await SeedWarehousesV3Async(cancellationToken);
        await SeedFeatureFlagsAsync(cancellationToken);
        await SeedApprovalRulesAsync(cancellationToken);

        var adminRole = await dbContext.Roles.FirstAsync(role => role.Name == "administrator", cancellationToken);
        var adminUser = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == _options.AdminEmail.ToLowerInvariant(), cancellationToken);

        if (adminUser is null)
        {
            var user = new User(_options.AdminFullName, _options.AdminEmail, passwordHasher.Hash(_options.AdminPassword), adminRole.Id);
            user.SetCreatedAt(clock.UtcNow);
            dbContext.Users.Add(user);
        }

        if (!await dbContext.Categories.AnyAsync(cancellationToken))
        {
            dbContext.Categories.AddRange(
                CreateCategory("ELEC", "Electronics", "Operational electronic products"),
                CreateCategory("FURN", "Furniture", "Office and store furniture"),
                CreateCategory("PACK", "Packaging", "Packaging and dispatch supplies"));
        }

        if (!await dbContext.Suppliers.AnyAsync(cancellationToken))
        {
            dbContext.Suppliers.AddRange(
                CreateSupplier("SUP-ACME", "ACME Supply", "Maria Costa", "acme@vendors.local", "+55 11 99999-1000"),
                CreateSupplier("SUP-ALPHA", "Alpha Distribution", "Joao Lima", "alpha@vendors.local", "+55 11 99999-2000"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (!await dbContext.Products.AnyAsync(cancellationToken))
        {
            var categories = await dbContext.Categories.AsNoTracking().ToDictionaryAsync(category => category.Code, cancellationToken);
            var suppliers = await dbContext.Suppliers.AsNoTracking().ToDictionaryAsync(supplier => supplier.Code, cancellationToken);

            dbContext.Products.AddRange(
                CreateProduct("NB-15", "Notebook 15", "Corporate notebook 15 inches", 4500m, 3600m, 5, categories["ELEC"].Id, suppliers["SUP-ACME"].Id),
                CreateProduct("CHAIR-01", "Ergo Chair", "Ergonomic office chair", 980m, 640m, 8, categories["FURN"].Id, suppliers["SUP-ALPHA"].Id),
                CreateProduct("BOX-STD", "Dispatch Box", "Standard dispatch cardboard box", 12m, 5m, 20, categories["PACK"].Id, suppliers["SUP-ACME"].Id));
        }

        await BackfillProductCostsAsync(cancellationToken);

        if (!await dbContext.Customers.AnyAsync(cancellationToken))
        {
            dbContext.Customers.AddRange(
                CreateCustomer("CUS-001", "Studio Orion", "12.345.678/0001-99", "ops@orion.local", "+55 11 99888-1000", CustomerStatus.Active),
                CreateCustomer("CUS-002", "Mercurio Retail", "98.765.432/0001-55", "compras@mercurio.local", "+55 11 99888-2000", CustomerStatus.Active));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedCostCentersAsync(cancellationToken);

        if (!await dbContext.InventoryBalances.AnyAsync(cancellationToken))
        {
            var warehouse = await dbContext.WarehouseLocations.FirstAsync(location => location.IsDefault, cancellationToken);
            var products = await dbContext.Products.AsNoTracking().ToListAsync(cancellationToken);

            foreach (var product in products)
            {
                var balance = new InventoryBalance(product.Id, warehouse.Id);
                balance.SetCreatedAt(clock.UtcNow);
                balance.AddStock(product.Sku == "BOX-STD" ? 12 : 25, clock.UtcNow);
                dbContext.InventoryBalances.Add(balance);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedTransactionalDataAsync(cancellationToken);
        await BackfillCostSnapshotsAsync(cancellationToken);
        await BackfillBranchAssignmentsAsync(cancellationToken);
    }

    private void AddRolePermissions(Guid roleId, IReadOnlyCollection<string> codes, IReadOnlyCollection<Permission> permissions)
    {
        var existingPermissionIds = dbContext.RolePermissions
            .Where(rolePermission => rolePermission.RoleId == roleId)
            .Select(rolePermission => rolePermission.PermissionId)
            .ToHashSet();

        foreach (var code in codes)
        {
            var permission = permissions.First(current => current.Code == code);
            if (existingPermissionIds.Add(permission.Id))
            {
                dbContext.RolePermissions.Add(new RolePermission(roleId, permission.Id));
            }
        }
    }

    private Category CreateCategory(string code, string name, string description)
    {
        var category = new Category(code, name, description);
        category.SetCreatedAt(clock.UtcNow);
        return category;
    }

    private Supplier CreateSupplier(string code, string name, string contactName, string email, string phone)
    {
        var supplier = new Supplier(code, name, contactName, email, phone);
        supplier.SetCreatedAt(clock.UtcNow);
        return supplier;
    }

    private Product CreateProduct(string sku, string name, string description, decimal unitPrice, decimal standardCost, int reorderLevel, Guid categoryId, Guid supplierId)
    {
        var product = new Product(sku, name, description, unitPrice, standardCost, reorderLevel, categoryId, supplierId);
        product.SetCreatedAt(clock.UtcNow);
        return product;
    }

    private Customer CreateCustomer(string code, string name, string document, string email, string phone, CustomerStatus status)
    {
        var customer = new Customer(code, name, document, email, phone, status);
        customer.SetCreatedAt(clock.UtcNow);
        return customer;
    }

    private async Task SeedTransactionalDataAsync(CancellationToken cancellationToken)
    {
        var headquarters = await dbContext.Branches.AsNoTracking().FirstAsync(branch => branch.IsHeadquarters, cancellationToken);
        var defaultCostCenter = await dbContext.CostCenters.AsNoTracking().FirstAsync(costCenter => costCenter.BranchId == headquarters.Id, cancellationToken);

        if (!await dbContext.SalesOrders.AnyAsync(cancellationToken))
        {
            var now = clock.UtcNow;
            var warehouse = await dbContext.WarehouseLocations.FirstAsync(location => location.IsDefault, cancellationToken);
            var customer = await dbContext.Customers.OrderBy(current => current.Name).FirstAsync(cancellationToken);
            var secondCustomer = await dbContext.Customers.OrderBy(current => current.Name).Skip(1).FirstOrDefaultAsync(cancellationToken) ?? customer;
            var notebook = await dbContext.Products.FirstAsync(product => product.Sku == "NB-15", cancellationToken);
            var chair = await dbContext.Products.FirstAsync(product => product.Sku == "CHAIR-01", cancellationToken);

            var dispatchedOrder = new SalesOrder("SO-SEED-001", customer.Id, warehouse.Id, headquarters.Id, defaultCostCenter.Id, "Seed dispatched order");
            dispatchedOrder.SetCreatedAt(now.AddDays(-5));
            dispatchedOrder.AddItem(notebook.Id, notebook.Sku, notebook.Name, 1, notebook.UnitPrice, notebook.StandardCost);
            dispatchedOrder.Approve(now.AddDays(-4));

            var reservation = new StockReservation(dispatchedOrder.Id, notebook.Id, warehouse.Id, 1);
            reservation.SetCreatedAt(now.AddDays(-4));
            dbContext.StockReservations.Add(reservation);

            var balance = await dbContext.InventoryBalances.FirstAsync(current => current.ProductId == notebook.Id && current.WarehouseLocationId == warehouse.Id, cancellationToken);
            balance.Reserve(1, now.AddDays(-4));
            dbContext.StockMovements.Add(new StockMovement(notebook.Id, warehouse.Id, StockMovementType.Reservation, 1, "Seed sales reservation", "seed", dispatchedOrder.Id, null));

            balance.DispatchReserved(1, now.AddDays(-3));
            reservation.Consume(now.AddDays(-3));
            dbContext.StockMovements.Add(new StockMovement(notebook.Id, warehouse.Id, StockMovementType.Outbound, 1, "Seed sales dispatch", "seed", dispatchedOrder.Id, null));
            dispatchedOrder.Dispatch(now.AddDays(-3));

            var draftOrder = new SalesOrder("SO-SEED-002", secondCustomer.Id, warehouse.Id, headquarters.Id, defaultCostCenter.Id, "Seed draft order");
            draftOrder.SetCreatedAt(now.AddDays(-1));
            draftOrder.AddItem(chair.Id, chair.Sku, chair.Name, 2, chair.UnitPrice, chair.StandardCost);

            dbContext.SalesOrders.AddRange(dispatchedOrder, draftOrder);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.PurchaseOrders.AnyAsync(cancellationToken))
        {
            var now = clock.UtcNow;
            var warehouse = await dbContext.WarehouseLocations.FirstAsync(location => location.IsDefault, cancellationToken);
            var supplier = await dbContext.Suppliers.OrderBy(current => current.Name).FirstAsync(cancellationToken);
            var box = await dbContext.Products.FirstAsync(product => product.Sku == "BOX-STD", cancellationToken);
            var balance = await dbContext.InventoryBalances.FirstAsync(current => current.ProductId == box.Id && current.WarehouseLocationId == warehouse.Id, cancellationToken);

            var purchaseOrder = new PurchaseOrder("PO-SEED-001", supplier.Id, warehouse.Id, headquarters.Id, defaultCostCenter.Id, now.AddDays(-2), "Seed purchase replenishment");
            purchaseOrder.SetCreatedAt(now.AddDays(-7));
            purchaseOrder.AddItem(box.Id, box.Sku, box.Name, 50, 4.50m);
            purchaseOrder.Approve(now.AddDays(-6));

            var orderItem = purchaseOrder.Items.First();
            var receipt = new PurchaseReceipt(purchaseOrder.Id, "RCV-SEED-001", now.AddDays(-5), "Seed full receipt", null);
            receipt.SetCreatedAt(now.AddDays(-5));
            orderItem.RegisterReceipt(50, now.AddDays(-5));
            receipt.AddItem(box.Id, box.Sku, box.Name, 50, 4.50m);
            balance.AddStock(50, now.AddDays(-5));
            box.UpdateStandardCost(4.50m, now.AddDays(-5));
            purchaseOrder.RegisterReceipt(receipt, now.AddDays(-5));

            var payable = new Payable(supplier.Id, purchaseOrder.Id, receipt.Id, headquarters.Id, defaultCostCenter.Id, "RCV-SEED-001-AP", "Seed supplier payable", now.AddDays(-5), now.AddDays(15), receipt.TotalAmount);
            payable.SetCreatedAt(now.AddDays(-5));

            dbContext.PurchaseOrders.Add(purchaseOrder);
            dbContext.PurchaseReceipts.Add(receipt);
            dbContext.Payables.Add(payable);
            dbContext.StockMovements.Add(new StockMovement(box.Id, warehouse.Id, StockMovementType.Inbound, 50, "Seed purchase receipt", "seed", purchaseOrder.Id, null));

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Invoices.AnyAsync(cancellationToken))
        {
            var now = clock.UtcNow;
            var dispatchedOrder = await dbContext.SalesOrders
                .Include(order => order.Items)
                .Include(order => order.Customer)
                .FirstAsync(order => order.Status == SalesOrderStatus.Dispatched, cancellationToken);

            var invoice = new Invoice("INV-SEED-001", dispatchedOrder.Id, dispatchedOrder.CustomerId, dispatchedOrder.BranchId, dispatchedOrder.CostCenterId, now.AddDays(-3), now.AddDays(7), "Seed invoice");
            invoice.SetCreatedAt(now.AddDays(-3));
            foreach (var item in dispatchedOrder.Items)
            {
                invoice.AddItem(item.ProductId, item.Sku, item.ProductName, item.Quantity, item.UnitPrice, item.UnitCost);
            }

            var receivable = new Receivable(dispatchedOrder.CustomerId, invoice.Id, dispatchedOrder.BranchId, dispatchedOrder.CostCenterId, invoice.InvoiceNumber, $"Seed receivable for {dispatchedOrder.OrderNumber}", now.AddDays(-3), now.AddDays(7), invoice.TotalAmount);
            receivable.SetCreatedAt(now.AddDays(-3));

            dbContext.Invoices.Add(invoice);
            dbContext.Receivables.Add(receivable);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task BackfillProductCostsAsync(CancellationToken cancellationToken)
    {
        var costMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["NB-15"] = 3600m,
            ["CHAIR-01"] = 640m,
            ["BOX-STD"] = 5m
        };

        var products = await dbContext.Products.ToListAsync(cancellationToken);
        var changed = false;

        foreach (var product in products)
        {
            if (product.StandardCost > 0)
            {
                continue;
            }

            var standardCost = costMap.TryGetValue(product.Sku, out var mappedCost)
                ? mappedCost
                : Math.Round(product.UnitPrice * 0.60m, 2);

            product.UpdateStandardCost(standardCost, clock.UtcNow);
            changed = true;
        }

        if (changed)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task BackfillCostSnapshotsAsync(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        var now = clock.UtcNow;
        var salesOrderItems = await dbContext.SalesOrderItems
            .Where(item => item.UnitCost <= 0)
            .ToListAsync(cancellationToken);

        foreach (var item in salesOrderItems)
        {
            if (products.TryGetValue(item.ProductId, out var product))
            {
                item.UpdateCostSnapshot(product.StandardCost, now);
            }
        }

        var invoiceItems = await dbContext.InvoiceItems
            .Where(item => item.UnitCost <= 0)
            .ToListAsync(cancellationToken);

        foreach (var item in invoiceItems)
        {
            if (products.TryGetValue(item.ProductId, out var product))
            {
                item.UpdateCostSnapshot(product.StandardCost, now);
            }
        }

        if (salesOrderItems.Count > 0 || invoiceItems.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

public sealed class PraxisDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PraxisDbContext>
{
    public PraxisDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__PraxisDatabase")
            ?? "Host=localhost;Port=5432;Database=praxis_db;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<PraxisDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PraxisDbContext(optionsBuilder.Options);
    }
}
