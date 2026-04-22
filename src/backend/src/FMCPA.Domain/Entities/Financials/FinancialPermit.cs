using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Domain.Entities.Financials;

public sealed class FinancialPermit
{
    private FinancialPermit()
    {
    }

    public FinancialPermit(
        string financialName,
        string institutionOrDependency,
        string placeOrStand,
        DateOnly validFrom,
        DateOnly validTo,
        string schedule,
        string negotiatedTerms,
        int statusCatalogEntryId,
        string? notes)
    {
        if (statusCatalogEntryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCatalogEntryId), "The financial permit status is required.");
        }

        if (validTo < validFrom)
        {
            throw new ArgumentOutOfRangeException(nameof(validTo), "The financial permit end date cannot be earlier than the start date.");
        }

        Id = Guid.NewGuid();
        FinancialName = NormalizeRequired(financialName, nameof(financialName));
        InstitutionOrDependency = NormalizeRequired(institutionOrDependency, nameof(institutionOrDependency));
        PlaceOrStand = NormalizeRequired(placeOrStand, nameof(placeOrStand));
        ValidFrom = validFrom;
        ValidTo = validTo;
        Schedule = NormalizeRequired(schedule, nameof(schedule));
        NegotiatedTerms = NormalizeRequired(negotiatedTerms, nameof(negotiatedTerms));
        StatusCatalogEntryId = statusCatalogEntryId;
        Notes = NormalizeOptional(notes);
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string FinancialName { get; private set; } = string.Empty;

    public string InstitutionOrDependency { get; private set; } = string.Empty;

    public string PlaceOrStand { get; private set; } = string.Empty;

    public DateOnly ValidFrom { get; private set; }

    public DateOnly ValidTo { get; private set; }

    public string Schedule { get; private set; } = string.Empty;

    public string NegotiatedTerms { get; private set; } = string.Empty;

    public int StatusCatalogEntryId { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset? UpdatedUtc { get; private set; }

    public ModuleStatusCatalogEntry? StatusCatalogEntry { get; private set; }

    public ICollection<FinancialCredit> Credits { get; } = new List<FinancialCredit>();

    public void SyncStatus(int statusCatalogEntryId)
    {
        if (statusCatalogEntryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCatalogEntryId), "The financial permit status is required.");
        }

        StatusCatalogEntryId = statusCatalogEntryId;
        UpdatedUtc = DateTimeOffset.UtcNow;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required financial permit value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
