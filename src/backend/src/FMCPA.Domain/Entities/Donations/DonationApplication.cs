using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Domain.Entities.Donations;

public sealed class DonationApplication
{
    private DonationApplication()
    {
    }

    public DonationApplication(
        Guid donationId,
        string beneficiaryName,
        Guid? responsibleContactId,
        string responsibleName,
        DateOnly applicationDate,
        decimal appliedAmount,
        int statusCatalogEntryId,
        string? verificationDetails,
        string? closingDetails)
    {
        if (donationId == Guid.Empty)
        {
            throw new ArgumentException("The donation identifier is required.", nameof(donationId));
        }

        if (appliedAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(appliedAmount), "The applied amount must be greater than zero.");
        }

        if (statusCatalogEntryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCatalogEntryId), "The application status is required.");
        }

        Id = Guid.NewGuid();
        DonationId = donationId;
        BeneficiaryName = NormalizeRequired(beneficiaryName, nameof(beneficiaryName));
        ResponsibleContactId = responsibleContactId == Guid.Empty ? null : responsibleContactId;
        ResponsibleName = NormalizeRequired(responsibleName, nameof(responsibleName));
        ApplicationDate = applicationDate;
        AppliedAmount = decimal.Round(appliedAmount, 2, MidpointRounding.AwayFromZero);
        StatusCatalogEntryId = statusCatalogEntryId;
        VerificationDetails = NormalizeOptional(verificationDetails);
        ClosingDetails = NormalizeOptional(closingDetails);
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid DonationId { get; private set; }

    public string BeneficiaryName { get; private set; } = string.Empty;

    public Guid? ResponsibleContactId { get; private set; }

    public string ResponsibleName { get; private set; } = string.Empty;

    public DateOnly ApplicationDate { get; private set; }

    public decimal AppliedAmount { get; private set; }

    public int StatusCatalogEntryId { get; private set; }

    public string? VerificationDetails { get; private set; }

    public string? ClosingDetails { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public Donation? Donation { get; private set; }

    public Contact? ResponsibleContact { get; private set; }

    public ModuleStatusCatalogEntry? StatusCatalogEntry { get; private set; }

    public ICollection<DonationApplicationEvidence> Evidences { get; } = new List<DonationApplicationEvidence>();

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required donation application value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
