using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Common;
using Praxis.Application.Models;
using Praxis.Application.Persistence;
using Praxis.Domain.Billing;
using Praxis.Domain.Inventory;
using Praxis.Domain.Operations;
using Praxis.Domain.Purchasing;

namespace Praxis.Application.Services;

public sealed class PurchasingService(
    IPraxisDbContext dbContext,
    ICurrentUserService currentUserService,
    IEventBus eventBus,
    IDashboardCache dashboardCache,
    PlatformPolicyService platformPolicyService)
{
    public async Task<IReadOnlyCollection<PurchaseOrderResponse>> ListAsync(
        Guid? branchId = null,
        Guid? costCenterId = null,
        ApprovalWorkflowStatus? approvalStatus = null,
        CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Purchasing, branchId, cancellationToken);

        var query = dbContext.PurchaseOrders
            .AsNoTracking()
            .Include(order => order.Items)
            .Include(order => order.Receipts)
            .ThenInclude(receipt => receipt.Items)
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

    public async Task<PurchaseOrderResponse> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders
            .AsNoTracking()
            .Include(current => current.Items)
            .Include(current => current.Receipts)
            .ThenInclude(receipt => receipt.Items)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Purchasing, order.BranchId, cancellationToken);

        return await MapOrderAsync(order, cancellationToken);
    }

    public async Task<PurchaseOrderResponse> CreateAsync(CreatePurchaseOrderRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Purchasing, request.BranchId, cancellationToken);

        if (request.Items.Count == 0)
        {
            throw new ValidationException("Purchase order must contain at least one item.");
        }

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(current => current.Id == request.SupplierId, cancellationToken)
            ?? throw new ValidationException("Supplier does not exist.");

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

        var groupedItems = request.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new PurchaseOrderItemRequest(group.Key, group.Sum(item => item.Quantity), group.Last().UnitCost))
            .ToArray();

        var productIds = groupedItems.Select(item => item.ProductId).Distinct().ToArray();
        var products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        if (products.Count != productIds.Length)
        {
            throw new ValidationException("One or more products do not exist.");
        }

        foreach (var item in groupedItems)
        {
            if (item.Quantity <= 0)
            {
                throw new ValidationException("Purchase order item quantity must be greater than zero.");
            }

            if (item.UnitCost < 0)
            {
                throw new ValidationException("Purchase order item unit cost cannot be negative.");
            }
        }

        var orderNumber = $"PO-{utcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..19].ToUpperInvariant();
        var order = new PurchaseOrder(orderNumber, request.SupplierId, request.WarehouseLocationId, request.BranchId, request.CostCenterId, request.ExpectedDeliveryDateUtc, request.Notes);
        order.SetCreatedAt(utcNow);

        foreach (var item in groupedItems)
        {
            var product = products[item.ProductId];
            order.AddItem(product.Id, product.Sku, product.Name, item.Quantity, item.UnitCost);
        }

        dbContext.PurchaseOrders.Add(order);

        var approvalRule = await platformPolicyService.FindRequiredApprovalRuleAsync(
            ApprovalModule.PurchaseOrder,
            order.TotalAmount,
            request.BranchId,
            cancellationToken);

        var actor = currentUserService.GetCurrentUser();

        if (approvalRule is not null)
        {
            order.RequireApproval(approvalRule.Id, utcNow);

            var decision = new ApprovalDecision(
                ApprovalModule.PurchaseOrder,
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
            "purchasing.order.created",
            nameof(PurchaseOrder),
            order.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new
            {
                order.OrderNumber,
                order.SupplierId,
                order.BranchId,
                order.CostCenterId,
                order.WarehouseLocationId,
                order.TotalAmount,
                order.ExpectedDeliveryDateUtc,
                order.ApprovalStatus
            })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        if (approvalRule is not null)
        {
            await eventBus.PublishAsync(
                "purchasing.order.pending-approval",
                new
                {
                    order.Id,
                    order.OrderNumber,
                    order.BranchId,
                    order.CostCenterId,
                    SupplierName = supplier.Name,
                    BranchName = branch.Name,
                    CostCenterName = costCenter?.Name,
                    order.TotalAmount,
                    approvalRule.RequiredRoleName
                },
                cancellationToken);
        }

        return await MapOrderAsync(order, supplier.Name, branch.Name, warehouse.Name, costCenter?.Name, cancellationToken);
    }

    public async Task<PurchaseOrderResponse> ApproveAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders
            .Include(current => current.Items)
            .Include(current => current.Receipts)
            .ThenInclude(receipt => receipt.Items)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Purchasing, order.BranchId, cancellationToken);

        var supplier = await dbContext.Suppliers.FirstAsync(current => current.Id == order.SupplierId, cancellationToken);
        var warehouse = await dbContext.WarehouseLocations.FirstAsync(current => current.Id == order.WarehouseLocationId, cancellationToken);
        var branchName = order.BranchId.HasValue
            ? await dbContext.Branches.Where(branch => branch.Id == order.BranchId.Value).Select(branch => branch.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var costCenterName = order.CostCenterId.HasValue
            ? await dbContext.CostCenters.Where(costCenter => costCenter.Id == order.CostCenterId.Value).Select(costCenter => costCenter.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        if (order.ApprovalStatus == ApprovalWorkflowStatus.Pending)
        {
            var approvalRule = order.ApprovalRuleId.HasValue
                ? await dbContext.ApprovalRules.FirstAsync(rule => rule.Id == order.ApprovalRuleId.Value, cancellationToken)
                : throw new ConflictException("Approval rule was not found for this purchase order.");

            var currentUser = currentUserService.GetCurrentUser();
            platformPolicyService.EnsureUserCanApprove(approvalRule, currentUser);

            order.MarkApprovalGranted(approvalRule.Id, currentUser.UserId, currentUser.Name, utcNow);

            var decision = await dbContext.ApprovalDecisions
                .Where(current => current.Module == ApprovalModule.PurchaseOrder && current.EntityId == order.Id && current.Status == ApprovalDecisionStatus.Pending)
                .OrderByDescending(current => current.RequestedAtUtc)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new ConflictException("Approval decision was not found for this purchase order.");

            decision.Approve(currentUser.UserId, currentUser.Name, "Approved from purchasing flow", utcNow);
        }

        order.Approve(utcNow);

        var actor = currentUserService.GetCurrentUser();
        dbContext.AuditEntries.Add(new AuditEntry(
            "purchasing.order.approved",
            nameof(PurchaseOrder),
            order.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new { order.OrderNumber, order.TotalAmount, order.Status, order.ApprovalStatus })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        await eventBus.PublishAsync(
            "purchasing.order.approved",
            new
            {
                order.Id,
                order.OrderNumber,
                order.BranchId,
                order.CostCenterId,
                SupplierName = supplier.Name,
                BranchName = branchName,
                CostCenterName = costCenterName,
                WarehouseName = warehouse.Name,
                order.TotalAmount
            },
            cancellationToken);

        return await MapOrderAsync(order, supplier.Name, branchName, warehouse.Name, costCenterName, cancellationToken);
    }

    public async Task<PurchaseOrderResponse> RejectAsync(Guid id, string? notes, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders
            .Include(current => current.Items)
            .Include(current => current.Receipts)
            .ThenInclude(receipt => receipt.Items)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        if (order.ApprovalStatus != ApprovalWorkflowStatus.Pending)
        {
            throw new ConflictException("Only pending approvals can be rejected.");
        }

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Purchasing, order.BranchId, cancellationToken);

        var approvalRule = order.ApprovalRuleId.HasValue
            ? await dbContext.ApprovalRules.FirstAsync(rule => rule.Id == order.ApprovalRuleId.Value, cancellationToken)
            : throw new ConflictException("Approval rule was not found for this purchase order.");

        var currentUser = currentUserService.GetCurrentUser();
        platformPolicyService.EnsureUserCanApprove(approvalRule, currentUser);

        var decision = await dbContext.ApprovalDecisions
            .Where(current => current.Module == ApprovalModule.PurchaseOrder && current.EntityId == order.Id && current.Status == ApprovalDecisionStatus.Pending)
            .OrderByDescending(current => current.RequestedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new ConflictException("Approval decision was not found for this purchase order.");

        decision.Reject(currentUser.UserId, currentUser.Name, notes, utcNow);
        order.MarkApprovalRejected(notes, utcNow);
        order.Cancel($"Rejected in approval flow. {notes}".Trim(), utcNow);

        dbContext.AuditEntries.Add(new AuditEntry(
            "purchasing.order.rejected",
            nameof(PurchaseOrder),
            order.Id.ToString(),
            currentUser.UserId,
            currentUser.Name,
            JsonSerializer.Serialize(new { order.OrderNumber, order.TotalAmount, notes })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        await eventBus.PublishAsync(
            "purchasing.order.rejected",
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

    public async Task<PurchaseOrderResponse> ReceiveAsync(Guid id, ReceivePurchaseOrderRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new ValidationException("Receipt must contain at least one item.");
        }

        var receivedAtUtc = request.ReceivedAtUtc ?? utcNow;
        var dueDateUtc = request.DueDateUtc ?? receivedAtUtc.AddDays(30);

        var order = await dbContext.PurchaseOrders
            .Include(current => current.Items)
            .Include(current => current.Receipts)
            .ThenInclude(receipt => receipt.Items)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Purchasing, order.BranchId, cancellationToken);

        var supplier = await dbContext.Suppliers.FirstAsync(current => current.Id == order.SupplierId, cancellationToken);
        var warehouse = await dbContext.WarehouseLocations.FirstAsync(current => current.Id == order.WarehouseLocationId, cancellationToken);
        var branchName = order.BranchId.HasValue
            ? await dbContext.Branches.Where(branch => branch.Id == order.BranchId.Value).Select(branch => branch.Name).FirstOrDefaultAsync(cancellationToken)
            : null;
        var costCenterName = order.CostCenterId.HasValue
            ? await dbContext.CostCenters.Where(costCenter => costCenter.Id == order.CostCenterId.Value).Select(costCenter => costCenter.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        var groupedItems = request.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new ReceivePurchaseOrderItemRequest(group.Key, group.Sum(item => item.Quantity)))
            .ToArray();

        var orderItemProductIds = order.Items.Select(item => item.ProductId).ToArray();
        if (groupedItems.Any(item => !orderItemProductIds.Contains(item.ProductId)))
        {
            throw new ValidationException("Receipt contains items that are not part of the purchase order.");
        }

        var products = await dbContext.Products
            .Where(product => orderItemProductIds.Contains(product.Id))
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        var balances = await dbContext.InventoryBalances
            .Where(balance => balance.WarehouseLocationId == order.WarehouseLocationId && orderItemProductIds.Contains(balance.ProductId))
            .ToDictionaryAsync(balance => balance.ProductId, cancellationToken);

        var actor = currentUserService.GetCurrentUser();
        var receiptNumber = $"RCV-{receivedAtUtc:yyyyMMdd}-{order.Receipts.Count + 1:00}";
        var receipt = new PurchaseReceipt(order.Id, receiptNumber, receivedAtUtc, request.Notes, actor.UserId);
        receipt.SetCreatedAt(receivedAtUtc);

        foreach (var item in groupedItems)
        {
            if (item.Quantity <= 0)
            {
                throw new ValidationException("Receipt quantity must be greater than zero.");
            }

            var orderItem = order.Items.First(current => current.ProductId == item.ProductId);
            var product = products[item.ProductId];

            orderItem.RegisterReceipt(item.Quantity, receivedAtUtc);
            receipt.AddItem(product.Id, product.Sku, product.Name, item.Quantity, orderItem.UnitCost);

            if (!balances.TryGetValue(item.ProductId, out var balance))
            {
                balance = new InventoryBalance(item.ProductId, order.WarehouseLocationId);
                balance.SetCreatedAt(receivedAtUtc);
                balances[item.ProductId] = balance;
                dbContext.InventoryBalances.Add(balance);
            }

            balance.AddStock(item.Quantity, receivedAtUtc);
            product.UpdateStandardCost(orderItem.UnitCost, receivedAtUtc);

            dbContext.StockMovements.Add(new StockMovement(
                item.ProductId,
                order.WarehouseLocationId,
                StockMovementType.Inbound,
                item.Quantity,
                $"Receipt for purchase order {order.OrderNumber}",
                "purchase-order",
                order.Id,
                actor.UserId));
        }

        order.RegisterReceipt(receipt, receivedAtUtc);
        dbContext.PurchaseReceipts.Add(receipt);

        var payable = new Payable(
            order.SupplierId,
            order.Id,
            receipt.Id,
            order.BranchId,
            order.CostCenterId,
            $"{receipt.ReceiptNumber}-AP",
            $"Supplier payable for receipt {receipt.ReceiptNumber}",
            receivedAtUtc,
            dueDateUtc,
            receipt.TotalAmount);

        payable.SetCreatedAt(receivedAtUtc);
        dbContext.Payables.Add(payable);

        dbContext.AuditEntries.Add(new AuditEntry(
            "purchasing.order.received",
            nameof(PurchaseOrder),
            order.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new
            {
                order.OrderNumber,
                receipt.ReceiptNumber,
                receipt.TotalAmount,
                DueDateUtc = dueDateUtc,
                order.Status
            })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        await eventBus.PublishAsync(
            "purchasing.order.received",
            new
            {
                order.Id,
                order.OrderNumber,
                order.BranchId,
                order.CostCenterId,
                ReceiptId = receipt.Id,
                receipt.ReceiptNumber,
                SupplierName = supplier.Name,
                BranchName = branchName,
                CostCenterName = costCenterName,
                WarehouseName = warehouse.Name,
                receipt.TotalAmount
            },
            cancellationToken);

        return await MapOrderAsync(order, supplier.Name, branchName, warehouse.Name, costCenterName, cancellationToken);
    }

    public async Task<PurchaseOrderResponse> CancelAsync(Guid id, string? notes, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.PurchaseOrders
            .Include(current => current.Items)
            .Include(current => current.Receipts)
            .ThenInclude(receipt => receipt.Items)
            .FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Purchase order not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Purchasing, order.BranchId, cancellationToken);

        order.Cancel(notes, utcNow);

        var actor = currentUserService.GetCurrentUser();
        dbContext.AuditEntries.Add(new AuditEntry(
            "purchasing.order.cancelled",
            nameof(PurchaseOrder),
            order.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new { order.OrderNumber, order.TotalAmount, order.Status, notes })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        return await MapOrderAsync(order, cancellationToken);
    }

    private async Task<IReadOnlyCollection<PurchaseOrderResponse>> MapOrdersAsync(IReadOnlyCollection<PurchaseOrder> orders, CancellationToken cancellationToken)
    {
        var supplierIds = orders.Select(order => order.SupplierId).Distinct().ToArray();
        var warehouseIds = orders.Select(order => order.WarehouseLocationId).Distinct().ToArray();
        var branchIds = orders.Where(order => order.BranchId.HasValue).Select(order => order.BranchId!.Value).Distinct().ToArray();
        var costCenterIds = orders.Where(order => order.CostCenterId.HasValue).Select(order => order.CostCenterId!.Value).Distinct().ToArray();

        var suppliers = await dbContext.Suppliers
            .AsNoTracking()
            .Where(supplier => supplierIds.Contains(supplier.Id))
            .ToDictionaryAsync(supplier => supplier.Id, supplier => supplier.Name, cancellationToken);

        var warehouses = await dbContext.WarehouseLocations
            .AsNoTracking()
            .Where(warehouse => warehouseIds.Contains(warehouse.Id))
            .ToDictionaryAsync(warehouse => warehouse.Id, warehouse => warehouse.Name, cancellationToken);

        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        var costCenters = await dbContext.CostCenters
            .AsNoTracking()
            .Where(costCenter => costCenterIds.Contains(costCenter.Id))
            .ToDictionaryAsync(costCenter => costCenter.Id, costCenter => costCenter.Name, cancellationToken);

        return orders
            .Select(order => ToResponse(
                order,
                suppliers[order.SupplierId],
                order.BranchId.HasValue && branches.TryGetValue(order.BranchId.Value, out var branchName) ? branchName : null,
                warehouses[order.WarehouseLocationId],
                order.CostCenterId.HasValue && costCenters.TryGetValue(order.CostCenterId.Value, out var costCenterName) ? costCenterName : null))
            .ToArray();
    }

    private async Task<PurchaseOrderResponse> MapOrderAsync(PurchaseOrder order, CancellationToken cancellationToken)
    {
        var supplierName = await dbContext.Suppliers
            .AsNoTracking()
            .Where(supplier => supplier.Id == order.SupplierId)
            .Select(supplier => supplier.Name)
            .FirstAsync(cancellationToken);

        var warehouseName = await dbContext.WarehouseLocations
            .AsNoTracking()
            .Where(warehouse => warehouse.Id == order.WarehouseLocationId)
            .Select(warehouse => warehouse.Name)
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

        return ToResponse(order, supplierName, branchName, warehouseName, costCenterName);
    }

    private Task<PurchaseOrderResponse> MapOrderAsync(
        PurchaseOrder order,
        string supplierName,
        string? branchName,
        string warehouseName,
        string? costCenterName,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(ToResponse(order, supplierName, branchName, warehouseName, costCenterName));
    }

    private static PurchaseOrderResponse ToResponse(
        PurchaseOrder order,
        string supplierName,
        string? branchName,
        string warehouseName,
        string? costCenterName)
    {
        return new PurchaseOrderResponse(
            order.Id,
            order.OrderNumber,
            order.SupplierId,
            supplierName,
            order.BranchId,
            branchName,
            order.CostCenterId,
            costCenterName,
            order.WarehouseLocationId,
            warehouseName,
            order.Status,
            order.ApprovalStatus,
            order.TotalAmount,
            order.ExpectedDeliveryDateUtc,
            order.Notes,
            order.CreatedAtUtc,
            order.Items
                .OrderBy(item => item.ProductName)
                .Select(item => new PurchaseOrderItemResponse(
                    item.ProductId,
                    item.Sku,
                    item.ProductName,
                    item.Quantity,
                    item.ReceivedQuantity,
                    item.UnitCost,
                    item.LineTotal))
                .ToArray(),
            order.Receipts
                .OrderByDescending(receipt => receipt.ReceivedAtUtc)
                .Select(receipt => new PurchaseReceiptResponse(
                    receipt.Id,
                    receipt.ReceiptNumber,
                    receipt.TotalAmount,
                    receipt.Notes,
                    receipt.ReceivedAtUtc,
                    receipt.Items
                        .OrderBy(item => item.ProductName)
                        .Select(item => new PurchaseReceiptItemResponse(
                            item.ProductId,
                            item.Sku,
                            item.ProductName,
                            item.Quantity,
                            item.UnitCost,
                            item.LineTotal))
                        .ToArray()))
                .ToArray());
    }
}
