using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Praxis.Application.Models;
using Praxis.Application.Services;
using Praxis.Domain.Identity;
using Praxis.Domain.Operations;

namespace Praxis.Api.Controllers;

[ApiController]
[Route("api/sales-orders")]
[Authorize]
public sealed class SalesOrdersController(SalesOrderService salesOrderService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.SalesRead)]
    public async Task<ActionResult<IReadOnlyCollection<SalesOrderResponse>>> ListAsync(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? costCenterId,
        [FromQuery] ApprovalWorkflowStatus? approvalStatus,
        CancellationToken cancellationToken)
    {
        return Ok(await salesOrderService.ListAsync(branchId, costCenterId, approvalStatus, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = PermissionCatalog.SalesRead)]
    public async Task<ActionResult<SalesOrderResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await salesOrderService.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = PermissionCatalog.SalesWrite)]
    public async Task<ActionResult<SalesOrderResponse>> CreateAsync([FromBody] CreateSalesOrderRequest request, CancellationToken cancellationToken)
    {
        return Ok(await salesOrderService.CreateAsync(request, DateTime.UtcNow, cancellationToken));
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = PermissionCatalog.SalesWrite)]
    public async Task<ActionResult<SalesOrderResponse>> ApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await salesOrderService.ApproveAsync(id, DateTime.UtcNow, cancellationToken));
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = PermissionCatalog.SalesWrite)]
    public async Task<ActionResult<SalesOrderResponse>> RejectAsync(Guid id, [FromBody] OrderStatusTransitionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await salesOrderService.RejectAsync(id, request.Notes, DateTime.UtcNow, cancellationToken));
    }

    [HttpPost("{id:guid}/dispatch")]
    [Authorize(Policy = PermissionCatalog.SalesWrite)]
    public async Task<ActionResult<SalesOrderResponse>> DispatchAsync(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await salesOrderService.DispatchAsync(id, DateTime.UtcNow, cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = PermissionCatalog.SalesWrite)]
    public async Task<ActionResult<SalesOrderResponse>> CancelAsync(Guid id, [FromBody] OrderStatusTransitionRequest request, CancellationToken cancellationToken)
    {
        return Ok(await salesOrderService.CancelAsync(id, request.Notes, DateTime.UtcNow, cancellationToken));
    }
}
