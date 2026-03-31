using Praxis.Domain.Common;

namespace Praxis.Domain.Catalog;

public sealed class Category : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Category()
    {
    }

    public Category(string code, string name, string description)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description.Trim();
    }

    public void Update(string code, string name, string description, bool isActive, DateTime utcNow)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description.Trim();
        IsActive = isActive;
        Touch(utcNow);
    }
}

public sealed class Supplier : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ContactName { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; } = true;

    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Supplier()
    {
    }

    public Supplier(string code, string name, string? contactName, string? email, string? phone)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        ContactName = contactName?.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
    }

    public void Update(string code, string name, string? contactName, string? email, string? phone, bool isActive, DateTime utcNow)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        ContactName = contactName?.Trim();
        Email = email?.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
        IsActive = isActive;
        Touch(utcNow);
    }
}

public sealed class Product : BaseEntity
{
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public decimal StandardCost { get; private set; }
    public int ReorderLevel { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid CategoryId { get; private set; }
    public Guid? SupplierId { get; private set; }

    public Category Category { get; private set; } = null!;
    public Supplier? Supplier { get; private set; }

    private Product()
    {
    }

    public Product(
        string sku,
        string name,
        string description,
        decimal unitPrice,
        decimal standardCost,
        int reorderLevel,
        Guid categoryId,
        Guid? supplierId)
    {
        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description.Trim();
        UnitPrice = unitPrice;
        StandardCost = standardCost;
        ReorderLevel = reorderLevel;
        CategoryId = categoryId;
        SupplierId = supplierId;
    }

    public void Update(
        string sku,
        string name,
        string description,
        decimal unitPrice,
        decimal standardCost,
        int reorderLevel,
        Guid categoryId,
        Guid? supplierId,
        bool isActive,
        DateTime utcNow)
    {
        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description.Trim();
        UnitPrice = unitPrice;
        StandardCost = standardCost;
        ReorderLevel = reorderLevel;
        CategoryId = categoryId;
        SupplierId = supplierId;
        IsActive = isActive;
        Touch(utcNow);
    }

    public void UpdateStandardCost(decimal standardCost, DateTime utcNow)
    {
        StandardCost = standardCost;
        Touch(utcNow);
    }
}
