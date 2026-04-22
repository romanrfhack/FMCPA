namespace FMCPA.Domain.Entities.Audit;

public sealed class AuditEvent
{
    private AuditEvent()
    {
    }

    public AuditEvent(
        string moduleCode,
        string moduleName,
        string entityType,
        string entityId,
        string actionType,
        string title,
        string detail,
        string? relatedStatusCode,
        string? reference,
        string navigationPath,
        bool isCloseEvent,
        string? metadataJson,
        DateTimeOffset? occurredUtc = null)
    {
        Id = Guid.NewGuid();
        OccurredUtc = occurredUtc ?? DateTimeOffset.UtcNow;
        ModuleCode = NormalizeCode(moduleCode, nameof(moduleCode));
        ModuleName = NormalizeRequired(moduleName, nameof(moduleName));
        EntityType = NormalizeCode(entityType, nameof(entityType));
        EntityId = NormalizeRequired(entityId, nameof(entityId));
        ActionType = NormalizeCode(actionType, nameof(actionType));
        Title = NormalizeRequired(title, nameof(title));
        Detail = NormalizeRequired(detail, nameof(detail));
        RelatedStatusCode = NormalizeOptionalCode(relatedStatusCode);
        Reference = NormalizeOptional(reference);
        NavigationPath = NormalizeRequired(navigationPath, nameof(navigationPath));
        IsCloseEvent = isCloseEvent;
        MetadataJson = NormalizeOptional(metadataJson);
    }

    public Guid Id { get; private set; }

    public DateTimeOffset OccurredUtc { get; private set; }

    public string ModuleCode { get; private set; } = string.Empty;

    public string ModuleName { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string ActionType { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Detail { get; private set; } = string.Empty;

    public string? RelatedStatusCode { get; private set; }

    public string? Reference { get; private set; }

    public string NavigationPath { get; private set; } = string.Empty;

    public bool IsCloseEvent { get; private set; }

    public string? MetadataJson { get; private set; }

    private static string NormalizeCode(string value, string paramName)
    {
        return NormalizeRequired(value, paramName).ToUpperInvariant();
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required audit event value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeOptionalCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }
}
