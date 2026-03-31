using Microsoft.EntityFrameworkCore;
using Praxis.Application.Abstractions;
using Praxis.Application.Common;
using Praxis.Application.Persistence;
using Praxis.Domain.Operations;

namespace Praxis.Application.Services;

public sealed class PlatformPolicyService(IPraxisDbContext dbContext)
{
    public async Task EnsureModuleEnabledAsync(string moduleKey, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        var normalizedKey = moduleKey.Trim().ToLowerInvariant();

        var flags = await dbContext.ModuleFeatureFlags
            .AsNoTracking()
            .Where(flag => flag.ModuleKey == normalizedKey && (flag.BranchId == null || flag.BranchId == branchId))
            .ToListAsync(cancellationToken);

        var effectiveFlag = branchId.HasValue
            ? flags.FirstOrDefault(flag => flag.BranchId == branchId) ?? flags.FirstOrDefault(flag => flag.BranchId == null)
            : flags.FirstOrDefault(flag => flag.BranchId == null);

        if (effectiveFlag is { IsEnabled: false })
        {
            throw new ForbiddenException($"Module '{normalizedKey}' is currently disabled.");
        }
    }

    public async Task<ApprovalRule?> FindRequiredApprovalRuleAsync(
        ApprovalModule module,
        decimal requestedAmount,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ApprovalRules
            .AsNoTracking()
            .Where(rule =>
                rule.IsActive &&
                rule.Module == module &&
                rule.MinimumAmount <= requestedAmount &&
                (rule.BranchId == null || rule.BranchId == branchId))
            .OrderByDescending(rule => rule.BranchId == branchId)
            .ThenByDescending(rule => rule.MinimumAmount)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void EnsureUserCanApprove(ApprovalRule rule, CurrentUserContext currentUser)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new ForbiddenException("Authenticated users are required to approve records.");
        }

        if (string.Equals(currentUser.Role, "administrator", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.Equals(currentUser.Role, rule.RequiredRoleName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException($"This approval requires the role '{rule.RequiredRoleName}'.");
        }
    }
}
