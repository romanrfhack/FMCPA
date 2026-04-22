using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Domain.Entities.Markets;

public sealed class Market
{
    private Market()
    {
    }

    public Market(
        string name,
        string borough,
        int statusCatalogEntryId,
        Guid? secretaryGeneralContactId,
        string secretaryGeneralName,
        string? notes)
    {
        if (statusCatalogEntryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCatalogEntryId), "The market status is required.");
        }

        Id = Guid.NewGuid();
        Name = NormalizeRequired(name, nameof(name));
        Borough = NormalizeRequired(borough, nameof(borough));
        StatusCatalogEntryId = statusCatalogEntryId;
        SecretaryGeneralContactId = secretaryGeneralContactId == Guid.Empty ? null : secretaryGeneralContactId;
        SecretaryGeneralName = NormalizeRequired(secretaryGeneralName, nameof(secretaryGeneralName));
        Notes = NormalizeOptional(notes);
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Borough { get; private set; } = string.Empty;

    public int StatusCatalogEntryId { get; private set; }

    public Guid? SecretaryGeneralContactId { get; private set; }

    public string SecretaryGeneralName { get; private set; } = string.Empty;

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset? UpdatedUtc { get; private set; }

    public ModuleStatusCatalogEntry? StatusCatalogEntry { get; private set; }

    public Contact? SecretaryGeneralContact { get; private set; }

    public ICollection<MarketTenant> Tenants { get; } = new List<MarketTenant>();

    public ICollection<MarketIssue> Issues { get; } = new List<MarketIssue>();

    public void SyncStatus(int statusCatalogEntryId)
    {
        if (statusCatalogEntryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCatalogEntryId), "The market status is required.");
        }

        StatusCatalogEntryId = statusCatalogEntryId;
        UpdatedUtc = DateTimeOffset.UtcNow;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required market value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
