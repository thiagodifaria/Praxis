using Praxis.Domain.Common;

namespace Praxis.Domain.Billing;

public enum InvoiceStatus
{
    Issued = 0,
    Paid = 1,
    Cancelled = 2
}

public enum FinancialTitleStatus
{
    Open = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4
}

public sealed class Invoice : BaseEntity
{
    public string InvoiceNumber { get; private set; } = string.Empty;
    public Guid SalesOrderId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Issued;
    public DateTime IssuedAtUtc { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public ICollection<InvoiceItem> Items { get; private set; } = new List<InvoiceItem>();

    private Invoice()
    {
    }

    public Invoice(string invoiceNumber, Guid salesOrderId, Guid customerId, Guid? branchId, Guid? costCenterId, DateTime issuedAtUtc, DateTime dueDateUtc, string? notes)
    {
        InvoiceNumber = invoiceNumber.Trim().ToUpperInvariant();
        SalesOrderId = salesOrderId;
        CustomerId = customerId;
        BranchId = branchId;
        CostCenterId = costCenterId;
        IssuedAtUtc = issuedAtUtc;
        DueDateUtc = dueDateUtc;
        Notes = notes?.Trim();
    }

    public void AddItem(Guid productId, string sku, string productName, int quantity, decimal unitPrice, decimal unitCost)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Invoice quantity must be greater than zero.");
        }

        Items.Add(new InvoiceItem(Id, productId, sku, productName, quantity, unitPrice, unitCost));
        TotalAmount = Items.Sum(item => item.LineTotal);
    }

    public void MarkPaid(DateTime utcNow)
    {
        if (Status == InvoiceStatus.Cancelled)
        {
            throw new InvalidOperationException("Cancelled invoices cannot be paid.");
        }

        Status = InvoiceStatus.Paid;
        PaidAtUtc = utcNow;
        Touch(utcNow);
    }

    public void Cancel(string? notes, DateTime utcNow)
    {
        if (Status == InvoiceStatus.Paid)
        {
            throw new InvalidOperationException("Paid invoices cannot be cancelled.");
        }

        Status = InvoiceStatus.Cancelled;
        Notes = string.IsNullOrWhiteSpace(notes) ? Notes : notes.Trim();
        CancelledAtUtc = utcNow;
        Touch(utcNow);
    }

    public void AssignBranchContext(Guid? branchId, Guid? costCenterId, DateTime utcNow)
    {
        BranchId = branchId;
        CostCenterId = costCenterId;
        Touch(utcNow);
    }
}

public sealed class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal LineCost { get; private set; }

    public Invoice Invoice { get; private set; } = null!;

    private InvoiceItem()
    {
    }

    public InvoiceItem(Guid invoiceId, Guid productId, string sku, string productName, int quantity, decimal unitPrice, decimal unitCost)
    {
        InvoiceId = invoiceId;
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

public sealed class Receivable : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public string DocumentNumber { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime IssuedAtUtc { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public decimal OriginalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal OutstandingAmount { get; private set; }
    public FinancialTitleStatus Status { get; private set; } = FinancialTitleStatus.Open;
    public DateTime? SettledAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public ICollection<ReceivableSettlement> Settlements { get; private set; } = new List<ReceivableSettlement>();

    private Receivable()
    {
    }

    public Receivable(Guid customerId, Guid invoiceId, Guid? branchId, Guid? costCenterId, string documentNumber, string description, DateTime issuedAtUtc, DateTime dueDateUtc, decimal originalAmount)
    {
        CustomerId = customerId;
        InvoiceId = invoiceId;
        BranchId = branchId;
        CostCenterId = costCenterId;
        DocumentNumber = documentNumber.Trim().ToUpperInvariant();
        Description = description.Trim();
        IssuedAtUtc = issuedAtUtc;
        DueDateUtc = dueDateUtc;
        OriginalAmount = originalAmount;
        OutstandingAmount = originalAmount;
    }

    public void ApplySettlement(ReceivableSettlement settlement, DateTime utcNow)
    {
        if (Status == FinancialTitleStatus.Cancelled || Status == FinancialTitleStatus.Paid)
        {
            throw new InvalidOperationException("Receivable cannot receive new settlements.");
        }

        if (settlement.Amount > OutstandingAmount)
        {
            throw new InvalidOperationException("Settlement exceeds outstanding receivable amount.");
        }

        Settlements.Add(settlement);
        PaidAmount += settlement.Amount;
        OutstandingAmount -= settlement.Amount;
        Status = OutstandingAmount == 0m ? FinancialTitleStatus.Paid : FinancialTitleStatus.PartiallyPaid;

        if (Status == FinancialTitleStatus.Paid)
        {
            SettledAtUtc = utcNow;
        }

        Touch(utcNow);
    }

    public void MarkOverdue(DateTime utcNow)
    {
        if ((Status == FinancialTitleStatus.Open || Status == FinancialTitleStatus.PartiallyPaid) && DueDateUtc < utcNow)
        {
            Status = FinancialTitleStatus.Overdue;
            Touch(utcNow);
        }
    }

    public void Cancel(DateTime utcNow)
    {
        if (PaidAmount > 0)
        {
            throw new InvalidOperationException("Partially paid receivables cannot be cancelled.");
        }

        Status = FinancialTitleStatus.Cancelled;
        CancelledAtUtc = utcNow;
        OutstandingAmount = 0m;
        Touch(utcNow);
    }

    public void AssignBranchContext(Guid? branchId, Guid? costCenterId, DateTime utcNow)
    {
        BranchId = branchId;
        CostCenterId = costCenterId;
        Touch(utcNow);
    }
}

public sealed class ReceivableSettlement : BaseEntity
{
    public Guid ReceivableId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaidAtUtc { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public Guid? ReceivedByUserId { get; private set; }

    public Receivable Receivable { get; private set; } = null!;

    private ReceivableSettlement()
    {
    }

    public ReceivableSettlement(Guid receivableId, decimal amount, DateTime paidAtUtc, string paymentMethod, string? notes, Guid? receivedByUserId)
    {
        ReceivableId = receivableId;
        Amount = amount;
        PaidAtUtc = paidAtUtc;
        PaymentMethod = paymentMethod.Trim();
        Notes = notes?.Trim();
        ReceivedByUserId = receivedByUserId;
    }
}

public sealed class Payable : BaseEntity
{
    public Guid SupplierId { get; private set; }
    public Guid? PurchaseOrderId { get; private set; }
    public Guid? PurchaseReceiptId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public string DocumentNumber { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime IssuedAtUtc { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public decimal OriginalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal OutstandingAmount { get; private set; }
    public FinancialTitleStatus Status { get; private set; } = FinancialTitleStatus.Open;
    public DateTime? SettledAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }

    public ICollection<PayableSettlement> Settlements { get; private set; } = new List<PayableSettlement>();

    private Payable()
    {
    }

    public Payable(Guid supplierId, Guid? purchaseOrderId, Guid? purchaseReceiptId, Guid? branchId, Guid? costCenterId, string documentNumber, string description, DateTime issuedAtUtc, DateTime dueDateUtc, decimal originalAmount)
    {
        SupplierId = supplierId;
        PurchaseOrderId = purchaseOrderId;
        PurchaseReceiptId = purchaseReceiptId;
        BranchId = branchId;
        CostCenterId = costCenterId;
        DocumentNumber = documentNumber.Trim().ToUpperInvariant();
        Description = description.Trim();
        IssuedAtUtc = issuedAtUtc;
        DueDateUtc = dueDateUtc;
        OriginalAmount = originalAmount;
        OutstandingAmount = originalAmount;
    }

    public void ApplySettlement(PayableSettlement settlement, DateTime utcNow)
    {
        if (Status == FinancialTitleStatus.Cancelled || Status == FinancialTitleStatus.Paid)
        {
            throw new InvalidOperationException("Payable cannot receive new settlements.");
        }

        if (settlement.Amount > OutstandingAmount)
        {
            throw new InvalidOperationException("Settlement exceeds outstanding payable amount.");
        }

        Settlements.Add(settlement);
        PaidAmount += settlement.Amount;
        OutstandingAmount -= settlement.Amount;
        Status = OutstandingAmount == 0m ? FinancialTitleStatus.Paid : FinancialTitleStatus.PartiallyPaid;

        if (Status == FinancialTitleStatus.Paid)
        {
            SettledAtUtc = utcNow;
        }

        Touch(utcNow);
    }

    public void MarkOverdue(DateTime utcNow)
    {
        if ((Status == FinancialTitleStatus.Open || Status == FinancialTitleStatus.PartiallyPaid) && DueDateUtc < utcNow)
        {
            Status = FinancialTitleStatus.Overdue;
            Touch(utcNow);
        }
    }

    public void Cancel(DateTime utcNow)
    {
        if (PaidAmount > 0)
        {
            throw new InvalidOperationException("Partially paid payables cannot be cancelled.");
        }

        Status = FinancialTitleStatus.Cancelled;
        CancelledAtUtc = utcNow;
        OutstandingAmount = 0m;
        Touch(utcNow);
    }

    public void AssignBranchContext(Guid? branchId, Guid? costCenterId, DateTime utcNow)
    {
        BranchId = branchId;
        CostCenterId = costCenterId;
        Touch(utcNow);
    }
}

public sealed class PayableSettlement : BaseEntity
{
    public Guid PayableId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaidAtUtc { get; private set; }
    public string PaymentMethod { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public Guid? PaidByUserId { get; private set; }

    public Payable Payable { get; private set; } = null!;

    private PayableSettlement()
    {
    }

    public PayableSettlement(Guid payableId, decimal amount, DateTime paidAtUtc, string paymentMethod, string? notes, Guid? paidByUserId)
    {
        PayableId = payableId;
        Amount = amount;
        PaidAtUtc = paidAtUtc;
        PaymentMethod = paymentMethod.Trim();
        Notes = notes?.Trim();
        PaidByUserId = paidByUserId;
    }
}
