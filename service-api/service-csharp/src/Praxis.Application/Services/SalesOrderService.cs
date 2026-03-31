using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Common;
using Praxis.Application.Models;
using Praxis.Application.Persistence;
using Praxis.Domain.Inventory;
using Praxis.Domain.Operations;
using Praxis.Domain.Sales;

namespace Praxis.Application.Services;

public sealed class SalesOrderService(
    IPraxisDbContext dbContext,
    ICurrentUserService currentUserService,
    IEventBus eventBus,
    IDashboardCache dashboardCache,
    PlatformPolicyService platformPolicyService)
{
    public async Task<IReadOnlyCollection<SalesOrderResponse>> ListAsync(
        Guid? branchId = null,
        Guid? costCenterId = null,
        ApprovalWorkflowStatus? approvalStatus = null,
        CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Sales, branchId, cancellationToken);

        var query = dbContext.SalesOrders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.Items)
            .AsQueryable();

        if (branchId.HasValue)
        {
            query = query.Where(order => order.BranchId == branchId);
        }

        if (costCenterId.HasValue)
        {
            query = query.Where(order => order.CostCenterId == costCenterId);
        }

        if (approvalStatus.HasValue)
        {
            query = query.Where(order => order.ApprovalStatus == approvalStatus.Value);
        }

        var orders = await query
            .OrderByDescending(order => order.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return await MapOrdersAsync(orders, cancellationToken);
    }

    public async Task<SalesOrderResponse> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .AsNoTracking()
            .Include(current => current.Customer)
            .Include(current => current.Items)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Sales order not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Sales, order.BranchId, cancellationToken);

        return await MapOrderAsync(order, cancellationToken);
    }

    public async Task<SalesOrderResponse> CreateAsync(CreateSalesOrderRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Sales, request.BranchId, cancellationToken);

        if (request.Items.Count == 0)
        {
            throw new ValidationException("Sales order must contain at least one item.");
        }

        var customer = await dbContext.Customers.FirstOrDefaultAsync(current => current.Id == request.CustomerId, cancellationToken)
            ?? throw new ValidationException("Customer does not exist.");

        var branch = await dbContext.Branches.FirstOrDefaultAsync(current => current.Id == request.BranchId, cancellationToken)
            ?? throw new ValidationException("Branch does not exist.");

        CostCenter? costCenter = null;
        if (request.CostCenterId.HasValue)
        {
            costCenter = await dbContext.CostCenters.FirstOrDefaultAsync(current => current.Id == request.CostCenterId.Value, cancellationToken)
                ?? throw new ValidationException("Cost center does not exist.");

            if (costCenter.BranchId != request.BranchId)
            {
                throw new ValidationException("Cost center does not belong to the selected branch.");
            }
        }

        var warehouse = await dbContext.WarehouseLocations.FirstOrDefaultAsync(current => current.Id == request.WarehouseLocationId, cancellationToken)
            ?? throw new ValidationException("Warehouse does not exist.");

        if (warehouse.BranchId != request.BranchId)
        {
            throw new ValidationException("Warehouse does not belong to the selected branch.");
        }

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToArray();
        var products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Length)
        {
            throw new ValidationException("One or more products do not exist.");
        }

        var orderNumber = $"SO-{utcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..19].ToUpperInvariant();
        var order = new SalesOrder(orderNumber, request.CustomerId, request.WarehouseLocationId, request.BranchId, request.CostCenterId, request.Notes);
        order.SetCreatedAt(utcNow);

        foreach (var item in request.Items)
        {
            var product = products.First(product => product.Id == item.ProductId);
            order.AddItem(product.Id, product.Sku, product.Name, item.Quantity, product.UnitPrice, product.StandardCost);
        }

        dbContext.SalesOrders.Add(order);

        var approvalRule = await platformPolicyService.FindRequiredApprovalRuleAsync(
            ApprovalModule.SalesOrder,
            order.TotalAmount,
            request.BranchId,
            cancellationToken);

        var actor = currentUserService.GetCurrentUser();

        if (approvalRule is not null)
        {
            order.RequireApproval(approvalRule.Id, utcNow);

            var decision = new ApprovalDecision(
                ApprovalModule.SalesOrder,
                order.Id,
                approvalRule.Id,
                order.OrderNumber,
                order.TotalAmount,
                request.BranchId,
                request.CostCenterId,
                actor.UserId,
                actor.Name,
                request.Notes,
                utcNow);

            decision.SetCreatedAt(utcNow);
            dbContext.ApprovalDecisions.Add(decision);
        }

        dbContext.AuditEntries.Add(new AuditEntry(
            "sales.order.created",
            nameof(SalesOrder),
            order.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new
            {
                order.OrderNumber,
                order.CustomerId,
                order.BranchId,
                order.CostCenterId,
                order.WarehouseLocationId,
                order.TotalAmount,
                order.ApprovalStatus
            })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        if (approvalRule is not null)
        {
            await eventBus.PublishAsync(
                "sales.order.pending-approval",
                new
                {
                    order.Id,
                    order.OrderNumber,
                    order.BranchId,
                    order.CostCenterId,
                    CustomerName = customer.Name,
                    BranchName = branch.Name,
                    CostCenterName = costCenter?.Name,
                    order.TotalAmount,
                    approvalRule.RequiredRoleName
                },
                cancellationToken);
        }

        return await MapOrderAsync(order, customer.Name, branch.Name, warehouse.Name, costCenter?.Name, cancellationToken);
    }

    public async Task<SalesOrderResponse> ApproveAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(current => current.Customer)
            .Include(current => current.Items)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Sales order not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Sales, order.BranchId, cancellationToken);

        var warehouse = await dbContext.WarehouseLocations.FirstAsync(current => current.Id == order.WarehouseLocationId, cancellationToken);
        var branchName = order.BranchId.HasValue
            ? await dbContext.Branches
                .Where(branch => branch.Id == order.BranchId.Value)
                .Select(branch => branch.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var costCenterName = order.CostCenterId.HasValue
            ? await dbContext.CostCenters
                .Where(costCenter => costCenter.Id == order.CostCenterId.Value)
                .Select(costCenter => costCenter.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        if (order.ApprovalStatus == ApprovalWorkflowStatus.Pending)
        {
            var approvalRule = order.ApprovalRuleId.HasValue
                ? await dbContext.ApprovalRules.FirstAsync(rule => rule.Id == order.ApprovalRuleId.Value, cancellationToken)
                : throw new ConflictException("Approval rule was not found for this order.");

            var currentUser = currentUserService.GetCurrentUser();
            platformPolicyService.EnsureUserCanApprove(approvalRule, currentUser);

            order.MarkApprovalGranted(approvalRule.Id, currentUser.UserId, currentUser.Name, utcNow);

            var decision = await dbContext.ApprovalDecisions
                .Where(current => current.Module == ApprovalModule.SalesOrder && current.EntityId == order.Id && current.Status == ApprovalDecisionStatus.Pending)
                .OrderByDescending(current => current.RequestedAtUtc)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new ConflictException("Approval decision was not found for this order.");

            decision.Approve(currentUser.UserId, currentUser.Name, "Approved from sales flow", utcNow);
        }

        var itemProductIds = order.Items.Select(item => item.ProductId).ToArray();

        var products = await dbContext.Products
            .Where(product => itemProductIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        var balances = await dbContext.InventoryBalances
            .Where(balance => balance.WarehouseLocationId == order.WarehouseLocationId && itemProductIds.Contains(balance.ProductId))
            .ToListAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            var balance = balances.FirstOrDefault(current => current.ProductId == item.ProductId)
                ?? throw new ConflictException($"There is no inventory balance for {item.ProductName}.");

            balance.Reserve(item.Quantity, utcNow);

            var reservation = new StockReservation(order.Id, item.ProductId, order.WarehouseLocationId, item.Quantity);
            reservation.SetCreatedAt(utcNow);
            dbContext.StockReservations.Add(reservation);

            dbContext.StockMovements.Add(new StockMovement(
                item.ProductId,
                order.WarehouseLocationId,
                StockMovementType.Reservation,
                item.Quantity,
                $"Reservation for order {order.OrderNumber}",
                "sales-order",
                order.Id,
                currentUserService.GetCurrentUser().UserId));
        }

        order.Approve(utcNow);

        var actor = currentUserService.GetCurrentUser();
        dbContext.AuditEntries.Add(new AuditEntry(
            "sales.order.approved",
            nameof(SalesOrder),
            order.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new { order.OrderNumber, order.TotalAmount, order.Status, order.ApprovalStatus })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        await eventBus.PublishAsync(
            "sales.order.approved",
            new
            {
                order.Id,
                order.OrderNumber,
                order.BranchId,
                order.CostCenterId,
                CustomerName = order.Customer.Name,
                BranchName = branchName,
                CostCenterName = costCenterName,
                WarehouseName = warehouse.Name,
                order.TotalAmount
            },
            cancellationToken);

        foreach (var item in order.Items)
        {
            var product = products[item.ProductId];
            var balance = balances.First(current => current.ProductId == item.ProductId);

            if (balance.AvailableQuantity <= product.ReorderLevel)
            {
                await eventBus.PublishAsync(
                    "inventory.low-stock.detected",
                    new
                    {
                        ProductId = product.Id,
                        product.Sku,
                        ProductName = product.Name,
                        BranchId = order.BranchId,
                        WarehouseId = warehouse.Id,
                        WarehouseName = warehouse.Name,
                        balance.AvailableQuantity,
                        product.ReorderLevel
                    },
                    cancellationToken);
            }
        }

        return await MapOrderAsync(order, order.Customer.Name, branchName, warehouse.Name, costCenterName, cancellationToken);
    }

    public async Task<SalesOrderResponse> RejectAsync(Guid id, string? notes, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(current => current.Customer)
            .Include(current => current.Items)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Sales order not found.");

        if (order.ApprovalStatus != ApprovalWorkflowStatus.Pending)
        {
            throw new ConflictException("Only pending approvals can be rejected.");
        }

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Sales, order.BranchId, cancellationToken);

        var approvalRule = order.ApprovalRuleId.HasValue
            ? await dbContext.ApprovalRules.FirstAsync(rule => rule.Id == order.ApprovalRuleId.Value, cancellationToken)
            : throw new ConflictException("Approval rule was not found for this order.");

        var currentUser = currentUserService.GetCurrentUser();
        platformPolicyService.EnsureUserCanApprove(approvalRule, currentUser);

        var decision = await dbContext.ApprovalDecisions
            .Where(current => current.Module == ApprovalModule.SalesOrder && current.EntityId == order.Id && current.Status == ApprovalDecisionStatus.Pending)
            .OrderByDescending(current => current.RequestedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException("Approval decision was not found for this order.");

        decision.Reject(currentUser.UserId, currentUser.Name, notes, utcNow);
        order.MarkApprovalRejected(notes, utcNow);
        order.Cancel($"Rejected in approval flow. {notes}".Trim(), utcNow);

        dbContext.AuditEntries.Add(new AuditEntry(
            "sales.order.rejected",
            nameof(SalesOrder),
            order.Id.ToString(),
            currentUser.UserId,
            currentUser.Name,
            JsonSerializer.Serialize(new { order.OrderNumber, order.TotalAmount, notes })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        await eventBus.PublishAsync(
            "sales.order.rejected",
            new
            {
                order.Id,
                order.OrderNumber,
                order.BranchId,
                order.CostCenterId,
                order.TotalAmount,
                Notes = notes
            },
            cancellationToken);

        return await MapOrderAsync(order, cancellationToken);
    }

    public async Task<SalesOrderResponse> DispatchAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(current => current.Customer)
            .Include(current => current.Items)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Sales order not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Sales, order.BranchId, cancellationToken);

        var warehouse = await dbContext.WarehouseLocations.FirstAsync(current => current.Id == order.WarehouseLocationId, cancellationToken);
        var branchName = order.BranchId.HasValue
            ? await dbContext.Branches.Where(branch => branch.Id == order.BranchId.Value).Select(branch => branch.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        var costCenterName = order.CostCenterId.HasValue
            ? await dbContext.CostCenters.Where(costCenter => costCenter.Id == order.CostCenterId.Value).Select(costCenter => costCenter.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        var itemProductIds = order.Items.Select(item => item.ProductId).ToArray();

        var balances = await dbContext.InventoryBalances
            .Where(balance => balance.WarehouseLocationId == order.WarehouseLocationId && itemProductIds.Contains(balance.ProductId))
            .ToListAsync(cancellationToken);

        var reservations = await dbContext.StockReservations
            .Where(reservation => reservation.SalesOrderId == order.Id && reservation.Status == ReservationStatus.Active)
            .ToListAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            var balance = balances.FirstOrDefault(current => current.ProductId == item.ProductId)
                ?? throw new ConflictException($"There is no inventory balance for {item.ProductName}.");

            var reservation = reservations.FirstOrDefault(current => current.ProductId == item.ProductId)
                ?? throw new ConflictException($"There is no active reservation for {item.ProductName}.");

            balance.DispatchReserved(item.Quantity, utcNow);
            reservation.Consume(utcNow);

            dbContext.StockMovements.Add(new StockMovement(
                item.ProductId,
                order.WarehouseLocationId,
                StockMovementType.Outbound,
                item.Quantity,
                $"Dispatch for order {order.OrderNumber}",
                "sales-order",
                order.Id,
                currentUserService.GetCurrentUser().UserId));
        }

        order.Dispatch(utcNow);

        var actor = currentUserService.GetCurrentUser();
        dbContext.AuditEntries.Add(new AuditEntry(
            "sales.order.dispatched",
            nameof(SalesOrder),
            order.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new { order.OrderNumber, order.TotalAmount, order.Status })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        await eventBus.PublishAsync(
            "sales.order.dispatched",
            new
            {
                order.Id,
                order.OrderNumber,
                order.BranchId,
                order.CostCenterId,
                CustomerName = order.Customer.Name,
                BranchName = branchName,
                CostCenterName = costCenterName,
                WarehouseName = warehouse.Name,
                order.TotalAmount
            },
            cancellationToken);

        return await MapOrderAsync(order, order.Customer.Name, branchName, warehouse.Name, costCenterName, cancellationToken);
    }

    public async Task<SalesOrderResponse> CancelAsync(Guid id, string? notes, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.SalesOrders
            .Include(current => current.Customer)
            .Include(current => current.Items)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Sales order not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Sales, order.BranchId, cancellationToken);

        if (order.Status == SalesOrderStatus.Approved)
        {
            var itemProductIds = order.Items.Select(item => item.ProductId).ToArray();
            var balances = await dbContext.InventoryBalances
                .Where(balance => balance.WarehouseLocationId == order.WarehouseLocationId && itemProductIds.Contains(balance.ProductId))
                .ToListAsync(cancellationToken);

            var reservations = await dbContext.StockReservations
                .Where(reservation => reservation.SalesOrderId == order.Id && reservation.Status == ReservationStatus.Active)
                .ToListAsync(cancellationToken);

            foreach (var item in order.Items)
            {
                var balance = balances.FirstOrDefault(current => current.ProductId == item.ProductId);
                var reservation = reservations.FirstOrDefault(current => current.ProductId == item.ProductId);

                if (balance is null || reservation is null)
                {
                    continue;
                }

                balance.ReleaseReservation(item.Quantity, utcNow);
                reservation.Release(utcNow);

                dbContext.StockMovements.Add(new StockMovement(
                    item.ProductId,
                    order.WarehouseLocationId,
                    StockMovementType.ReservationRelease,
                    item.Quantity,
                    $"Reservation release for order {order.OrderNumber}",
                    "sales-order",
                    order.Id,
                    currentUserService.GetCurrentUser().UserId));
            }
        }

        order.Cancel(notes, utcNow);

        var actor = currentUserService.GetCurrentUser();
        dbContext.AuditEntries.Add(new AuditEntry(
            "sales.order.cancelled",
            nameof(SalesOrder),
            order.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new { order.OrderNumber, order.TotalAmount, order.Status, notes })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        return await MapOrderAsync(order, cancellationToken);
    }

    private async Task<IReadOnlyCollection<SalesOrderResponse>> MapOrdersAsync(IReadOnlyCollection<SalesOrder> orders, CancellationToken cancellationToken)
    {
        var customerIds = orders.Select(order => order.CustomerId).Distinct().ToArray();
        var branchIds = orders.Where(order => order.BranchId.HasValue).Select(order => order.BranchId!.Value).Distinct().ToArray();
        var costCenterIds = orders.Where(order => order.CostCenterId.HasValue).Select(order => order.CostCenterId!.Value).Distinct().ToArray();
        var warehouseIds = orders.Select(order => order.WarehouseLocationId).Distinct().ToArray();

        var customers = await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customerIds.Contains(customer.Id))
            .ToDictionaryAsync(customer => customer.Id, customer => customer.Name, cancellationToken);

        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        var costCenters = await dbContext.CostCenters
            .AsNoTracking()
            .Where(costCenter => costCenterIds.Contains(costCenter.Id))
            .ToDictionaryAsync(costCenter => costCenter.Id, costCenter => costCenter.Name, cancellationToken);

        var warehouses = await dbContext.WarehouseLocations
            .AsNoTracking()
            .Where(warehouse => warehouseIds.Contains(warehouse.Id))
            .ToDictionaryAsync(warehouse => warehouse.Id, warehouse => warehouse.Name, cancellationToken);

        return orders
            .Select(order => ToResponse(
                order,
                customers.TryGetValue(order.CustomerId, out var customerName) ? customerName : "Customer",
                order.BranchId.HasValue && branches.TryGetValue(order.BranchId.Value, out var branchName) ? branchName : null,
                warehouses.TryGetValue(order.WarehouseLocationId, out var warehouseName) ? warehouseName : "Warehouse",
                order.CostCenterId.HasValue && costCenters.TryGetValue(order.CostCenterId.Value, out var costCenterName) ? costCenterName : null))
            .ToArray();
    }

    private async Task<SalesOrderResponse> MapOrderAsync(SalesOrder order, CancellationToken cancellationToken)
    {
        var customerName = await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == order.CustomerId)
            .Select(customer => customer.Name)
            .FirstAsync(cancellationToken);

        var branchName = order.BranchId.HasValue
            ? await dbContext.Branches
                .AsNoTracking()
                .Where(branch => branch.Id == order.BranchId.Value)
                .Select(branch => branch.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var costCenterName = order.CostCenterId.HasValue
            ? await dbContext.CostCenters
                .AsNoTracking()
                .Where(costCenter => costCenter.Id == order.CostCenterId.Value)
                .Select(costCenter => costCenter.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var warehouseName = await dbContext.WarehouseLocations
            .AsNoTracking()
            .Where(warehouse => warehouse.Id == order.WarehouseLocationId)
            .Select(warehouse => warehouse.Name)
            .FirstAsync(cancellationToken);

        return ToResponse(order, customerName, branchName, warehouseName, costCenterName);
    }

    private Task<SalesOrderResponse> MapOrderAsync(
        SalesOrder order,
        string customerName,
        string? branchName,
        string warehouseName,
        string? costCenterName,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ToResponse(order, customerName, branchName, warehouseName, costCenterName));
    }

    private static SalesOrderResponse ToResponse(
        SalesOrder order,
        string customerName,
        string? branchName,
        string warehouseName,
        string? costCenterName)
    {
        return new SalesOrderResponse(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            customerName,
            order.BranchId,
            branchName,
            order.CostCenterId,
            costCenterName,
            order.WarehouseLocationId,
            warehouseName,
            order.Status,
            order.ApprovalStatus,
            order.TotalAmount,
            order.Notes,
            order.CreatedAtUtc,
            order.Items
                .OrderBy(item => item.ProductName)
                .Select(item => new SalesOrderItemResponse(item.ProductId, item.Sku, item.ProductName, item.Quantity, item.UnitPrice, item.LineTotal))
                .ToArray());
    }
}
