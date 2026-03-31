using Praxis.Domain.Common;
using Praxis.Domain.Operations;

namespace Praxis.Domain.Purchasing;

public enum PurchaseOrderStatus
{
    Draft = 0,
    Approved = 1,
    PartiallyReceived = 2,
    Received = 3,
    Cancelled = 4
}

public sealed class PurchaseOrder : BaseEntity
{
    public string OrderNumber { get; private set; } = string.Empty;
    public PurchaseOrderStatus Status { get; private set; } = PurchaseOrderStatus.Draft;
    public ApprovalWorkflowStatus ApprovalStatus { get; private set; } = ApprovalWorkflowStatus.NotRequired;
    public string? Notes { get; private set; }
    public decimal TotalAmount { get; private set; }
    public Guid SupplierId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid WarehouseLocationId { get; private set; }
    public Guid? ApprovalRuleId { get; private set; }
    public DateTime? ApprovalRequestedAtUtc { get; private set; }
    public DateTime? ApprovalDecidedAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public string? ApprovedByName { get; private set; }
    public DateTime? ExpectedDeliveryDateUtc { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? ReceivedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public ICollection<PurchaseOrderItem> Items { get; private set; } = new List<PurchaseOrderItem>();
    public ICollection<PurchaseReceipt> Receipts { get; private set; } = new List<PurchaseReceipt>();

    private PurchaseOrder()
    {
    }

    public PurchaseOrder(string orderNumber, Guid supplierId, Guid warehouseLocationId, Guid branchId, Guid? costCenterId, DateTime? expectedDeliveryDateUtc, string? notes)
    {
        OrderNumber = orderNumber.Trim().ToUpperInvariant();
        SupplierId = supplierId;
        WarehouseLocationId = warehouseLocationId;
        BranchId = branchId;
        CostCenterId = costCenterId;
        ExpectedDeliveryDateUtc = expectedDeliveryDateUtc;
        Notes = notes?.Trim();
    }

    public void AddItem(Guid productId, string sku, string productName, int quantity, decimal unitCost)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Purchase order quantity must be greater than zero.");
        }

        if (unitCost < 0)
        {
            throw new InvalidOperationException("Purchase order unit cost cannot be negative.");
        }

        Items.Add(new PurchaseOrderItem(Id, productId, sku, productName, quantity, unitCost));
        TotalAmount = Items.Sum(item => item.LineTotal);
    }

    public void RequireApproval(Guid approvalRuleId, DateTime utcNow)
    {
        ApprovalRuleId = approvalRuleId;
        ApprovalStatus = ApprovalWorkflowStatus.Pending;
        ApprovalRequestedAtUtc = utcNow;
        ApprovalDecidedAtUtc = null;
        ApprovedByUserId = null;
        ApprovedByName = null;
        Touch(utcNow);
    }

    public void MarkApprovalGranted(Guid approvalRuleId, Guid? approvedByUserId, string? approvedByName, DateTime utcNow)
    {
        ApprovalRuleId = approvalRuleId;
        ApprovalStatus = ApprovalWorkflowStatus.Approved;
        ApprovalDecidedAtUtc = utcNow;
        ApprovedByUserId = approvedByUserId;
        ApprovedByName = approvedByName?.Trim();
        Touch(utcNow);
    }

    public void MarkApprovalRejected(string? notes, DateTime utcNow)
    {
        ApprovalStatus = ApprovalWorkflowStatus.Rejected;
        ApprovalDecidedAtUtc = utcNow;
        Notes = string.IsNullOrWhiteSpace(notes) ? Notes : notes.Trim();
        Touch(utcNow);
    }

    public void AssignBranchContext(Guid branchId, Guid? costCenterId, DateTime utcNow)
    {
        BranchId = branchId;
        CostCenterId = costCenterId;
        Touch(utcNow);
    }

    public void Approve(DateTime utcNow)
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft purchase orders can be approved.");
        }

        if (ApprovalStatus == ApprovalWorkflowStatus.Pending)
        {
            throw new InvalidOperationException("Purchase order is still waiting for approval.");
        }

        if (ApprovalStatus == ApprovalWorkflowStatus.Rejected)
        {
            throw new InvalidOperationException("Rejected purchase orders must be resubmitted before approval.");
        }

        Status = PurchaseOrderStatus.Approved;
        ApprovedAtUtc = utcNow;
        Touch(utcNow);
    }

    public void RegisterReceipt(PurchaseReceipt receipt, DateTime utcNow)
    {
        if (Status != PurchaseOrderStatus.Approved && Status != PurchaseOrderStatus.PartiallyReceived)
        {
            throw new InvalidOperationException("Only approved purchase orders can receive items.");
        }

        Receipts.Add(receipt);
        var totalOrdered = Items.Sum(item => item.Quantity);
        var totalReceived = Items.Sum(item => item.ReceivedQuantity);

        Status = totalReceived >= totalOrdered
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;

        if (Status == PurchaseOrderStatus.Received)
        {
            ReceivedAtUtc = utcNow;
        }

        Touch(utcNow);
    }

    public void Cancel(string? notes, DateTime utcNow)
    {
        if (Status == PurchaseOrderStatus.Received || Status == PurchaseOrderStatus.PartiallyReceived)
        {
            throw new InvalidOperationException("Received purchase orders cannot be cancelled.");
        }

        if (Status == PurchaseOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Purchase order is already cancelled.");
        }

        Status = PurchaseOrderStatus.Cancelled;
        Notes = string.IsNullOrWhiteSpace(notes) ? Notes : notes.Trim();
        CancelledAtUtc = utcNow;
        Touch(utcNow);
    }
}

public sealed class PurchaseOrderItem : BaseEntity
{
    public Guid PurchaseOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public int ReceivedQuantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }

    public PurchaseOrder PurchaseOrder { get; private set; } = null!;

    private PurchaseOrderItem()
    {
    }

    public PurchaseOrderItem(Guid purchaseOrderId, Guid productId, string sku, string productName, int quantity, decimal unitCost)
    {
        PurchaseOrderId = purchaseOrderId;
        ProductId = productId;
        Sku = sku.Trim().ToUpperInvariant();
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitCost = unitCost;
        LineTotal = quantity * unitCost;
    }

    public void RegisterReceipt(int quantity, DateTime utcNow)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Receipt quantity must be greater than zero.");
        }

        if (ReceivedQuantity + quantity > Quantity)
        {
            throw new InvalidOperationException("Receipt quantity exceeds the ordered amount.");
        }

        ReceivedQuantity += quantity;
        Touch(utcNow);
    }
}

public sealed class PurchaseReceipt : BaseEntity
{
    public Guid PurchaseOrderId { get; private set; }
    public string ReceiptNumber { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
    public Guid? ReceivedByUserId { get; private set; }

    public PurchaseOrder PurchaseOrder { get; private set; } = null!;
    public ICollection<PurchaseReceiptItem> Items { get; private set; } = new List<PurchaseReceiptItem>();

    private PurchaseReceipt()
    {
    }

    public PurchaseReceipt(Guid purchaseOrderId, string receiptNumber, DateTime receivedAtUtc, string? notes, Guid? receivedByUserId)
    {
        PurchaseOrderId = purchaseOrderId;
        ReceiptNumber = receiptNumber.Trim().ToUpperInvariant();
        ReceivedAtUtc = receivedAtUtc;
        Notes = notes?.Trim();
        ReceivedByUserId = receivedByUserId;
    }

    public void AddItem(Guid productId, string sku, string productName, int quantity, decimal unitCost)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Receipt quantity must be greater than zero.");
        }

        Items.Add(new PurchaseReceiptItem(Id, productId, sku, productName, quantity, unitCost));
        TotalAmount = Items.Sum(item => item.LineTotal);
    }
}

public sealed class PurchaseReceiptItem : BaseEntity
{
    public Guid PurchaseReceiptId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }

    public PurchaseReceipt PurchaseReceipt { get; private set; } = null!;

    private PurchaseReceiptItem()
    {
    }

    public PurchaseReceiptItem(Guid purchaseReceiptId, Guid productId, string sku, string productName, int quantity, decimal unitCost)
    {
        PurchaseReceiptId = purchaseReceiptId;
        ProductId = productId;
        Sku = sku.Trim().ToUpperInvariant();
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitCost = unitCost;
        LineTotal = quantity * unitCost;
    }
}
