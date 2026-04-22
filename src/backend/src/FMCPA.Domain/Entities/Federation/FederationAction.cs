using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Domain.Entities.Federation;

public sealed class FederationAction
{
    private FederationAction()
    {
    }

    public FederationAction(
        string actionTypeCode,
        string counterpartyOrInstitution,
        DateOnly actionDate,
        string objective,
        int statusCatalogEntryId,
        string? notes)
    {
        if (statusCatalogEntryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCatalogEntryId), "The federation action status is required.");
        }

        Id = Guid.NewGuid();
        ActionTypeCode = NormalizeCode(actionTypeCode);
        CounterpartyOrInstitution = NormalizeRequired(counterpartyOrInstitution, nameof(counterpartyOrInstitution));
        ActionDate = actionDate;
        Objective = NormalizeRequired(objective, nameof(objective));
        StatusCatalogEntryId = statusCatalogEntryId;
        Notes = NormalizeOptional(notes);
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string ActionTypeCode { get; private set; } = string.Empty;

    public string CounterpartyOrInstitution { get; private set; } = string.Empty;

    public DateOnly ActionDate { get; private set; }

    public string Objective { get; private set; } = string.Empty;

    public int StatusCatalogEntryId { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset? UpdatedUtc { get; private set; }

    public ModuleStatusCatalogEntry? StatusCatalogEntry { get; private set; }

    public ICollection<FederationActionParticipant> Participants { get; } = new List<FederationActionParticipant>();

    public void SyncStatus(int statusCatalogEntryId)
    {
        if (statusCatalogEntryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCatalogEntryId), "The federation action status is required.");
        }

        StatusCatalogEntryId = statusCatalogEntryId;
        UpdatedUtc = DateTimeOffset.UtcNow;
    }

    private static string NormalizeCode(string value)
    {
        return NormalizeRequired(value, nameof(value)).ToUpperInvariant();
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required federation action value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
