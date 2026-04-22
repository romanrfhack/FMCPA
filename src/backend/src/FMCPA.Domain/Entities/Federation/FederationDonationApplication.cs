using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Domain.Entities.Federation;

public sealed class FederationDonationApplication
{
    private FederationDonationApplication()
    {
    }

    public FederationDonationApplication(
        Guid federationDonationId,
        string beneficiaryOrDestinationName,
        DateOnly applicationDate,
        decimal appliedAmount,
        int statusCatalogEntryId,
        string? verificationDetails,
        string? closingDetails)
    {
        if (federationDonationId == Guid.Empty)
        {
            throw new ArgumentException("The federation donation identifier is required.", nameof(federationDonationId));
        }

        if (appliedAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(appliedAmount), "The federation applied amount must be greater than zero.");
        }

        if (statusCatalogEntryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCatalogEntryId), "The federation application status is required.");
        }

        Id = Guid.NewGuid();
        FederationDonationId = federationDonationId;
        BeneficiaryOrDestinationName = NormalizeRequired(beneficiaryOrDestinationName, nameof(beneficiaryOrDestinationName));
        ApplicationDate = applicationDate;
        AppliedAmount = decimal.Round(appliedAmount, 2, MidpointRounding.AwayFromZero);
        StatusCatalogEntryId = statusCatalogEntryId;
        VerificationDetails = NormalizeOptional(verificationDetails);
        ClosingDetails = NormalizeOptional(closingDetails);
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid FederationDonationId { get; private set; }

    public string BeneficiaryOrDestinationName { get; private set; } = string.Empty;

    public DateOnly ApplicationDate { get; private set; }

    public decimal AppliedAmount { get; private set; }

    public int StatusCatalogEntryId { get; private set; }

    public string? VerificationDetails { get; private set; }

    public string? ClosingDetails { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public FederationDonation? FederationDonation { get; private set; }

    public ModuleStatusCatalogEntry? StatusCatalogEntry { get; private set; }

    public ICollection<FederationDonationApplicationEvidence> Evidences { get; } = new List<FederationDonationApplicationEvidence>();

    public ICollection<FederationDonationApplicationCommission> Commissions { get; } = new List<FederationDonationApplicationCommission>();

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required federation donation application value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
