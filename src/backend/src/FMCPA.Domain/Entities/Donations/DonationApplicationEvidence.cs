using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Domain.Entities.Donations;

public sealed class DonationApplicationEvidence
{
    private DonationApplicationEvidence()
    {
    }

    public DonationApplicationEvidence(
        Guid donationApplicationId,
        int evidenceTypeId,
        string? description,
        string originalFileName,
        string storedRelativePath,
        string? contentType,
        long fileSizeBytes,
        DateTimeOffset uploadedUtc)
    {
        if (donationApplicationId == Guid.Empty)
        {
            throw new ArgumentException("The donation application identifier is required.", nameof(donationApplicationId));
        }

        if (evidenceTypeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(evidenceTypeId), "The evidence type is required.");
        }

        if (fileSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "The evidence file size is required.");
        }

        Id = Guid.NewGuid();
        DonationApplicationId = donationApplicationId;
        EvidenceTypeId = evidenceTypeId;
        Description = NormalizeOptional(description);
        OriginalFileName = NormalizeRequired(originalFileName, nameof(originalFileName));
        StoredRelativePath = NormalizeRequired(storedRelativePath, nameof(storedRelativePath));
        ContentType = NormalizeOptional(contentType);
        FileSizeBytes = fileSizeBytes;
        UploadedUtc = uploadedUtc;
    }

    public Guid Id { get; private set; }

    public Guid DonationApplicationId { get; private set; }

    public int EvidenceTypeId { get; private set; }

    public string? Description { get; private set; }

    public string OriginalFileName { get; private set; } = string.Empty;

    public string StoredRelativePath { get; private set; } = string.Empty;

    public string? ContentType { get; private set; }

    public long FileSizeBytes { get; private set; }

    public DateTimeOffset UploadedUtc { get; private set; }

    public DonationApplication? DonationApplication { get; private set; }

    public EvidenceType? EvidenceType { get; private set; }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required donation evidence value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
