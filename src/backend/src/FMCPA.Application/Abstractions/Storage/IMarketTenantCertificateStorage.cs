namespace FMCPA.Application.Abstractions.Storage;

public interface IMarketTenantCertificateStorage
{
    Task<StoredMarketTenantCertificateFile> SaveAsync(
        Guid tenantId,
        string originalFileName,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken);

    Task<MarketTenantCertificateDownload?> OpenReadAsync(
        string relativePath,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken);

    Task DeleteIfExistsAsync(
        string relativePath,
        CancellationToken cancellationToken);
}

public sealed record StoredMarketTenantCertificateFile(
    string RelativePath,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedUtc,
    string? Sha256Hex);

public sealed record MarketTenantCertificateDownload(
    Stream Content,
    string OriginalFileName,
    string ContentType);
