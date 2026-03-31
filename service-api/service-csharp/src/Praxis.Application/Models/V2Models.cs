using Praxis.Domain.Billing;
using Praxis.Domain.Operations;
using Praxis.Domain.Purchasing;

namespace Praxis.Application.Models;

public sealed record PurchaseOrderItemRequest(Guid ProductId, int Quantity, decimal UnitCost);

public sealed record CreatePurchaseOrderRequest(
    Guid SupplierId,
    Guid BranchId,
    Guid? CostCenterId,
    Guid WarehouseLocationId,
    DateTime? ExpectedDeliveryDateUtc,
    string? Notes,
    IReadOnlyCollection<PurchaseOrderItemRequest> Items);

public sealed record ReceivePurchaseOrderItemRequest(Guid ProductId, int Quantity);

public sealed record ReceivePurchaseOrderRequest(
    DateTime? ReceivedAtUtc,
    DateTime? DueDateUtc,
    string? Notes,
    IReadOnlyCollection<ReceivePurchaseOrderItemRequest> Items);

public sealed record PurchaseOrderItemResponse(
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    int ReceivedQuantity,
    decimal UnitCost,
    decimal LineTotal);

public sealed record PurchaseReceiptItemResponse(Guid ProductId, string Sku, string ProductName, int Quantity, decimal UnitCost, decimal LineTotal);

public sealed record PurchaseReceiptResponse(
    Guid Id,
    string ReceiptNumber,
    decimal TotalAmount,
    string? Notes,
    DateTime ReceivedAtUtc,
    IReadOnlyCollection<PurchaseReceiptItemResponse> Items);

public sealed record PurchaseOrderResponse(
    Guid Id,
    string OrderNumber,
    Guid SupplierId,
    string SupplierName,
    Guid? BranchId,
    string? BranchName,
    Guid? CostCenterId,
    string? CostCenterName,
    Guid WarehouseLocationId,
    string WarehouseName,
    PurchaseOrderStatus Status,
    ApprovalWorkflowStatus ApprovalStatus,
    decimal TotalAmount,
    DateTime? ExpectedDeliveryDateUtc,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<PurchaseOrderItemResponse> Items,
    IReadOnlyCollection<PurchaseReceiptResponse> Receipts);

public sealed record IssueInvoiceRequest(Guid SalesOrderId, DateTime? DueDateUtc, string? Notes);

public sealed record InvoiceItemResponse(
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal UnitCost,
    decimal LineTotal,
    decimal LineCost);

public sealed record InvoiceResponse(
    Guid Id,
    string InvoiceNumber,
    Guid SalesOrderId,
    string SalesOrderNumber,
    Guid CustomerId,
    string CustomerName,
    Guid? BranchId,
    string? BranchName,
    Guid? CostCenterId,
    string? CostCenterName,
    InvoiceStatus Status,
    DateTime IssuedAtUtc,
    DateTime DueDateUtc,
    decimal TotalAmount,
    string? Notes,
    IReadOnlyCollection<InvoiceItemResponse> Items);

public sealed record FinancialSettlementRequest(decimal Amount, DateTime? PaidAtUtc, string PaymentMethod, string? Notes);

public sealed record ReceivableSettlementResponse(Guid Id, decimal Amount, DateTime PaidAtUtc, string PaymentMethod, string? Notes);

public sealed record ReceivableResponse(
    Guid Id,
    string DocumentNumber,
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    Guid? BranchId,
    string? BranchName,
    Guid? CostCenterId,
    string? CostCenterName,
    string Description,
    DateTime IssuedAtUtc,
    DateTime DueDateUtc,
    decimal OriginalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    FinancialTitleStatus Status,
    IReadOnlyCollection<ReceivableSettlementResponse> Settlements);

public sealed record PayableSettlementResponse(Guid Id, decimal Amount, DateTime PaidAtUtc, string PaymentMethod, string? Notes);

public sealed record PayableResponse(
    Guid Id,
    string DocumentNumber,
    Guid? PurchaseOrderId,
    string? PurchaseOrderNumber,
    Guid? PurchaseReceiptId,
    string? PurchaseReceiptNumber,
    Guid SupplierId,
    string SupplierName,
    Guid? BranchId,
    string? BranchName,
    Guid? CostCenterId,
    string? CostCenterName,
    string Description,
    DateTime IssuedAtUtc,
    DateTime DueDateUtc,
    decimal OriginalAmount,
    decimal PaidAmount,
    decimal OutstandingAmount,
    FinancialTitleStatus Status,
    IReadOnlyCollection<PayableSettlementResponse> Settlements);

public sealed record ReportRange(DateTime FromUtc, DateTime ToUtc);

public sealed record ReportingTopCustomerItem(Guid CustomerId, string CustomerName, decimal Revenue, decimal Margin);

public sealed record ReportingTopSupplierItem(Guid SupplierId, string SupplierName, decimal Spend);

public sealed record ReportingBranchPerformanceItem(Guid? BranchId, string BranchName, decimal Revenue, decimal OpenReceivablesAmount, int OpenAlerts);

public sealed record ReportingCostCenterPerformanceItem(Guid? CostCenterId, string CostCenterName, decimal Revenue, decimal Spend);

public sealed record ReportingOverviewResponse(
    DateTime GeneratedAtUtc,
    DateTime FromUtc,
    DateTime ToUtc,
    decimal GrossRevenue,
    decimal GrossCost,
    decimal GrossMargin,
    decimal GrossMarginPercentage,
    int IssuedInvoices,
    int DispatchedOrders,
    int ReceivedPurchaseOrders,
    decimal OpenReceivablesAmount,
    decimal OverdueReceivablesAmount,
    decimal OpenPayablesAmount,
    IReadOnlyCollection<ReportingTopCustomerItem> TopCustomers,
    IReadOnlyCollection<ReportingTopSupplierItem> TopSuppliers,
    IReadOnlyCollection<ReportingBranchPerformanceItem> BranchPerformance,
    IReadOnlyCollection<ReportingCostCenterPerformanceItem> CostCenterPerformance);

public sealed record InventoryTurnoverItem(Guid ProductId, string ProductName, string ProductSku, int OnHandQuantity, int OutboundQuantity, decimal TurnoverRatio);

public sealed record InventoryTurnoverResponse(
    DateTime GeneratedAtUtc,
    DateTime FromUtc,
    DateTime ToUtc,
    decimal InventoryValue,
    int InboundQuantity,
    int OutboundQuantity,
    decimal StockTurnoverRatio,
    IReadOnlyCollection<InventoryTurnoverItem> Items);

public sealed record OverdueReceivableReportItem(
    Guid ReceivableId,
    string DocumentNumber,
    string CustomerName,
    DateTime DueDateUtc,
    int DaysOverdue,
    decimal OutstandingAmount);

public sealed record OverdueReceivablesReportResponse(
    DateTime GeneratedAtUtc,
    int TotalOpenTitles,
    int OverdueTitles,
    decimal OverdueAmount,
    IReadOnlyCollection<OverdueReceivableReportItem> Items);

public sealed record AuditEntryResponse(
    Guid Id,
    string EventType,
    string EntityName,
    string EntityId,
    Guid? ActorUserId,
    string? ActorName,
    string MetadataJson,
    DateTime CreatedAtUtc);

public sealed record OperationalAlertResponse(
    Guid Id,
    string Code,
    string Title,
    string Message,
    string Source,
    Guid? BranchId,
    string? BranchName,
    string? ReferenceId,
    AlertSeverity Severity,
    AlertStatus Status,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc);
