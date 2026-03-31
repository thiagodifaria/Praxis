using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Common;
using Praxis.Application.Models;
using Praxis.Application.Persistence;
using Praxis.Domain.Operations;

namespace Praxis.Application.Services;

public sealed class OperationsService(
    IPraxisDbContext dbContext,
    ICurrentUserService currentUserService,
    IDashboardCache dashboardCache,
    PlatformPolicyService platformPolicyService)
{
    public async Task<IReadOnlyCollection<AuditEntryResponse>> ListAuditEntriesAsync(
        string? eventType,
        string? entityName,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Operations, cancellationToken: cancellationToken);

        var normalizedLimit = Math.Clamp(limit, 1, 250);
        var query = dbContext.AuditEntries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var normalizedEventType = eventType.Trim();
            query = query.Where(entry => entry.EventType == normalizedEventType);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            var normalizedEntityName = entityName.Trim();
            query = query.Where(entry => entry.EntityName == normalizedEntityName);
        }

        return await query
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Take(normalizedLimit)
            .Select(entry => new AuditEntryResponse(
                entry.Id,
                entry.EventType,
                entry.EntityName,
                entry.EntityId,
                entry.ActorUserId,
                entry.ActorName,
                entry.MetadataJson,
                entry.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<OperationalAlertResponse>> ListAlertsAsync(Guid? branchId, bool openOnly, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Operations, branchId, cancellationToken);

        var query = dbContext.OperationalAlerts.AsNoTracking().AsQueryable();

        if (openOnly)
        {
            query = query.Where(alert => alert.Status == AlertStatus.Open);
        }

        if (branchId.HasValue)
        {
            query = query.Where(alert => alert.BranchId == null || alert.BranchId == branchId);
        }

        var alerts = await query
            .OrderByDescending(alert => alert.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var branchIds = alerts.Where(alert => alert.BranchId.HasValue).Select(alert => alert.BranchId!.Value).Distinct().ToArray();
        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        return alerts
            .Select(alert => new OperationalAlertResponse(
                alert.Id,
                alert.Code,
                alert.Title,
                alert.Message,
                alert.Source,
                alert.BranchId,
                alert.BranchId.HasValue && branches.TryGetValue(alert.BranchId.Value, out var branchName) ? branchName : null,
                alert.ReferenceId,
                alert.Severity,
                alert.Status,
                alert.CreatedAtUtc,
                alert.ResolvedAtUtc))
            .ToArray();
    }

    public async Task<OperationalAlertResponse> ResolveAlertAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var alert = await dbContext.OperationalAlerts.FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Operational alert not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Operations, alert.BranchId, cancellationToken);

        alert.Resolve(utcNow);

        var branchName = alert.BranchId.HasValue
            ? await dbContext.Branches
                .Where(branch => branch.Id == alert.BranchId.Value)
                .Select(branch => branch.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var actor = currentUserService.GetCurrentUser();
        dbContext.AuditEntries.Add(new AuditEntry(
            "operations.alert.resolved",
            nameof(OperationalAlert),
            alert.Id.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(new { alert.Code, alert.Title, alert.Source, alert.BranchId })));

        await dbContext.SaveChangesAsync(cancellationToken);
        await dashboardCache.RemoveAsync(cancellationToken);

        return new OperationalAlertResponse(
            alert.Id,
            alert.Code,
            alert.Title,
            alert.Message,
            alert.Source,
            alert.BranchId,
            branchName,
            alert.ReferenceId,
            alert.Severity,
            alert.Status,
            alert.CreatedAtUtc,
            alert.ResolvedAtUtc);
    }
}
