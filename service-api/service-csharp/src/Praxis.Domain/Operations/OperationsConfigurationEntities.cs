using Praxis.Domain.Common;

namespace Praxis.Domain.Operations;

public enum ApprovalModule
{
    SalesOrder = 0,
    PurchaseOrder = 1
}

public enum ApprovalDecisionStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum ApprovalWorkflowStatus
{
    NotRequired = 0,
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum NotificationSeverity
{
    Info = 0,
    Success = 1,
    Warning = 2,
    Critical = 3
}

public static class ModuleCatalog
{
    public const string Dashboard = "dashboard";
    public const string Catalog = "catalog";
    public const string Customers = "customers";
    public const string Sales = "sales";
    public const string Purchasing = "purchasing";
    public const string Inventory = "inventory";
    public const string Billing = "billing";
    public const string Reporting = "reporting";
    public const string Operations = "operations";
    public const string Notifications = "notifications";
    public const string Settings = "settings";

    public static IReadOnlyList<string> All =>
    [
        Dashboard,
        Catalog,
        Customers,
        Sales,
        Purchasing,
        Inventory,
        Billing,
        Reporting,
        Operations,
        Notifications,
        Settings
    ];
}

public sealed class Branch : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string LegalName { get; private set; } = string.Empty;
    public string Document { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public bool IsHeadquarters { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Branch()
    {
    }

    public Branch(
        string code,
        string name,
        string legalName,
        string document,
        string city,
        string state,
        bool isHeadquarters,
        bool isActive)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        LegalName = legalName.Trim();
        Document = document.Trim();
        City = city.Trim();
        State = state.Trim().ToUpperInvariant();
        IsHeadquarters = isHeadquarters;
        IsActive = isActive;
    }

    public void Update(
        string code,
        string name,
        string legalName,
        string document,
        string city,
        string state,
        bool isHeadquarters,
        bool isActive,
        DateTime utcNow)
    {
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        LegalName = legalName.Trim();
        Document = document.Trim();
        City = city.Trim();
        State = state.Trim().ToUpperInvariant();
        IsHeadquarters = isHeadquarters;
        IsActive = isActive;
        Touch(utcNow);
    }
}

public sealed class CostCenter : BaseEntity
{
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private CostCenter()
    {
    }

    public CostCenter(Guid branchId, string code, string name, string description, bool isActive)
    {
        BranchId = branchId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description.Trim();
        IsActive = isActive;
    }

    public void Update(Guid branchId, string code, string name, string description, bool isActive, DateTime utcNow)
    {
        BranchId = branchId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description.Trim();
        IsActive = isActive;
        Touch(utcNow);
    }
}

public sealed class ModuleFeatureFlag : BaseEntity
{
    public string ModuleKey { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid? BranchId { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    private ModuleFeatureFlag()
    {
    }

    public ModuleFeatureFlag(string moduleKey, string displayName, string description, Guid? branchId, bool isEnabled)
    {
        ModuleKey = moduleKey.Trim().ToLowerInvariant();
        DisplayName = displayName.Trim();
        Description = description.Trim();
        BranchId = branchId;
        IsEnabled = isEnabled;
    }

    public void Update(string displayName, string description, bool isEnabled, DateTime utcNow)
    {
        DisplayName = displayName.Trim();
        Description = description.Trim();
        IsEnabled = isEnabled;
        Touch(utcNow);
    }
}

public sealed class ApprovalRule : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public ApprovalModule Module { get; private set; }
    public Guid? BranchId { get; private set; }
    public decimal MinimumAmount { get; private set; }
    public string RequiredRoleName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private ApprovalRule()
    {
    }

    public ApprovalRule(
        string name,
        ApprovalModule module,
        Guid? branchId,
        decimal minimumAmount,
        string requiredRoleName,
        string description,
        bool isActive)
    {
        Name = name.Trim();
        Module = module;
        BranchId = branchId;
        MinimumAmount = minimumAmount;
        RequiredRoleName = requiredRoleName.Trim().ToLowerInvariant();
        Description = description.Trim();
        IsActive = isActive;
    }

    public void Update(
        string name,
        ApprovalModule module,
        Guid? branchId,
        decimal minimumAmount,
        string requiredRoleName,
        string description,
        bool isActive,
        DateTime utcNow)
    {
        Name = name.Trim();
        Module = module;
        BranchId = branchId;
        MinimumAmount = minimumAmount;
        RequiredRoleName = requiredRoleName.Trim().ToLowerInvariant();
        Description = description.Trim();
        IsActive = isActive;
        Touch(utcNow);
    }
}

public sealed class ApprovalDecision : BaseEntity
{
    public ApprovalModule Module { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid ApprovalRuleId { get; private set; }
    public ApprovalDecisionStatus Status { get; private set; } = ApprovalDecisionStatus.Pending;
    public string ReferenceNumber { get; private set; } = string.Empty;
    public decimal RequestedAmount { get; private set; }
    public Guid? BranchId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public Guid? RequestedByUserId { get; private set; }
    public string? RequestedByName { get; private set; }
    public Guid? DecisionByUserId { get; private set; }
    public string? DecisionByName { get; private set; }
    public string? Notes { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? DecidedAtUtc { get; private set; }

    private ApprovalDecision()
    {
    }

    public ApprovalDecision(
        ApprovalModule module,
        Guid entityId,
        Guid approvalRuleId,
        string referenceNumber,
        decimal requestedAmount,
        Guid? branchId,
        Guid? costCenterId,
        Guid? requestedByUserId,
        string? requestedByName,
        string? notes,
        DateTime requestedAtUtc)
    {
        Module = module;
        EntityId = entityId;
        ApprovalRuleId = approvalRuleId;
        ReferenceNumber = referenceNumber.Trim().ToUpperInvariant();
        RequestedAmount = requestedAmount;
        BranchId = branchId;
        CostCenterId = costCenterId;
        RequestedByUserId = requestedByUserId;
        RequestedByName = requestedByName?.Trim();
        Notes = notes?.Trim();
        RequestedAtUtc = requestedAtUtc;
    }

    public void Approve(Guid? decisionByUserId, string? decisionByName, string? notes, DateTime utcNow)
    {
        Status = ApprovalDecisionStatus.Approved;
        DecisionByUserId = decisionByUserId;
        DecisionByName = decisionByName?.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? Notes : notes.Trim();
        DecidedAtUtc = utcNow;
        Touch(utcNow);
    }

    public void Reject(Guid? decisionByUserId, string? decisionByName, string? notes, DateTime utcNow)
    {
        Status = ApprovalDecisionStatus.Rejected;
        DecisionByUserId = decisionByUserId;
        DecisionByName = decisionByName?.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? Notes : notes.Trim();
        DecidedAtUtc = utcNow;
        Touch(utcNow);
    }
}

public sealed class RealtimeNotification : BaseEntity
{
    public string RoutingKey { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationSeverity Severity { get; private set; } = NotificationSeverity.Info;
    public Guid? BranchId { get; private set; }
    public Guid? RecipientUserId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime PublishedAtUtc { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public string MetadataJson { get; private set; } = "{}";

    private RealtimeNotification()
    {
    }

    public RealtimeNotification(
        string routingKey,
        string source,
        string title,
        string message,
        NotificationSeverity severity,
        Guid? branchId,
        Guid? recipientUserId,
        string metadataJson,
        DateTime publishedAtUtc)
    {
        RoutingKey = routingKey.Trim().ToLowerInvariant();
        Source = source.Trim().ToLowerInvariant();
        Title = title.Trim();
        Message = message.Trim();
        Severity = severity;
        BranchId = branchId;
        RecipientUserId = recipientUserId;
        MetadataJson = metadataJson;
        PublishedAtUtc = publishedAtUtc;
    }

    public void MarkRead(DateTime utcNow)
    {
        IsRead = true;
        ReadAtUtc = utcNow;
        Touch(utcNow);
    }
}
