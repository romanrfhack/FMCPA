using FMCPA.Application.Abstractions.Storage;

namespace FMCPA.Infrastructure.Storage.Markets;

public sealed class MarketTenantCertificateStorage : IMarketTenantCertificateStorage
{
    private readonly IDocumentBinaryStore documentBinaryStore;

    public MarketTenantCertificateStorage(
        IDocumentBinaryStore documentBinaryStore)
    {
        this.documentBinaryStore = documentBinaryStore ?? throw new ArgumentNullException(nameof(documentBinaryStore));
    }

    public async Task<StoredMarketTenantCertificateFile> SaveAsync(
        Guid tenantId,
        string originalFileName,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var storedFile = await documentBinaryStore.SaveAsync(
            DocumentAreaCodes.MarketsTenantCertificates,
            tenantId.ToString("N"),
            originalFileName,
            contentType,
            content,
            cancellationToken);

        return new StoredMarketTenantCertificateFile(
            storedFile.RelativePath,
            storedFile.OriginalFileName,
            storedFile.ContentType,
            storedFile.SizeBytes,
            storedFile.UploadedUtc,
            storedFile.Sha256Hex);
    }

    public Task<MarketTenantCertificateDownload?> OpenReadAsync(
        string relativePath,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        return OpenReadInternalAsync(relativePath, originalFileName, contentType, cancellationToken);
    }

    public Task DeleteIfExistsAsync(
        string relativePath,
        CancellationToken cancellationToken)
    {
        return documentBinaryStore.DeleteIfExistsAsync(
            DocumentAreaCodes.MarketsTenantCertificates,
            relativePath,
            cancellationToken);
    }

    private async Task<MarketTenantCertificateDownload?> OpenReadInternalAsync(
        string relativePath,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        var download = await documentBinaryStore.OpenReadAsync(
            DocumentAreaCodes.MarketsTenantCertificates,
            relativePath,
            originalFileName,
            contentType,
            cancellationToken);

        return download is null
            ? null
            : new MarketTenantCertificateDownload(download.Content, download.OriginalFileName, download.ContentType);
    }
}
