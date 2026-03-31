using Praxis.Domain.Common;

namespace Praxis.Domain.Operations;

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public enum AlertStatus
{
    Open = 0,
    Resolved = 1
}

public sealed class AuditEntry : BaseEntity
{
    public string EventType { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public Guid? ActorUserId { get; private set; }
    public string? ActorName { get; private set; }
    public string MetadataJson { get; private set; } = "{}";

    private AuditEntry()
    {
    }

    public AuditEntry(string eventType, string entityName, string entityId, Guid? actorUserId, string? actorName, string metadataJson)
    {
        EventType = eventType.Trim();
        EntityName = entityName.Trim();
        EntityId = entityId.Trim();
        ActorUserId = actorUserId;
        ActorName = actorName?.Trim();
        MetadataJson = metadataJson;
    }
}

public sealed class OperationalAlert : BaseEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public Guid? BranchId { get; private set; }
    public string? ReferenceId { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public AlertStatus Status { get; private set; } = AlertStatus.Open;
    public DateTime? ResolvedAtUtc { get; private set; }

    private OperationalAlert()
    {
    }

    public OperationalAlert(string code, string title, string message, string source, Guid? branchId, string? referenceId, AlertSeverity severity)
    {
        Code = code.Trim().ToUpperInvariant();
        Title = title.Trim();
        Message = message.Trim();
        Source = source.Trim();
        BranchId = branchId;
        ReferenceId = referenceId?.Trim();
        Severity = severity;
    }

    public void Resolve(DateTime utcNow)
    {
        Status = AlertStatus.Resolved;
        ResolvedAtUtc = utcNow;
        Touch(utcNow);
    }
}
