using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Praxis.Application.Models;
using Praxis.Application.Services;
using Praxis.Domain.Identity;
using Praxis.Domain.Operations;

namespace Praxis.Api.Controllers;

[ApiController]
[Route("api/purchase-orders")]
[Authorize]
public sealed class PurchasingController(PurchasingService purchasingService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.PurchasingRead)]
    public async Task<ActionResult<IReadOnlyCollection<PurchaseOrderResponse>>> ListAsync(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? costCenterId,
        [FromQuery] ApprovalWorkflowStatus? approvalStatus,
        CancellationToken cancellationToken)
    {
        return Ok(await purchasingService.ListAsync(branchId, costCenterId, approvalStatus, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.PurchasingRead)]
    public async Task<ActionResult<PurchaseOrderResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await purchasingService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.PurchasingWrite)]
    public async Task<ActionResult<PurchaseOrderResponse>> CreateAsync([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        return Ok(await purchasingService.CreateAsync(request, DateTime.UtcNow, cancellationToken));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = PermissionCatalog.PurchasingWrite)]
    public async Task<ActionResult<PurchaseOrderResponse>> ApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await purchasingService.ApproveAsync(id, DateTime.UtcNow, cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = PermissionCatalog.PurchasingWrite)]
    public async Task<ActionResult<PurchaseOrderResponse>> RejectAsync(Guid id, [FromBody] OrderStatusTransitionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await purchasingService.RejectAsync(id, request.Notes, DateTime.UtcNow, cancellationToken));
    }

    [HttpPost("{id:guid}/receive")]
    [Authorize(Policy = PermissionCatalog.PurchasingWrite)]
    public async Task<ActionResult<PurchaseOrderResponse>> ReceiveAsync(Guid id, [FromBody] ReceivePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        return Ok(await purchasingService.ReceiveAsync(id, request, DateTime.UtcNow, cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = PermissionCatalog.PurchasingWrite)]
    public async Task<ActionResult<PurchaseOrderResponse>> CancelAsync(Guid id, [FromBody] OrderStatusTransitionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await purchasingService.CancelAsync(id, request.Notes, DateTime.UtcNow, cancellationToken));
    }
}
