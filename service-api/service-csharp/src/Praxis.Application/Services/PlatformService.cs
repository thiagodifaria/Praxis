using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Common;
using Praxis.Application.Models;
using Praxis.Application.Persistence;
using Praxis.Domain.Operations;

namespace Praxis.Application.Services;

public sealed class PlatformService(
    IPraxisDbContext dbContext,
    ICurrentUserService currentUserService,
    IEventBus eventBus,
    PlatformPolicyService platformPolicyService)
{
    public async Task<IReadOnlyCollection<BranchResponse>> ListBranchesAsync(CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Settings, cancellationToken: cancellationToken);

        return await dbContext.Branches
            .AsNoTracking()
            .OrderBy(branch => branch.Name)
            .Select(branch => new BranchResponse(
                branch.Id,
                branch.Code,
                branch.Name,
                branch.LegalName,
                branch.Document,
                branch.City,
                branch.State,
                branch.IsHeadquarters,
                branch.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<BranchResponse> UpsertBranchAsync(Guid? id, BranchUpsertRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Settings, cancellationToken: cancellationToken);

        var existing = await dbContext.Branches.FirstOrDefaultAsync(branch => branch.Code == request.Code.Trim().ToUpperInvariant() && branch.Id != id, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Branch code is already in use.");
        }

        Branch branch;
        if (id.HasValue)
        {
            branch = await dbContext.Branches.FirstOrDefaultAsync(current => current.Id == id.Value, cancellationToken)
                ?? throw new NotFoundException("Branch not found.");

            branch.Update(request.Code, request.Name, request.LegalName, request.Document, request.City, request.State, request.IsHeadquarters, request.IsActive, utcNow);
        }
        else
        {
            branch = new Branch(request.Code, request.Name, request.LegalName, request.Document, request.City, request.State, request.IsHeadquarters, request.IsActive);
            branch.SetCreatedAt(utcNow);
            dbContext.Branches.Add(branch);
        }

        await SaveConfigurationAuditAsync(
            id.HasValue ? "settings.branch.updated" : "settings.branch.created",
            nameof(Branch),
            branch.Id,
            new { branch.Code, branch.Name, branch.City, branch.State, branch.IsActive },
            cancellationToken);

        return new BranchResponse(branch.Id, branch.Code, branch.Name, branch.LegalName, branch.Document, branch.City, branch.State, branch.IsHeadquarters, branch.IsActive);
    }

    public async Task<IReadOnlyCollection<CostCenterResponse>> ListCostCentersAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Settings, branchId, cancellationToken);

        var query = dbContext.CostCenters.AsNoTracking().AsQueryable();
        if (branchId.HasValue)
        {
            query = query.Where(costCenter => costCenter.BranchId == branchId);
        }

        var costCenters = await query.OrderBy(costCenter => costCenter.Name).ToListAsync(cancellationToken);
        var branchIds = costCenters.Select(costCenter => costCenter.BranchId).Distinct().ToArray();
        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        return costCenters
            .Select(costCenter => new CostCenterResponse(
                costCenter.Id,
                costCenter.BranchId,
                branches[costCenter.BranchId],
                costCenter.Code,
                costCenter.Name,
                costCenter.Description,
                costCenter.IsActive))
            .ToArray();
    }

    public async Task<CostCenterResponse> UpsertCostCenterAsync(Guid? id, CostCenterUpsertRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Settings, request.BranchId, cancellationToken);

        var branch = await dbContext.Branches.FirstOrDefaultAsync(current => current.Id == request.BranchId, cancellationToken)
            ?? throw new ValidationException("Branch does not exist.");

        var existing = await dbContext.CostCenters.FirstOrDefaultAsync(
            costCenter => costCenter.BranchId == request.BranchId && costCenter.Code == request.Code.Trim().ToUpperInvariant() && costCenter.Id != id,
            cancellationToken);

        if (existing is not null)
        {
            throw new ConflictException("Cost center code is already in use for this branch.");
        }

        CostCenter costCenter;
        if (id.HasValue)
        {
            costCenter = await dbContext.CostCenters.FirstOrDefaultAsync(current => current.Id == id.Value, cancellationToken)
                ?? throw new NotFoundException("Cost center not found.");

            costCenter.Update(request.BranchId, request.Code, request.Name, request.Description, request.IsActive, utcNow);
        }
        else
        {
            costCenter = new CostCenter(request.BranchId, request.Code, request.Name, request.Description, request.IsActive);
            costCenter.SetCreatedAt(utcNow);
            dbContext.CostCenters.Add(costCenter);
        }

        await SaveConfigurationAuditAsync(
            id.HasValue ? "settings.cost-center.updated" : "settings.cost-center.created",
            nameof(CostCenter),
            costCenter.Id,
            new { costCenter.BranchId, costCenter.Code, costCenter.Name, costCenter.IsActive },
            cancellationToken);

        return new CostCenterResponse(costCenter.Id, costCenter.BranchId, branch.Name, costCenter.Code, costCenter.Name, costCenter.Description, costCenter.IsActive);
    }

    public async Task<IReadOnlyCollection<FeatureFlagResponse>> ListFeatureFlagsAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Settings, branchId, cancellationToken);

        var query = dbContext.ModuleFeatureFlags.AsNoTracking().AsQueryable();
        if (branchId.HasValue)
        {
            query = query.Where(flag => flag.BranchId == branchId || flag.BranchId == null);
        }

        var flags = await query
            .OrderBy(flag => flag.ModuleKey)
            .ThenBy(flag => flag.BranchId.HasValue ? 1 : 0)
            .ToListAsync(cancellationToken);

        var branchIds = flags.Where(flag => flag.BranchId.HasValue).Select(flag => flag.BranchId!.Value).Distinct().ToArray();
        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        return flags
            .Select(flag => new FeatureFlagResponse(
                flag.Id,
                flag.ModuleKey,
                flag.DisplayName,
                flag.Description,
                flag.IsEnabled,
                flag.BranchId,
                flag.BranchId.HasValue && branches.TryGetValue(flag.BranchId.Value, out var branchName) ? branchName : null))
            .ToArray();
    }

    public async Task<FeatureFlagResponse> UpdateFeatureFlagAsync(Guid id, FeatureFlagUpdateRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var flag = await dbContext.ModuleFeatureFlags.FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Feature flag not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Settings, flag.BranchId, cancellationToken);

        flag.Update(request.DisplayName, request.Description, request.IsEnabled, utcNow);

        await SaveConfigurationAuditAsync(
            "settings.feature-flag.updated",
            nameof(ModuleFeatureFlag),
            flag.Id,
            new { flag.ModuleKey, flag.BranchId, flag.IsEnabled },
            cancellationToken);

        var branchName = flag.BranchId.HasValue
            ? await dbContext.Branches.Where(branch => branch.Id == flag.BranchId.Value).Select(branch => branch.Name).FirstOrDefaultAsync(cancellationToken)
            : null;

        await eventBus.PublishAsync(
            "settings.feature-flag.updated",
            new
            {
                flag.Id,
                flag.ModuleKey,
                flag.BranchId,
                BranchName = branchName,
                flag.IsEnabled
            },
            cancellationToken);

        return new FeatureFlagResponse(flag.Id, flag.ModuleKey, flag.DisplayName, flag.Description, flag.IsEnabled, flag.BranchId, branchName);
    }

    public async Task<IReadOnlyCollection<ApprovalRuleResponse>> ListApprovalRulesAsync(ApprovalModule? module = null, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Settings, branchId, cancellationToken);

        var query = dbContext.ApprovalRules.AsNoTracking().AsQueryable();
        if (module.HasValue)
        {
            query = query.Where(rule => rule.Module == module.Value);
        }

        if (branchId.HasValue)
        {
            query = query.Where(rule => rule.BranchId == null || rule.BranchId == branchId);
        }

        var rules = await query
            .OrderBy(rule => rule.Module)
            .ThenByDescending(rule => rule.MinimumAmount)
            .ToListAsync(cancellationToken);

        var branchIds = rules.Where(rule => rule.BranchId.HasValue).Select(rule => rule.BranchId!.Value).Distinct().ToArray();
        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        return rules
            .Select(rule => new ApprovalRuleResponse(
                rule.Id,
                rule.Name,
                rule.Module,
                rule.BranchId,
                rule.BranchId.HasValue && branches.TryGetValue(rule.BranchId.Value, out var branchName) ? branchName : null,
                rule.MinimumAmount,
                rule.RequiredRoleName,
                rule.Description,
                rule.IsActive))
            .ToArray();
    }

    public async Task<ApprovalRuleResponse> UpsertApprovalRuleAsync(Guid? id, ApprovalRuleUpsertRequest request, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Settings, request.BranchId, cancellationToken);

        string? branchName = null;
        if (request.BranchId.HasValue)
        {
            branchName = await dbContext.Branches
                .Where(branch => branch.Id == request.BranchId.Value)
                .Select(branch => branch.Name)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new ValidationException("Branch does not exist.");
        }

        ApprovalRule rule;
        if (id.HasValue)
        {
            rule = await dbContext.ApprovalRules.FirstOrDefaultAsync(current => current.Id == id.Value, cancellationToken)
                ?? throw new NotFoundException("Approval rule not found.");

            rule.Update(request.Name, request.Module, request.BranchId, request.MinimumAmount, request.RequiredRoleName, request.Description, request.IsActive, utcNow);
        }
        else
        {
            rule = new ApprovalRule(request.Name, request.Module, request.BranchId, request.MinimumAmount, request.RequiredRoleName, request.Description, request.IsActive);
            rule.SetCreatedAt(utcNow);
            dbContext.ApprovalRules.Add(rule);
        }

        await SaveConfigurationAuditAsync(
            id.HasValue ? "settings.approval-rule.updated" : "settings.approval-rule.created",
            nameof(ApprovalRule),
            rule.Id,
            new { rule.Name, rule.Module, rule.BranchId, rule.MinimumAmount, rule.RequiredRoleName, rule.IsActive },
            cancellationToken);

        return new ApprovalRuleResponse(rule.Id, rule.Name, rule.Module, rule.BranchId, branchName, rule.MinimumAmount, rule.RequiredRoleName, rule.Description, rule.IsActive);
    }

    public async Task<IReadOnlyCollection<ApprovalQueueItemResponse>> ListApprovalQueueAsync(
        ApprovalDecisionStatus? status = null,
        ApprovalModule? module = null,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Operations, branchId, cancellationToken);

        var query = dbContext.ApprovalDecisions.AsNoTracking().AsQueryable();
        if (status.HasValue)
        {
            query = query.Where(decision => decision.Status == status.Value);
        }

        if (module.HasValue)
        {
            query = query.Where(decision => decision.Module == module.Value);
        }

        if (branchId.HasValue)
        {
            query = query.Where(decision => decision.BranchId == branchId);
        }

        var decisions = await query
            .OrderByDescending(decision => decision.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        var branchIds = decisions.Where(decision => decision.BranchId.HasValue).Select(decision => decision.BranchId!.Value).Distinct().ToArray();
        var costCenterIds = decisions.Where(decision => decision.CostCenterId.HasValue).Select(decision => decision.CostCenterId!.Value).Distinct().ToArray();
        var ruleIds = decisions.Select(decision => decision.ApprovalRuleId).Distinct().ToArray();

        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        var costCenters = await dbContext.CostCenters
            .AsNoTracking()
            .Where(costCenter => costCenterIds.Contains(costCenter.Id))
            .ToDictionaryAsync(costCenter => costCenter.Id, costCenter => costCenter.Name, cancellationToken);

        var rules = await dbContext.ApprovalRules
            .AsNoTracking()
            .Where(rule => ruleIds.Contains(rule.Id))
            .ToDictionaryAsync(rule => rule.Id, cancellationToken);

        return decisions
            .Select(decision =>
            {
                var rule = rules[decision.ApprovalRuleId];
                return new ApprovalQueueItemResponse(
                    decision.Id,
                    decision.Module,
                    decision.Status,
                    decision.EntityId,
                    decision.ReferenceNumber,
                    decision.RequestedAmount,
                    decision.ApprovalRuleId,
                    rule.Name,
                    rule.RequiredRoleName,
                    decision.BranchId,
                    decision.BranchId.HasValue && branches.TryGetValue(decision.BranchId.Value, out var branchName) ? branchName : null,
                    decision.CostCenterId,
                    decision.CostCenterId.HasValue && costCenters.TryGetValue(decision.CostCenterId.Value, out var costCenterName) ? costCenterName : null,
                    decision.RequestedAtUtc,
                    decision.DecidedAtUtc,
                    decision.RequestedByName,
                    decision.DecisionByName,
                    decision.Notes);
            })
            .ToArray();
    }

    public async Task<IReadOnlyCollection<NotificationResponse>> ListNotificationsAsync(Guid? branchId, bool unreadOnly, int limit, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Notifications, branchId, cancellationToken);

        var normalizedLimit = Math.Clamp(limit, 1, 100);
        var query = dbContext.RealtimeNotifications.AsNoTracking().AsQueryable();

        if (unreadOnly)
        {
            query = query.Where(notification => !notification.IsRead);
        }

        if (branchId.HasValue)
        {
            query = query.Where(notification => notification.BranchId == null || notification.BranchId == branchId);
        }

        var notifications = await query
            .OrderByDescending(notification => notification.PublishedAtUtc)
            .Take(normalizedLimit)
            .ToListAsync(cancellationToken);

        var branchIds = notifications.Where(notification => notification.BranchId.HasValue).Select(notification => notification.BranchId!.Value).Distinct().ToArray();
        var branches = await dbContext.Branches
            .AsNoTracking()
            .Where(branch => branchIds.Contains(branch.Id))
            .ToDictionaryAsync(branch => branch.Id, branch => branch.Name, cancellationToken);

        return notifications
            .Select(notification => new NotificationResponse(
                notification.Id,
                notification.RoutingKey,
                notification.Source,
                notification.Title,
                notification.Message,
                notification.Severity,
                notification.IsRead,
                notification.PublishedAtUtc,
                notification.BranchId,
                notification.BranchId.HasValue && branches.TryGetValue(notification.BranchId.Value, out var branchName) ? branchName : null,
                notification.MetadataJson))
            .ToArray();
    }

    public async Task MarkNotificationReadAsync(Guid id, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var notification = await dbContext.RealtimeNotifications.FirstOrDefaultAsync(current => current.Id == id, cancellationToken)
            ?? throw new NotFoundException("Notification not found.");

        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Notifications, notification.BranchId, cancellationToken);

        if (!notification.IsRead)
        {
            notification.MarkRead(utcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllNotificationsReadAsync(Guid? branchId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        await platformPolicyService.EnsureModuleEnabledAsync(ModuleCatalog.Notifications, branchId, cancellationToken);

        var notifications = await dbContext.RealtimeNotifications
            .Where(notification =>
                !notification.IsRead &&
                (!branchId.HasValue || notification.BranchId == null || notification.BranchId == branchId))
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.MarkRead(utcNow);
        }

        if (notifications.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SaveConfigurationAuditAsync(string eventType, string entityName, Guid entityId, object metadata, CancellationToken cancellationToken)
    {
        var actor = currentUserService.GetCurrentUser();
        dbContext.AuditEntries.Add(new AuditEntry(
            eventType,
            entityName,
            entityId.ToString(),
            actor.UserId,
            actor.Name,
            JsonSerializer.Serialize(metadata)));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
