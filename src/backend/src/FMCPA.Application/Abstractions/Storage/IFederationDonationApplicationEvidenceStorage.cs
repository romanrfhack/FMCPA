namespace FMCPA.Application.Abstractions.Storage;

public interface IFederationDonationApplicationEvidenceStorage
{
    Task<StoredFederationDonationApplicationEvidenceFile> SaveAsync(
        Guid federationDonationApplicationId,
        string originalFileName,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken);

    Task<FederationDonationApplicationEvidenceDownload?> OpenReadAsync(
        string relativePath,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken);

    Task DeleteIfExistsAsync(
        string relativePath,
        CancellationToken cancellationToken);
}

public sealed record StoredFederationDonationApplicationEvidenceFile(
    string RelativePath,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedUtc,
    string? Sha256Hex);

public sealed record FederationDonationApplicationEvidenceDownload(
    Stream Content,
    string OriginalFileName,
    string ContentType);
