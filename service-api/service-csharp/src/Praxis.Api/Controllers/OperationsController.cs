using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Praxis.Application.Models;
using Praxis.Application.Services;
using Praxis.Domain.Identity;
using Praxis.Domain.Operations;

namespace Praxis.Api.Controllers;

[ApiController]
[Route("api/operations")]
[Authorize]
public sealed class OperationsController(OperationsService operationsService, PlatformService platformService) : ControllerBase
{
    [HttpGet("audit")]
    [Authorize(Policy = PermissionCatalog.AuditRead)]
    public async Task<ActionResult<IReadOnlyCollection<AuditEntryResponse>>> ListAuditEntriesAsync(
        [FromQuery] string? eventType,
        [FromQuery] string? entityName,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        return Ok(await operationsService.ListAuditEntriesAsync(eventType, entityName, limit, cancellationToken));
    }

    [HttpGet("alerts")]
    [Authorize(Policy = PermissionCatalog.AuditRead)]
    public async Task<ActionResult<IReadOnlyCollection<OperationalAlertResponse>>> ListAlertsAsync(
        [FromQuery] Guid? branchId,
        [FromQuery] bool openOnly = true,
        CancellationToken cancellationToken = default)
    {
        return Ok(await operationsService.ListAlertsAsync(branchId, openOnly, cancellationToken));
    }

    [HttpPost("alerts/{id:guid}/resolve")]
    [Authorize(Policy = PermissionCatalog.OpsManage)]
    public async Task<ActionResult<OperationalAlertResponse>> ResolveAlertAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Ok(await operationsService.ResolveAlertAsync(id, DateTime.UtcNow, cancellationToken));
    }

    [HttpGet("branches")]
    [Authorize(Policy = PermissionCatalog.ConfigurationRead)]
    public async Task<ActionResult<IReadOnlyCollection<BranchResponse>>> ListBranchesAsync(CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.ListBranchesAsync(cancellationToken));
    }

    [HttpPost("branches")]
    [Authorize(Policy = PermissionCatalog.ConfigurationWrite)]
    public async Task<ActionResult<BranchResponse>> CreateBranchAsync([FromBody] BranchUpsertRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.UpsertBranchAsync(null, request, DateTime.UtcNow, cancellationToken));
    }

    [HttpPut("branches/{id:guid}")]
    [Authorize(Policy = PermissionCatalog.ConfigurationWrite)]
    public async Task<ActionResult<BranchResponse>> UpdateBranchAsync(Guid id, [FromBody] BranchUpsertRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.UpsertBranchAsync(id, request, DateTime.UtcNow, cancellationToken));
    }

    [HttpGet("cost-centers")]
    [Authorize(Policy = PermissionCatalog.ConfigurationRead)]
    public async Task<ActionResult<IReadOnlyCollection<CostCenterResponse>>> ListCostCentersAsync([FromQuery] Guid? branchId, CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.ListCostCentersAsync(branchId, cancellationToken));
    }

    [HttpPost("cost-centers")]
    [Authorize(Policy = PermissionCatalog.ConfigurationWrite)]
    public async Task<ActionResult<CostCenterResponse>> CreateCostCenterAsync([FromBody] CostCenterUpsertRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.UpsertCostCenterAsync(null, request, DateTime.UtcNow, cancellationToken));
    }

    [HttpPut("cost-centers/{id:guid}")]
    [Authorize(Policy = PermissionCatalog.ConfigurationWrite)]
    public async Task<ActionResult<CostCenterResponse>> UpdateCostCenterAsync(Guid id, [FromBody] CostCenterUpsertRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.UpsertCostCenterAsync(id, request, DateTime.UtcNow, cancellationToken));
    }

    [HttpGet("feature-flags")]
    [Authorize(Policy = PermissionCatalog.ConfigurationRead)]
    public async Task<ActionResult<IReadOnlyCollection<FeatureFlagResponse>>> ListFeatureFlagsAsync([FromQuery] Guid? branchId, CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.ListFeatureFlagsAsync(branchId, cancellationToken));
    }

    [HttpPut("feature-flags/{id:guid}")]
    [Authorize(Policy = PermissionCatalog.ConfigurationWrite)]
    public async Task<ActionResult<FeatureFlagResponse>> UpdateFeatureFlagAsync(Guid id, [FromBody] FeatureFlagUpdateRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.UpdateFeatureFlagAsync(id, request, DateTime.UtcNow, cancellationToken));
    }

    [HttpGet("approval-rules")]
    [Authorize(Policy = PermissionCatalog.ConfigurationRead)]
    public async Task<ActionResult<IReadOnlyCollection<ApprovalRuleResponse>>> ListApprovalRulesAsync(
        [FromQuery] ApprovalModule? module,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.ListApprovalRulesAsync(module, branchId, cancellationToken));
    }

    [HttpPost("approval-rules")]
    [Authorize(Policy = PermissionCatalog.ConfigurationWrite)]
    public async Task<ActionResult<ApprovalRuleResponse>> CreateApprovalRuleAsync([FromBody] ApprovalRuleUpsertRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.UpsertApprovalRuleAsync(null, request, DateTime.UtcNow, cancellationToken));
    }

    [HttpPut("approval-rules/{id:guid}")]
    [Authorize(Policy = PermissionCatalog.ConfigurationWrite)]
    public async Task<ActionResult<ApprovalRuleResponse>> UpdateApprovalRuleAsync(Guid id, [FromBody] ApprovalRuleUpsertRequest request, CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.UpsertApprovalRuleAsync(id, request, DateTime.UtcNow, cancellationToken));
    }

    [HttpGet("approvals")]
    [Authorize(Policy = PermissionCatalog.OpsManage)]
    public async Task<ActionResult<IReadOnlyCollection<ApprovalQueueItemResponse>>> ListApprovalQueueAsync(
        [FromQuery] ApprovalDecisionStatus? status,
        [FromQuery] ApprovalModule? module,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.ListApprovalQueueAsync(status, module, branchId, cancellationToken));
    }

    [HttpGet("notifications")]
    [Authorize(Policy = PermissionCatalog.NotificationRead)]
    public async Task<ActionResult<IReadOnlyCollection<NotificationResponse>>> ListNotificationsAsync(
        [FromQuery] Guid? branchId,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        return Ok(await platformService.ListNotificationsAsync(branchId, unreadOnly, limit, cancellationToken));
    }

    [HttpPost("notifications/{id:guid}/read")]
    [Authorize(Policy = PermissionCatalog.NotificationWrite)]
    public async Task<IActionResult> MarkNotificationReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await platformService.MarkNotificationReadAsync(id, DateTime.UtcNow, cancellationToken);
        return NoContent();
    }

    [HttpPost("notifications/read-all")]
    [Authorize(Policy = PermissionCatalog.NotificationWrite)]
    public async Task<IActionResult> MarkAllNotificationsReadAsync([FromQuery] Guid? branchId, CancellationToken cancellationToken = default)
    {
        await platformService.MarkAllNotificationsReadAsync(branchId, DateTime.UtcNow, cancellationToken);
        return NoContent();
    }
}
