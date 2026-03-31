using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Common;
using Praxis.Application.Models;
using Praxis.Application.Persistence;
using Praxis.Domain.Inventory;
using Praxis.Domain.Operations;

namespace Praxis.Application.Services;

public sealed class InventoryService(
    IPraxisDbContext dbContext,
    ICurrentUserService currentUserService,
    IEventBus eventBus,
    IDashboardCache dashboardCache,
    PlatformPolicyService platformPolicyService)
{
    public async Task<IReadOnlyCollection<WarehouseResponse>> ListWarehousesAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Inventory, branchId, cancellationToken);

        var warehouses = await dbContext.WarehouseLocations
            .AsNoTracking()
            .Where(location => !branchId.HasValue || location.BranchId == branchId)
            .OrderByDescending(location => location.IsDefault)
            .ThenBy(location => location.Name)
            .ToListAsync(cancellationToken);

        var branchIds = warehouses.Where(location => location.BranchId.HasValue).Select(location => location.BranchId!.Value).Distinct().ToArray();
        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        return warehouses
            .Select(location => new WarehouseResponse(
                location.Id,
                location.BranchId,
                location.BranchId.HasValue && branches.TryGetValue(location.BranchId.Value, out var branchName) ? branchName : null,
                location.Code,
                location.Name,
                location.Description,
                location.IsDefault))
            .ToArray();
    }

    public async Task<IReadOnlyCollection<InventoryBalanceResponse>> ListBalancesAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Inventory, branchId, cancellationToken);

        return await dbContext.InventoryBalances
            .AsNoTracking()
            .Join(
                dbContext.Products.AsNoTracking(),
                balance => balance.ProductId,
                product => product.Id,
                (balance, product) => new { balance, product })
            .Join(
                dbContext.WarehouseLocations.AsNoTracking(),
                combined => combined.balance.WarehouseLocationId,
                warehouse => warehouse.Id,
                (combined, warehouse) => new { combined.balance, combined.product, warehouse })
            .Where(entry => !branchId.HasValue || entry.warehouse.BranchId == branchId)
            .Join(
                dbContext.Branches.AsNoTracking(),
                entry => entry.warehouse.BranchId,
                branch => branch.Id,
                (entry, branch) => new { entry.balance, entry.product, entry.warehouse, BranchName = branch.Name })
            .Select(entry => new InventoryBalanceResponse(
                entry.product.Id,
                entry.product.Name,
                entry.product.Sku,
                entry.warehouse.BranchId,
                entry.BranchName,
                entry.warehouse.Id,
                entry.warehouse.Name,
                entry.balance.OnHandQuantity,
                entry.balance.ReservedQuantity,
                entry.balance.OnHandQuantity - entry.balance.ReservedQuantity,
                entry.product.ReorderLevel))
            .OrderBy(entry => entry.ProductName)
            .ToListAsync(cancellationToken);
    }

    public async Task<InventoryBalanceResponse> AdjustAsync(AdjustInventoryRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        if (request.QuantityDelta == 0)
        {
            throw new ValidationException("Inventory adjustment cannot be zero.");
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(current => current.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        var warehouse = await dbContext.WarehouseLocations.FirstOrDefaultAsync(current => current.Id == request.WarehouseLocationId, cancellationToken)
            ?? throw new NotFoundException("Warehouse not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Inventory, warehouse.BranchId, cancellationToken);

        var balance = await dbContext.InventoryBalances
            .FirstOrDefaultAsync(current =>
                current.ProductId == request.ProductId &&
                current.WarehouseLocationId == request.WarehouseLocationId,
                cancellationToken);

        if (balance is null)
        {
            balance = new InventoryBalance(request.ProductId, request.WarehouseLocationId);
            balance.SetCreatedAt(utcNow);
            dbContext.InventoryBalances.Add(balance);
        }

        balance.AddStock(request.QuantityDelta, utcNow);

        var actor = currentUserService.GetCurrentUser();

        dbContext.StockMovements.Add(new StockMovement(
            request.ProductId,
            request.WarehouseLocationId,
            StockMovementType.Adjustment,
            request.QuantityDelta,
            request.Reason,
            "inventory-adjustment",
            null,
            actor.UserId));

        dbContext.AuditEntries.Add(new AuditEntry(
            "inventory.adjusted",
            nameof(InventoryBalance),
            balance.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new
            {
                request.ProductId,
                request.WarehouseLocationId,
                request.QuantityDelta,
                request.Reason
            })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        var branchName = warehouse.BranchId.HasValue
            ? await dbContext.Branches
                .Where(branch => branch.Id == warehouse.BranchId.Value)
                .Select(branch => branch.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        if (balance.AvailableQuantity <= product.ReorderLevel)
        {
            await eventBus.PublishAsync(
                "inventory.low-stock.detected",
                new
                {
                    ProductId = product.Id,
                    product.Sku,
                    ProductName = product.Name,
                    BranchId = warehouse.BranchId,
                    WarehouseId = warehouse.Id,
                    WarehouseName = warehouse.Name,
                    balance.AvailableQuantity,
                    product.ReorderLevel
                },
                cancellationToken);
        }

        return new InventoryBalanceResponse(
            product.Id,
            product.Name,
            product.Sku,
            warehouse.BranchId,
            branchName,
            warehouse.Id,
            warehouse.Name,
            balance.OnHandQuantity,
            balance.ReservedQuantity,
            balance.AvailableQuantity,
            product.ReorderLevel);
    }

    public async Task<IReadOnlyCollection<StockMovementResponse>> ListMovementsAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Inventory, branchId, cancellationToken);

        return await dbContext.StockMovements
            .AsNoTracking()
            .Join(
                dbContext.Products.AsNoTracking(),
                movement => movement.ProductId,
                product => product.Id,
                (movement, product) => new { movement, product })
            .Join(
                dbContext.WarehouseLocations.AsNoTracking(),
                combined => combined.movement.WarehouseLocationId,
                warehouse => warehouse.Id,
                (combined, warehouse) => new { combined.movement, combined.product, warehouse })
            .Where(entry => !branchId.HasValue || entry.warehouse.BranchId == branchId)
            .Select(entry => new StockMovementResponse(
                entry.movement.Id,
                entry.product.Id,
                entry.product.Name,
                entry.product.Sku,
                entry.warehouse.Id,
                entry.warehouse.Name,
                entry.movement.Type,
                entry.movement.Quantity,
                entry.movement.Reason,
                entry.movement.ReferenceType,
                entry.movement.ReferenceId,
                entry.movement.CreatedAtUtc))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);
    }
}
