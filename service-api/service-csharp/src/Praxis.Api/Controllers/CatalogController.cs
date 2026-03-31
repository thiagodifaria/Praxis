using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Praxis.Application.Models;
using Praxis.Application.Services;
using Praxis.Domain.Identity;

namespace Praxis.Api.Controllers;

[ApiController]
[Route("api/catalog")]
[Authorize]
public sealed class CatalogController(CatalogService catalogService) : ControllerBase
{
    [HttpGet("categories")]
    [Authorize(Policy = PermissionCatalog.CatalogRead)]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        return Ok(await catalogService.ListCategoriesAsync(cancellationToken));
    }

    [HttpPost("categories")]
    [Authorize(Policy = PermissionCatalog.CatalogWrite)]
    public async Task<ActionResult<CategoryResponse>> CreateCategoryAsync([FromBody] CategoryUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.CreateCategoryAsync(request, DateTime.UtcNow, cancellationToken));
    }

    [HttpPut("categories/{id:guid}")]
    [Authorize(Policy = PermissionCatalog.CatalogWrite)]
    public async Task<ActionResult<CategoryResponse>> UpdateCategoryAsync(Guid id, [FromBody] CategoryUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.UpdateCategoryAsync(id, request, DateTime.UtcNow, cancellationToken));
    }

    [HttpGet("suppliers")]
    [Authorize(Policy = PermissionCatalog.CatalogRead)]
    public async Task<ActionResult<IReadOnlyCollection<SupplierResponse>>> ListSuppliersAsync(CancellationToken cancellationToken)
    {
        return Ok(await catalogService.ListSuppliersAsync(cancellationToken));
    }

    [HttpPost("suppliers")]
    [Authorize(Policy = PermissionCatalog.CatalogWrite)]
    public async Task<ActionResult<SupplierResponse>> CreateSupplierAsync([FromBody] SupplierUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.CreateSupplierAsync(request, DateTime.UtcNow, cancellationToken));
    }

    [HttpPut("suppliers/{id:guid}")]
    [Authorize(Policy = PermissionCatalog.CatalogWrite)]
    public async Task<ActionResult<SupplierResponse>> UpdateSupplierAsync(Guid id, [FromBody] SupplierUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.UpdateSupplierAsync(id, request, DateTime.UtcNow, cancellationToken));
    }

    [HttpGet("products")]
    [Authorize(Policy = PermissionCatalog.CatalogRead)]
    public async Task<ActionResult<IReadOnlyCollection<ProductResponse>>> ListProductsAsync(CancellationToken cancellationToken)
    {
        return Ok(await catalogService.ListProductsAsync(cancellationToken));
    }

    [HttpPost("products")]
    [Authorize(Policy = PermissionCatalog.CatalogWrite)]
    public async Task<ActionResult<ProductResponse>> CreateProductAsync([FromBody] ProductUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.CreateProductAsync(request, DateTime.UtcNow, cancellationToken));
    }

    [HttpPut("products/{id:guid}")]
    [Authorize(Policy = PermissionCatalog.CatalogWrite)]
    public async Task<ActionResult<ProductResponse>> UpdateProductAsync(Guid id, [FromBody] ProductUpsertRequest request, CancellationToken cancellationToken)
    {
        return Ok(await catalogService.UpdateProductAsync(id, request, DateTime.UtcNow, cancellationToken));
    }
}
