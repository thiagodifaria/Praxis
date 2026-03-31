using Praxis.Domain.Common;

namespace Praxis.Domain.Inventory;

public enum StockMovementType
{
    Inbound = 0,
    Outbound = 1,
    Adjustment = 2,
    Reservation = 3,
    ReservationRelease = 4
}

public enum ReservationStatus
{
    Active = 0,
    Released = 1,
    Consumed = 2
}

public sealed class WarehouseLocation : BaseEntity
{
    public Guid? BranchId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }

    public ICollection<InventoryBalance> InventoryBalances { get; private set; } = new List<InventoryBalance>();

    private WarehouseLocation()
    {
    }

    public WarehouseLocation(Guid? branchId, string code, string name, string description, bool isDefault)
    {
        BranchId = branchId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description.Trim();
        IsDefault = isDefault;
    }

    public void Update(Guid? branchId, string code, string name, string description, bool isDefault, DateTime utcNow)
    {
        BranchId = branchId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description.Trim();
        IsDefault = isDefault;
        Touch(utcNow);
    }
}

public sealed class InventoryBalance : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid WarehouseLocationId { get; private set; }
    public int OnHandQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }

    public int AvailableQuantity => OnHandQuantity - ReservedQuantity;

    private InventoryBalance()
    {
    }

    public InventoryBalance(Guid productId, Guid warehouseLocationId)
    {
        ProductId = productId;
        WarehouseLocationId = warehouseLocationId;
    }

    public void AddStock(int quantity, DateTime utcNow)
    {
        if (OnHandQuantity + quantity < ReservedQuantity)
        {
            throw new InvalidOperationException("Inventory cannot go below the reserved quantity.");
        }

        OnHandQuantity += quantity;
        Touch(utcNow);
    }

    public void Reserve(int quantity, DateTime utcNow)
    {
        if (quantity <= 0)
        {
            throw new InvalidOperationException("Reservation quantity must be greater than zero.");
        }

        if (AvailableQuantity < quantity)
        {
            throw new InvalidOperationException("Not enough stock available for reservation.");
        }

        ReservedQuantity += quantity;
        Touch(utcNow);
    }

    public void ReleaseReservation(int quantity, DateTime utcNow)
    {
        if (quantity <= 0 || ReservedQuantity < quantity)
        {
            throw new InvalidOperationException("Reservation release quantity is invalid.");
        }

        ReservedQuantity -= quantity;
        Touch(utcNow);
    }

    public void DispatchReserved(int quantity, DateTime utcNow)
    {
        if (quantity <= 0 || ReservedQuantity < quantity || OnHandQuantity < quantity)
        {
            throw new InvalidOperationException("Dispatch quantity is invalid.");
        }

        ReservedQuantity -= quantity;
        OnHandQuantity -= quantity;
        Touch(utcNow);
    }
}

public sealed class StockReservation : BaseEntity
{
    public Guid SalesOrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid WarehouseLocationId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; } = ReservationStatus.Active;
    public DateTime? ReleasedAtUtc { get; private set; }
    public DateTime? ConsumedAtUtc { get; private set; }

    private StockReservation()
    {
    }

    public StockReservation(Guid salesOrderId, Guid productId, Guid warehouseLocationId, int quantity)
    {
        SalesOrderId = salesOrderId;
        ProductId = productId;
        WarehouseLocationId = warehouseLocationId;
        Quantity = quantity;
    }

    public void Release(DateTime utcNow)
    {
        if (Status != ReservationStatus.Active)
        {
            throw new InvalidOperationException("Only active reservations can be released.");
        }

        Status = ReservationStatus.Released;
        ReleasedAtUtc = utcNow;
        Touch(utcNow);
    }

    public void Consume(DateTime utcNow)
    {
        if (Status != ReservationStatus.Active)
        {
            throw new InvalidOperationException("Only active reservations can be consumed.");
        }

        Status = ReservationStatus.Consumed;
        ConsumedAtUtc = utcNow;
        Touch(utcNow);
    }
}

public sealed class StockMovement : BaseEntity
{
    public Guid ProductId { get; private set; }
    public Guid WarehouseLocationId { get; private set; }
    public StockMovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid? ReferenceId { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    private StockMovement()
    {
    }

    public StockMovement(
        Guid productId,
        Guid warehouseLocationId,
        StockMovementType type,
        int quantity,
        string reason,
        string referenceType,
        Guid? referenceId,
        Guid? createdByUserId)
    {
        ProductId = productId;
        WarehouseLocationId = warehouseLocationId;
        Type = type;
        Quantity = quantity;
        Reason = reason.Trim();
        ReferenceType = referenceType.Trim();
        ReferenceId = referenceId;
        CreatedByUserId = createdByUserId;
    }
}
