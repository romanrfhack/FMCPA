using FMCPA.Domain.Entities.Security;

namespace FMCPA.Domain.Entities.Shared;

public sealed class ContactIntervention
{
    private ContactIntervention()
    {
    }

    public ContactIntervention(
        Guid contactId,
        string moduleKey,
        string originType,
        string originId,
        string originDisplayName,
        string subject,
        string helpType,
        string outcome,
        string? notes,
        DateTimeOffset occurredUtc,
        Guid createdByUserId,
        DateTimeOffset createdUtc)
    {
        if (contactId == Guid.Empty)
        {
            throw new ArgumentException("The contact identifier is required.", nameof(contactId));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("The creator user identifier is required.", nameof(createdByUserId));
        }

        if (occurredUtc == default)
        {
            throw new ArgumentException("The intervention occurrence date is required.", nameof(occurredUtc));
        }

        if (createdUtc == default)
        {
            throw new ArgumentException("The intervention creation date is required.", nameof(createdUtc));
        }

        Id = Guid.NewGuid();
        ContactId = contactId;
        ModuleKey = NormalizeCode(moduleKey);
        OriginType = NormalizeCode(originType);
        OriginId = NormalizeRequired(originId, nameof(originId));
        OriginDisplayName = NormalizeRequired(originDisplayName, nameof(originDisplayName));
        Subject = NormalizeRequired(subject, nameof(subject));
        HelpType = NormalizeCode(helpType);
        Outcome = NormalizeCode(outcome);
        Notes = NormalizeOptional(notes);
        OccurredUtc = occurredUtc.ToUniversalTime();
        CreatedByUserId = createdByUserId;
        CreatedUtc = createdUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }

    public Guid ContactId { get; private set; }

    public string ModuleKey { get; private set; } = string.Empty;

    public string OriginType { get; private set; } = string.Empty;

    public string OriginId { get; private set; } = string.Empty;

    public string OriginDisplayName { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    public string HelpType { get; private set; } = string.Empty;

    public string Outcome { get; private set; } = string.Empty;

    public string? Notes { get; private set; }

    public DateTimeOffset OccurredUtc { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset? UpdatedUtc { get; private set; }

    public DateTimeOffset? ArchivedUtc { get; private set; }

    public Contact? Contact { get; private set; }

    public ApplicationUser? CreatedByUser { get; private set; }

    private static string NormalizeCode(string value)
    {
        return NormalizeRequired(value, nameof(value)).ToUpperInvariant();
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required intervention value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
