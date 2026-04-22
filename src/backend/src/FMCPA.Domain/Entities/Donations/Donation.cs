using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Domain.Entities.Donations;

public sealed class Donation
{
    private Donation()
    {
    }

    public Donation(
        string donorEntityName,
        DateOnly donationDate,
        string donationType,
        decimal baseAmount,
        string reference,
        string? notes,
        int statusCatalogEntryId)
    {
        if (baseAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseAmount), "The donation base amount must be greater than zero.");
        }

        if (statusCatalogEntryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCatalogEntryId), "The donation status is required.");
        }

        Id = Guid.NewGuid();
        DonorEntityName = NormalizeRequired(donorEntityName, nameof(donorEntityName));
        DonationDate = donationDate;
        DonationType = NormalizeRequired(donationType, nameof(donationType));
        BaseAmount = decimal.Round(baseAmount, 2, MidpointRounding.AwayFromZero);
        Reference = NormalizeRequired(reference, nameof(reference));
        Notes = NormalizeOptional(notes);
        StatusCatalogEntryId = statusCatalogEntryId;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string DonorEntityName { get; private set; } = string.Empty;

    public DateOnly DonationDate { get; private set; }

    public string DonationType { get; private set; } = string.Empty;

    public decimal BaseAmount { get; private set; }

    public string Reference { get; private set; } = string.Empty;

    public string? Notes { get; private set; }

    public int StatusCatalogEntryId { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset? UpdatedUtc { get; private set; }

    public ModuleStatusCatalogEntry? StatusCatalogEntry { get; private set; }

    public ICollection<DonationApplication> Applications { get; } = new List<DonationApplication>();

    public void SyncStatus(int statusCatalogEntryId)
    {
        if (statusCatalogEntryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCatalogEntryId), "The donation status is required.");
        }

        StatusCatalogEntryId = statusCatalogEntryId;
        UpdatedUtc = DateTimeOffset.UtcNow;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required donation value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
