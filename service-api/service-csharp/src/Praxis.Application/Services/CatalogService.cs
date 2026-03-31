using Microsoft.EntityFrameworkCore;
using Praxis.Application.Common;
using Praxis.Application.Models;
using Praxis.Application.Persistence;
using Praxis.Domain.Catalog;
using Praxis.Domain.Operations;

namespace Praxis.Application.Services;

public sealed class CatalogService(IPraxisDbContext dbContext, PlatformPolicyService platformPolicyService)
{
    public async Task<IReadOnlyCollection<CategoryResponse>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Catalog, cancellationToken: cancellationToken);

        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(category.Id, category.Code, category.Name, category.Description, category.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryResponse> CreateCategoryAsync(CategoryUpsertRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Catalog, cancellationToken: cancellationToken);
        await EnsureCategoryCodeIsAvailableAsync(request.Code, null, cancellationToken);

        var category = new Category(request.Code, request.Name, request.Description);
        category.SetCreatedAt(utcNow);

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryResponse(category.Id, category.Code, category.Name, category.Description, category.IsActive);
    }

    public async Task<CategoryResponse> UpdateCategoryAsync(Guid id, CategoryUpsertRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Catalog, cancellationToken: cancellationToken);
        var category = await dbContext.Categories.FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Category not found.");

        await EnsureCategoryCodeIsAvailableAsync(request.Code, id, cancellationToken);
        category.Update(request.Code, request.Name, request.Description, request.IsActive, utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CategoryResponse(category.Id, category.Code, category.Name, category.Description, category.IsActive);
    }

    public async Task<IReadOnlyCollection<SupplierResponse>> ListSuppliersAsync(CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Catalog, cancellationToken: cancellationToken);
        return await dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .Select(supplier => new SupplierResponse(
                supplier.Id,
                supplier.Code,
                supplier.Name,
                supplier.ContactName,
                supplier.Email,
                supplier.Phone,
                supplier.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<SupplierResponse> CreateSupplierAsync(SupplierUpsertRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Catalog, cancellationToken: cancellationToken);
        await EnsureSupplierCodeIsAvailableAsync(request.Code, null, cancellationToken);

        var supplier = new Supplier(request.Code, request.Name, request.ContactName, request.Email, request.Phone);
        supplier.SetCreatedAt(utcNow);

        dbContext.Suppliers.Add(supplier);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new SupplierResponse(
            supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.ContactName,
            supplier.Email,
            supplier.Phone,
            supplier.IsActive);
    }

    public async Task<SupplierResponse> UpdateSupplierAsync(Guid id, SupplierUpsertRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Catalog, cancellationToken: cancellationToken);
        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Supplier not found.");

        await EnsureSupplierCodeIsAvailableAsync(request.Code, id, cancellationToken);
        supplier.Update(request.Code, request.Name, request.ContactName, request.Email, request.Phone, request.IsActive, utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SupplierResponse(
            supplier.Id,
            supplier.Code,
            supplier.Name,
            supplier.ContactName,
            supplier.Email,
            supplier.Phone,
            supplier.IsActive);
    }

    public async Task<IReadOnlyCollection<ProductResponse>> ListProductsAsync(CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Catalog, cancellationToken: cancellationToken);
        return await dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Supplier)
            .OrderBy(product => product.Name)
            .Select(product => new ProductResponse(
                product.Id,
                product.Sku,
                product.Name,
                product.Description,
                product.UnitPrice,
                product.StandardCost,
                product.ReorderLevel,
                product.IsActive,
                product.CategoryId,
                product.Category.Name,
                product.SupplierId,
                product.Supplier != null ? product.Supplier.Name : null))
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductResponse> CreateProductAsync(ProductUpsertRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Catalog, cancellationToken: cancellationToken);
        await EnsureProductIsValidAsync(request, null, cancellationToken);

        var product = new Product(
            request.Sku,
            request.Name,
            request.Description,
            request.UnitPrice,
            request.StandardCost,
            request.ReorderLevel,
            request.CategoryId,
            request.SupplierId);

        product.SetCreatedAt(utcNow);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetProductAsync(product.Id, cancellationToken);
    }

    public async Task<ProductResponse> UpdateProductAsync(Guid id, ProductUpsertRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Catalog, cancellationToken: cancellationToken);
        var product = await dbContext.Products.FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Product not found.");

        await EnsureProductIsValidAsync(request, id, cancellationToken);
        product.Update(
            request.Sku,
            request.Name,
            request.Description,
            request.UnitPrice,
            request.StandardCost,
            request.ReorderLevel,
            request.CategoryId,
            request.SupplierId,
            request.IsActive,
            utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetProductAsync(product.Id, cancellationToken);
    }

    private async Task<ProductResponse> GetProductAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Include(product => product.Category)
            .Include(product => product.Supplier)
            .Where(product => product.Id == id)
            .Select(product => new ProductResponse(
                product.Id,
                product.Sku,
                product.Name,
                product.Description,
                product.UnitPrice,
                product.StandardCost,
                product.ReorderLevel,
                product.IsActive,
                product.CategoryId,
                product.Category.Name,
                product.SupplierId,
                product.Supplier != null ? product.Supplier.Name : null))
            .FirstAsync(cancellationToken);
    }

    private async Task EnsureCategoryCodeIsAvailableAsync(string code, Guid? currentId, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var exists = await dbContext.Categories.AnyAsync(category => category.Code == normalizedCode && category.Id != currentId, cancellationToken);

        if (exists)
        {
            throw new ConflictException("Category code is already in use.");
        }
    }

    private async Task EnsureSupplierCodeIsAvailableAsync(string code, Guid? currentId, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var exists = await dbContext.Suppliers.AnyAsync(supplier => supplier.Code == normalizedCode && supplier.Id != currentId, cancellationToken);

        if (exists)
        {
            throw new ConflictException("Supplier code is already in use.");
        }
    }

    private async Task EnsureProductIsValidAsync(ProductUpsertRequest request, Guid? currentId, CancellationToken cancellationToken)
    {
        if (request.UnitPrice < 0)
        {
            throw new ValidationException("Product price cannot be negative.");
        }

        if (request.StandardCost < 0)
        {
            throw new ValidationException("Product standard cost cannot be negative.");
        }

        if (request.ReorderLevel < 0)
        {
            throw new ValidationException("Reorder level cannot be negative.");
        }

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        var skuExists = await dbContext.Products.AnyAsync(product => product.Sku == normalizedSku && product.Id != currentId, cancellationToken);

        if (skuExists)
        {
            throw new ConflictException("Product SKU is already in use.");
        }

        var categoryExists = await dbContext.Categories.AnyAsync(category => category.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new ValidationException("Category does not exist.");
        }

        if (request.SupplierId.HasValue)
        {
            var supplierExists = await dbContext.Suppliers.AnyAsync(supplier => supplier.Id == request.SupplierId.Value, cancellationToken);
            if (!supplierExists)
            {
                throw new ValidationException("Supplier does not exist.");
            }
        }
    }
}
