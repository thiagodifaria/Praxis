using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Praxis.Application.Models;
using Praxis.Application.Services;
using Praxis.Domain.Identity;

namespace Praxis.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
public sealed class InventoryController(InventoryService inventoryService) : ControllerBase
{
    [HttpGet("warehouses")]
    [Authorize(Policy = PermissionCatalog.InventoryRead)]
    public async Task<ActionResult<IReadOnlyCollection<WarehouseResponse>>> ListWarehousesAsync([FromQuery] Guid? branchId, CancellationToken cancellationToken)
    {
        return Ok(await inventoryService.ListWarehousesAsync(branchId, cancellationToken));
    }

    [HttpGet("balances")]
    [Authorize(Policy = PermissionCatalog.InventoryRead)]
    public async Task<ActionResult<IReadOnlyCollection<InventoryBalanceResponse>>> ListBalancesAsync([FromQuery] Guid? branchId, CancellationToken cancellationToken)
    {
        return Ok(await inventoryService.ListBalancesAsync(branchId, cancellationToken));
    }

    [HttpGet("movements")]
    [Authorize(Policy = PermissionCatalog.InventoryRead)]
    public async Task<ActionResult<IReadOnlyCollection<StockMovementResponse>>> ListMovementsAsync([FromQuery] Guid? branchId, CancellationToken cancellationToken)
    {
        return Ok(await inventoryService.ListMovementsAsync(branchId, cancellationToken));
    }

    [HttpPost("adjustments")]
    [Authorize(Policy = PermissionCatalog.InventoryWrite)]
    public async Task<ActionResult<InventoryBalanceResponse>> AdjustAsync([FromBody] AdjustInventoryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await inventoryService.AdjustAsync(request, DateTime.UtcNow, cancellationToken));
    }
}
