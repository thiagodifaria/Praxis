using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Billing;
using Praxis.Domain.Inventory;
using Praxis.Domain.Operations;

namespace Praxis.Infrastructure.Persistence;

public sealed partial class AppSeeder
{
    private async Task SeedBranchesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.Branches.AnyAsync(cancellationToken))
        {
            return;
        }

        var headquarters = CreateBranch("HQ", "Matriz Sao Paulo", "Praxis Holding LTDA", "12.345.678/0001-10", "Sao Paulo", "SP", true);
        var interior = CreateBranch("INT", "Filial Campinas", "Praxis Campinas LTDA", "12.345.678/0002-09", "Campinas", "SP", false);

        dbContext.Branches.AddRange(headquarters, interior);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedWarehousesV3Async(CancellationToken cancellationToken)
    {
        var branches = await dbContext.Branches.AsNoTracking().ToDictionaryAsync(branch => branch.Code, cancellationToken);
        var headquarters = branches["HQ"];
        var interior = branches["INT"];

        if (!await dbContext.WarehouseLocations.AnyAsync(cancellationToken))
        {
            var mainWarehouse = new WarehouseLocation(headquarters.Id, "MAIN", "Main Warehouse", "Primary warehouse for the OMS", true);
            var crossDock = new WarehouseLocation(headquarters.Id, "CROSS", "Cross Dock", "Fast outbound staging area", false);
            var interiorWarehouse = new WarehouseLocation(interior.Id, "INT-OPS", "Interior Operations", "Secondary branch warehouse", false);

            mainWarehouse.SetCreatedAt(clock.UtcNow);
            crossDock.SetCreatedAt(clock.UtcNow);
            interiorWarehouse.SetCreatedAt(clock.UtcNow);

            dbContext.WarehouseLocations.AddRange(mainWarehouse, crossDock, interiorWarehouse);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedCostCentersAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.CostCenters.AnyAsync(cancellationToken))
        {
            return;
        }

        var branches = await dbContext.Branches.AsNoTracking().ToDictionaryAsync(branch => branch.Code, cancellationToken);

        dbContext.CostCenters.AddRange(
            CreateCostCenter(branches["HQ"].Id, "OMS-HQ", "Operacoes Matriz", "Main operational cost center"),
            CreateCostCenter(branches["HQ"].Id, "LOG-HQ", "Logistica Matriz", "Logistics and dispatch center"),
            CreateCostCenter(branches["INT"].Id, "OMS-INT", "Operacoes Interior", "Interior branch operational center"));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedFeatureFlagsAsync(CancellationToken cancellationToken)
    {
        var existingFlags = await dbContext.ModuleFeatureFlags.ToDictionaryAsync(flag => $"{flag.ModuleKey}:{flag.BranchId}", cancellationToken);
        foreach (var moduleKey in ModuleCatalog.All)
        {
            var key = $"{moduleKey}:";
            if (existingFlags.ContainsKey(key))
            {
                continue;
            }

            var flag = new ModuleFeatureFlag(moduleKey, ToDisplayName(moduleKey), $"Global feature flag for {moduleKey}", null, true);
            flag.SetCreatedAt(clock.UtcNow);
            dbContext.ModuleFeatureFlags.Add(flag);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedApprovalRulesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.ApprovalRules.AnyAsync(cancellationToken))
        {
            return;
        }

        var branches = await dbContext.Branches.AsNoTracking().ToDictionaryAsync(branch => branch.Code, cancellationToken);

        var salesRule = new ApprovalRule("Sales orders above 5000", ApprovalModule.SalesOrder, null, 5000m, "sales-manager", "Requires sales management approval for higher value orders.", true);
        var purchaseRule = new ApprovalRule("Purchase orders above 3000", ApprovalModule.PurchaseOrder, null, 3000m, "inventory-manager", "Requires inventory management approval for higher value purchases.", true);
        var interiorPurchaseRule = new ApprovalRule("Interior branch purchase orders above 2000", ApprovalModule.PurchaseOrder, branches["INT"].Id, 2000m, "inventory-manager", "Lower threshold for interior branch purchasing.", true);

        salesRule.SetCreatedAt(clock.UtcNow);
        purchaseRule.SetCreatedAt(clock.UtcNow);
        interiorPurchaseRule.SetCreatedAt(clock.UtcNow);

        dbContext.ApprovalRules.AddRange(salesRule, purchaseRule, interiorPurchaseRule);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task BackfillBranchAssignmentsAsync(CancellationToken cancellationToken)
    {
        var branches = await dbContext.Branches.ToListAsync(cancellationToken);
        if (branches.Count == 0)
        {
            return;
        }

        var headquarterBranch = branches.FirstOrDefault(branch => branch.IsHeadquarters) ?? branches.First();
        var secondaryBranch = branches.FirstOrDefault(branch => branch.Id != headquarterBranch.Id) ?? headquarterBranch;
        var costCenters = await dbContext.CostCenters.ToListAsync(cancellationToken);
        var fallbackCostCenter = costCenters.FirstOrDefault(costCenter => costCenter.BranchId == headquarterBranch.Id);

        var warehouses = await dbContext.WarehouseLocations.ToListAsync(cancellationToken);
        foreach (var warehouse in warehouses.Where(warehouse => !warehouse.BranchId.HasValue))
        {
            var branchId = warehouse.Code.Contains("INT", StringComparison.OrdinalIgnoreCase) ? secondaryBranch.Id : headquarterBranch.Id;
            warehouse.Update(branchId, warehouse.Code, warehouse.Name, warehouse.Description, warehouse.IsDefault, clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var warehouseBranchMap = await dbContext.WarehouseLocations
            .AsNoTracking()
            .ToDictionaryAsync(location => location.Id, location => location.BranchId ?? headquarterBranch.Id, cancellationToken);

        var salesOrders = await dbContext.SalesOrders.ToListAsync(cancellationToken);
        foreach (var order in salesOrders.Where(order => !order.BranchId.HasValue))
        {
            var branchId = warehouseBranchMap.TryGetValue(order.WarehouseLocationId, out var mappedBranchId) ? mappedBranchId : headquarterBranch.Id;
            var costCenterId = costCenters.FirstOrDefault(center => center.BranchId == branchId)?.Id ?? fallbackCostCenter?.Id;
            order.AssignBranchContext(branchId, costCenterId, clock.UtcNow);
        }

        var purchaseOrders = await dbContext.PurchaseOrders.ToListAsync(cancellationToken);
        foreach (var order in purchaseOrders.Where(order => !order.BranchId.HasValue))
        {
            var branchId = warehouseBranchMap.TryGetValue(order.WarehouseLocationId, out var mappedBranchId) ? mappedBranchId : headquarterBranch.Id;
            var costCenterId = costCenters.FirstOrDefault(center => center.BranchId == branchId)?.Id ?? fallbackCostCenter?.Id;
            order.AssignBranchContext(branchId, costCenterId, clock.UtcNow);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var salesOrderMap = salesOrders.ToDictionary(order => order.Id);
        var purchaseOrderMap = purchaseOrders.ToDictionary(order => order.Id);

        var invoices = await dbContext.Invoices.ToListAsync(cancellationToken);
        foreach (var invoice in invoices.Where(invoice => !invoice.BranchId.HasValue && salesOrderMap.TryGetValue(invoice.SalesOrderId, out _)))
        {
            var sourceOrder = salesOrderMap[invoice.SalesOrderId];
            invoice.AssignBranchContext(sourceOrder.BranchId, sourceOrder.CostCenterId, clock.UtcNow);
        }

        var receivables = await dbContext.Receivables.ToListAsync(cancellationToken);
        foreach (var receivable in receivables.Where(receivable => !receivable.BranchId.HasValue))
        {
            var sourceInvoice = invoices.FirstOrDefault(invoice => invoice.Id == receivable.InvoiceId);
            if (sourceInvoice is not null)
            {
                receivable.AssignBranchContext(sourceInvoice.BranchId, sourceInvoice.CostCenterId, clock.UtcNow);
            }
        }

        var payables = await dbContext.Payables.ToListAsync(cancellationToken);
        foreach (var payable in payables.Where(payable => !payable.BranchId.HasValue))
        {
            if (payable.PurchaseOrderId.HasValue && purchaseOrderMap.TryGetValue(payable.PurchaseOrderId.Value, out var sourceOrder))
            {
                payable.AssignBranchContext(sourceOrder.BranchId, sourceOrder.CostCenterId, clock.UtcNow);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Branch CreateBranch(string code, string name, string legalName, string document, string city, string state, bool isHeadquarters)
    {
        var branch = new Branch(code, name, legalName, document, city, state, isHeadquarters, true);
        branch.SetCreatedAt(clock.UtcNow);
        return branch;
    }

    private CostCenter CreateCostCenter(Guid branchId, string code, string name, string description)
    {
        var costCenter = new CostCenter(branchId, code, name, description, true);
        costCenter.SetCreatedAt(clock.UtcNow);
        return costCenter;
    }

    private static string ToDisplayName(string moduleKey) => moduleKey switch
    {
        ModuleCatalog.Dashboard => "Dashboard",
        ModuleCatalog.Catalog => "Catalogo",
        ModuleCatalog.Customers => "Clientes",
        ModuleCatalog.Sales => "Vendas",
        ModuleCatalog.Purchasing => "Compras",
        ModuleCatalog.Inventory => "Estoque",
        ModuleCatalog.Billing => "Faturamento",
        ModuleCatalog.Reporting => "Relatorios",
        ModuleCatalog.Operations => "Operacoes",
        ModuleCatalog.Notifications => "Notificacoes",
        ModuleCatalog.Settings => "Configuracoes",
        _ => moduleKey
    };
}
