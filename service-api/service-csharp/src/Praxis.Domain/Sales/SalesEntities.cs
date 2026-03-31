using Praxis.Domain.Common;
using Praxis.Domain.Operations;

namespace Praxis.Domain.Sales;

public enum CustomerStatus
{
    Lead = 0,
    Active = 1,
    Inactive = 2
}

public enum SalesOrderStatus
{
    Draft = 0,
    Approved = 1,
    Dispatched = 2,
    Cancelled = 3
}

public sealed class Customer : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Document { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public CustomerStatus Status { get; private set; } = CustomerStatus.Active;

    public ICollection<SalesOrder> SalesOrders { get; private set; } = new List<SalesOrder>();

    private Customer()
    {
    }

    public Customer(string code, string name, string document, string? email, string? phone, CustomerStatus status)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Document = document.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
        Status = status;
    }

    public void Update(string code, string name, string document, string? email, string? phone, CustomerStatus status, DateTime utcNow)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Document = document.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
        Status = status;
        Touch(utcNow);
    }
}

public sealed class SalesOrder : BaseEntity
{
    public string OrderNumber { get; private set; } = string.Empty;
    public SalesOrderStatus Status { get; private set; } = SalesOrderStatus.Draft;
    public ApprovalWorkflowStatus ApprovalStatus { get; private set; } = ApprovalWorkflowStatus.NotRequired;
    public string? Notes { get; private set; }
    public decimal TotalAmount { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid WarehouseLocationId { get; private set; }
    public Guid? ApprovalRuleId { get; private set; }
    public DateTime? ApprovalRequestedAtUtc { get; private set; }
    public DateTime? ApprovalDecidedAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public string? ApprovedByName { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? DispatchedAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public Customer Customer { get; private set; } = null!;
    public ICollection<SalesOrderItem> Items { get; private set; } = new List<SalesOrderItem>();

    private SalesOrder()
    {
    }

    public SalesOrder(string orderNumber, Guid customerId, Guid warehouseLocationId, Guid branchId, Guid? costCenterId, string? notes)
    {
        OrderNumber = orderNumber.Trim().ToUpperInvariant();
        CustomerId = customerId;
        WarehouseLocationId = warehouseLocationId;
        BranchId = branchId;
        CostCenterId = costCenterId;
        Notes = notes?.Trim();
    }

    public void AddItem(Guid productId, string sku, string productName, int quantity, decimal unitPrice, decimal unitCost)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Order item quantity must be greater than zero.");
        }

        Items.Add(new SalesOrderItem(Id, productId, sku, productName, quantity, unitPrice, unitCost));
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
        if (Status != SalesOrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft orders can be approved.");
        }

        if (ApprovalStatus == ApprovalWorkflowStatus.Pending)
        {
            throw new InvalidOperationException("Order is still waiting for approval.");
        }

        if (ApprovalStatus == ApprovalWorkflowStatus.Rejected)
        {
            throw new InvalidOperationException("Rejected orders must be resubmitted before approval.");
        }

        Status = SalesOrderStatus.Approved;
        ApprovedAtUtc = utcNow;
        Touch(utcNow);
    }

    public void Dispatch(DateTime utcNow)
    {
        if (Status != SalesOrderStatus.Approved)
        {
            throw new InvalidOperationException("Only approved orders can be dispatched.");
        }

        Status = SalesOrderStatus.Dispatched;
        DispatchedAtUtc = utcNow;
        Touch(utcNow);
    }

    public void Cancel(string? notes, DateTime utcNow)
    {
        if (Status == SalesOrderStatus.Dispatched)
        {
            throw new InvalidOperationException("Dispatched orders cannot be cancelled.");
        }

        if (Status == SalesOrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Order is already cancelled.");
        }

        Status = SalesOrderStatus.Cancelled;
        Notes = string.IsNullOrWhiteSpace(notes) ? Notes : notes.Trim();
        CancelledAtUtc = utcNow;
        Touch(utcNow);
    }
}

public sealed class SalesOrderItem : BaseEntity
{
    public Guid SalesOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal LineCost { get; private set; }

    public SalesOrder SalesOrder { get; private set; } = null!;

    private SalesOrderItem()
    {
    }

    public SalesOrderItem(Guid salesOrderId, Guid productId, string sku, string productName, int quantity, decimal unitPrice, decimal unitCost)
    {
        SalesOrderId = salesOrderId;
        ProductId = productId;
        Sku = sku.Trim().ToUpperInvariant();
        ProductName = productName.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        UnitCost = unitCost;
        LineTotal = quantity * unitPrice;
        LineCost = quantity * unitCost;
    }

    public void UpdateCostSnapshot(decimal unitCost, DateTime utcNow)
    {
        UnitCost = unitCost;
        LineCost = Quantity * unitCost;
        Touch(utcNow);
    }
}
