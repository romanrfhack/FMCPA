namespace FMCPA.Application.Abstractions.Storage;

public interface IDonationApplicationEvidenceStorage
{
    Task<StoredDonationApplicationEvidenceFile> SaveAsync(
        Guid donationApplicationId,
        string originalFileName,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken);

    Task<DonationApplicationEvidenceDownload?> OpenReadAsync(
        string relativePath,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken);

    Task DeleteIfExistsAsync(
        string relativePath,
        CancellationToken cancellationToken);
}

public sealed record StoredDonationApplicationEvidenceFile(
    string RelativePath,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedUtc,
    string? Sha256Hex);

public sealed record DonationApplicationEvidenceDownload(
    Stream Content,
    string OriginalFileName,
    string ContentType);
