using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Praxis.Application.Models;
using Praxis.Application.Services;
using Praxis.Domain.Identity;

namespace Praxis.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(DashboardService dashboardService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PermissionCatalog.DashboardRead)]
    public async Task<ActionResult<DashboardSnapshotResponse>> GetAsync([FromQuery] Guid? branchId, [FromQuery] bool forceRefresh, CancellationToken cancellationToken)
    {
        return Ok(await dashboardService.GetAsync(branchId, forceRefresh, cancellationToken));
    }
}
