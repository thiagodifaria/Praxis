using Praxis.Domain.Operations;

namespace Praxis.Application.Models;

public sealed record BranchUpsertRequest(
    string Code,
    string Name,
    string LegalName,
    string Document,
    string City,
    string State,
    bool IsHeadquarters,
    bool IsActive = true);

public sealed record BranchResponse(
    Guid Id,
    string Code,
    string Name,
    string LegalName,
    string Document,
    string City,
    string State,
    bool IsHeadquarters,
    bool IsActive);

public sealed record CostCenterUpsertRequest(
    Guid BranchId,
    string Code,
    string Name,
    string Description,
    bool IsActive = true);

public sealed record CostCenterResponse(
    Guid Id,
    Guid BranchId,
    string BranchName,
    string Code,
    string Name,
    string Description,
    bool IsActive);

public sealed record FeatureFlagUpdateRequest(
    string DisplayName,
    string Description,
    bool IsEnabled);

public sealed record FeatureFlagResponse(
    Guid Id,
    string ModuleKey,
    string DisplayName,
    string Description,
    bool IsEnabled,
    Guid? BranchId,
    string? BranchName);

public sealed record ApprovalRuleUpsertRequest(
    string Name,
    ApprovalModule Module,
    Guid? BranchId,
    decimal MinimumAmount,
    string RequiredRoleName,
    string Description,
    bool IsActive = true);

public sealed record ApprovalRuleResponse(
    Guid Id,
    string Name,
    ApprovalModule Module,
    Guid? BranchId,
    string? BranchName,
    decimal MinimumAmount,
    string RequiredRoleName,
    string Description,
    bool IsActive);

public sealed record ApprovalActionRequest(string? Notes);

public sealed record ApprovalQueueItemResponse(
    Guid Id,
    ApprovalModule Module,
    ApprovalDecisionStatus Status,
    Guid EntityId,
    string ReferenceNumber,
    decimal RequestedAmount,
    Guid ApprovalRuleId,
    string ApprovalRuleName,
    string RequiredRoleName,
    Guid? BranchId,
    string? BranchName,
    Guid? CostCenterId,
    string? CostCenterName,
    DateTime RequestedAtUtc,
    DateTime? DecidedAtUtc,
    string? RequestedByName,
    string? DecisionByName,
    string? Notes);

public sealed record NotificationResponse(
    Guid Id,
    string RoutingKey,
    string Source,
    string Title,
    string Message,
    NotificationSeverity Severity,
    bool IsRead,
    DateTime PublishedAtUtc,
    Guid? BranchId,
    string? BranchName,
    string MetadataJson);

public sealed record NotificationStreamMessage(
    string RoutingKey,
    string Source,
    string Title,
    string Message,
    NotificationSeverity Severity,
    Guid? BranchId,
    string MetadataJson,
    DateTime PublishedAtUtc);
