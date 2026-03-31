using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Praxis.Application.Models;
using Praxis.Application.Services;
using Praxis.Domain.Identity;

namespace Praxis.Api.Controllers;

[ApiController]
[Route("api/reporting")]
[Authorize]
public sealed class ReportingController(ReportingService reportingService) : ControllerBase
{
    [HttpGet("overview")]
    [Authorize(Policy = PermissionCatalog.ReportingRead)]
    public async Task<ActionResult<ReportingOverviewResponse>> GetOverviewAsync(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? costCenterId,
        CancellationToken cancellationToken)
    {
        return Ok(await reportingService.GetOverviewAsync(fromUtc, toUtc, branchId, costCenterId, cancellationToken));
    }

    [HttpGet("inventory-turnover")]
    [Authorize(Policy = PermissionCatalog.ReportingRead)]
    public async Task<ActionResult<InventoryTurnoverResponse>> GetInventoryTurnoverAsync(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken)
    {
        return Ok(await reportingService.GetInventoryTurnoverAsync(fromUtc, toUtc, branchId, cancellationToken));
    }

    [HttpGet("overdue-receivables")]
    [Authorize(Policy = PermissionCatalog.ReportingRead)]
    public async Task<ActionResult<OverdueReceivablesReportResponse>> GetOverdueReceivablesAsync(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? costCenterId,
        CancellationToken cancellationToken)
    {
        return Ok(await reportingService.GetOverdueReceivablesAsync(branchId, costCenterId, cancellationToken));
    }
}
