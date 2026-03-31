using Microsoft.EntityFrameworkCore;
using Praxis.Domain.Billing;
using Praxis.Domain.Catalog;
using Praxis.Domain.Identity;
using Praxis.Domain.Inventory;
using Praxis.Domain.Operations;
using Praxis.Domain.Purchasing;
using Praxis.Domain.Sales;

namespace Praxis.Application.Abstractions
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    public sealed record CurrentUserContext(Guid? UserId, string? Email, string? Name, string? Role, IReadOnlyCollection<string> Permissions)
    {
        public bool IsAuthenticated => UserId.HasValue;
    }

    public interface ICurrentUserService
    {
        CurrentUserContext GetCurrentUser();
    }

    public interface IPasswordHasher
    {
        string Hash(string plainText);
        bool Verify(string plainText, string passwordHash);
    }

    public interface ITokenService
    {
        string CreateAccessToken(Guid userId, string fullName, string email, string roleName, IReadOnlyCollection<string> permissions);
        string GenerateRefreshToken();
    }

    public interface IEventBus
    {
        Task PublishAsync<T>(string routingKey, T payload, CancellationToken cancellationToken = default);
    }

    public interface IDashboardCache
    {
        Task<Praxis.Application.Models.DashboardSnapshotResponse?> GetAsync(CancellationToken cancellationToken = default);
        Task SetAsync(Praxis.Application.Models.DashboardSnapshotResponse snapshot, CancellationToken cancellationToken = default);
        Task RemoveAsync(CancellationToken cancellationToken = default);
    }
}

namespace Praxis.Application.Persistence
{
    public interface IPraxisDbContext
    {
        DbSet<Role> Roles { get; }
        DbSet<Permission> Permissions { get; }
        DbSet<RolePermission> RolePermissions { get; }
        DbSet<User> Users { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<Category> Categories { get; }
        DbSet<Supplier> Suppliers { get; }
        DbSet<Product> Products { get; }
        DbSet<Customer> Customers { get; }
        DbSet<SalesOrder> SalesOrders { get; }
        DbSet<SalesOrderItem> SalesOrderItems { get; }
        DbSet<PurchaseOrder> PurchaseOrders { get; }
        DbSet<PurchaseOrderItem> PurchaseOrderItems { get; }
        DbSet<PurchaseReceipt> PurchaseReceipts { get; }
        DbSet<PurchaseReceiptItem> PurchaseReceiptItems { get; }
        DbSet<WarehouseLocation> WarehouseLocations { get; }
        DbSet<InventoryBalance> InventoryBalances { get; }
        DbSet<StockReservation> StockReservations { get; }
        DbSet<StockMovement> StockMovements { get; }
        DbSet<Invoice> Invoices { get; }
        DbSet<InvoiceItem> InvoiceItems { get; }
        DbSet<Receivable> Receivables { get; }
        DbSet<ReceivableSettlement> ReceivableSettlements { get; }
        DbSet<Payable> Payables { get; }
        DbSet<PayableSettlement> PayableSettlements { get; }
        DbSet<AuditEntry> AuditEntries { get; }
        DbSet<OperationalAlert> OperationalAlerts { get; }
        DbSet<Branch> Branches { get; }
        DbSet<CostCenter> CostCenters { get; }
        DbSet<ModuleFeatureFlag> ModuleFeatureFlags { get; }
        DbSet<ApprovalRule> ApprovalRules { get; }
        DbSet<ApprovalDecision> ApprovalDecisions { get; }
        DbSet<RealtimeNotification> RealtimeNotifications { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}

namespace Praxis.Application.Common
{
    public sealed class NotFoundException(string message) : Exception(message);

    public sealed class ConflictException(string message) : Exception(message);

    public sealed class ValidationException(string message) : Exception(message);

    public sealed class ForbiddenException(string message) : Exception(message);
}

namespace Praxis.Application.Models
{
    public sealed record LoginRequest(string Email, string Password);

    public sealed record RefreshTokenRequest(string RefreshToken);

    public sealed record AuthUserResponse(
        Guid Id,
        string FullName,
        string Email,
        string Role,
        IReadOnlyCollection<string> Permissions);

    public sealed record LoginResponse(
        string AccessToken,
        string RefreshToken,
        DateTime RefreshTokenExpiresAtUtc,
        AuthUserResponse User);

    public sealed record CategoryUpsertRequest(string Code, string Name, string Description, bool IsActive = true);

    public sealed record CategoryResponse(Guid Id, string Code, string Name, string Description, bool IsActive);

    public sealed record SupplierUpsertRequest(
        string Code,
        string Name,
        string? ContactName,
        string? Email,
        string? Phone,
        bool IsActive = true);

    public sealed record SupplierResponse(
        Guid Id,
        string Code,
        string Name,
        string? ContactName,
        string? Email,
        string? Phone,
        bool IsActive);

    public sealed record ProductUpsertRequest(
        string Sku,
        string Name,
        string Description,
        decimal UnitPrice,
        decimal StandardCost,
        int ReorderLevel,
        Guid CategoryId,
        Guid? SupplierId,
        bool IsActive = true);

    public sealed record ProductResponse(
        Guid Id,
        string Sku,
        string Name,
        string Description,
        decimal UnitPrice,
        decimal StandardCost,
        int ReorderLevel,
        bool IsActive,
        Guid CategoryId,
        string CategoryName,
        Guid? SupplierId,
        string? SupplierName);

    public sealed record CustomerUpsertRequest(
        string Code,
        string Name,
        string Document,
        string? Email,
        string? Phone,
        CustomerStatus Status);

    public sealed record CustomerResponse(
        Guid Id,
        string Code,
        string Name,
        string Document,
        string? Email,
        string? Phone,
        CustomerStatus Status);

    public sealed record SalesOrderItemRequest(Guid ProductId, int Quantity);

    public sealed record CreateSalesOrderRequest(Guid CustomerId, Guid BranchId, Guid? CostCenterId, Guid WarehouseLocationId, string? Notes, IReadOnlyCollection<SalesOrderItemRequest> Items);

    public sealed record SalesOrderItemResponse(Guid ProductId, string Sku, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);

    public sealed record SalesOrderResponse(
        Guid Id,
        string OrderNumber,
        Guid CustomerId,
        string CustomerName,
        Guid? BranchId,
        string? BranchName,
        Guid? CostCenterId,
        string? CostCenterName,
        Guid WarehouseLocationId,
        string WarehouseName,
        SalesOrderStatus Status,
        ApprovalWorkflowStatus ApprovalStatus,
        decimal TotalAmount,
        string? Notes,
        DateTime CreatedAtUtc,
        IReadOnlyCollection<SalesOrderItemResponse> Items);

    public sealed record OrderStatusTransitionRequest(string? Notes);

    public sealed record WarehouseResponse(Guid Id, Guid? BranchId, string? BranchName, string Code, string Name, string Description, bool IsDefault);

    public sealed record InventoryBalanceResponse(
        Guid ProductId,
        string ProductName,
        string ProductSku,
        Guid? BranchId,
        string? BranchName,
        Guid WarehouseLocationId,
        string WarehouseName,
        int OnHandQuantity,
        int ReservedQuantity,
        int AvailableQuantity,
        int ReorderLevel);

    public sealed record AdjustInventoryRequest(Guid ProductId, Guid WarehouseLocationId, int QuantityDelta, string Reason);

    public sealed record StockMovementResponse(
        Guid Id,
        Guid ProductId,
        string ProductName,
        string ProductSku,
        Guid WarehouseLocationId,
        string WarehouseName,
        StockMovementType Type,
        int Quantity,
        string Reason,
        string ReferenceType,
        Guid? ReferenceId,
        DateTime CreatedAtUtc);

    public sealed record DashboardOrderOverview(string OrderNumber, string CustomerName, SalesOrderStatus Status, decimal TotalAmount, DateTime CreatedAtUtc);

    public sealed record DashboardLowStockItem(Guid ProductId, string ProductName, string ProductSku, int AvailableQuantity, int ReorderLevel, string WarehouseName);

    public sealed record DashboardMovementOverview(string ProductName, string ProductSku, string WarehouseName, string Reason, int Quantity, DateTime CreatedAtUtc);

    public sealed record DashboardAlertOverview(Guid Id, string Code, string Title, AlertSeverity Severity, AlertStatus Status, DateTime CreatedAtUtc);

    public sealed record DashboardInvoiceOverview(string InvoiceNumber, string CustomerName, InvoiceStatus Status, decimal TotalAmount, DateTime IssuedAtUtc);

    public sealed record DashboardBranchOverview(Guid? BranchId, string BranchName, int ActiveOrders, int ActivePurchaseOrders, decimal OpenReceivablesAmount, int OpenAlerts);

    public sealed record DashboardSnapshotResponse(
        int ActiveCustomers,
        int ActiveProducts,
        int DraftOrders,
        int ApprovedOrders,
        int ApprovedPurchaseOrders,
        int IssuedInvoices,
        int LowStockProducts,
        int PendingApprovals,
        int UnreadNotifications,
        decimal OrderPipelineAmount,
        decimal OpenReceivablesAmount,
        decimal OverdueReceivablesAmount,
        decimal OpenPayablesAmount,
        IReadOnlyCollection<DashboardOrderOverview> RecentOrders,
        IReadOnlyCollection<DashboardInvoiceOverview> RecentInvoices,
        IReadOnlyCollection<DashboardLowStockItem> LowStockItems,
        IReadOnlyCollection<DashboardMovementOverview> RecentMovements,
        IReadOnlyCollection<DashboardAlertOverview> OpenAlerts,
        IReadOnlyCollection<DashboardBranchOverview> Branches,
        DateTime GeneratedAtUtc);
}
