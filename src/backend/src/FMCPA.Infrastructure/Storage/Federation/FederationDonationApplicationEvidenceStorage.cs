using FMCPA.Application.Abstractions.Storage;

namespace FMCPA.Infrastructure.Storage.Federation;

public sealed class FederationDonationApplicationEvidenceStorage : IFederationDonationApplicationEvidenceStorage
{
    private readonly IDocumentBinaryStore documentBinaryStore;

    public FederationDonationApplicationEvidenceStorage(IDocumentBinaryStore documentBinaryStore)
    {
        this.documentBinaryStore = documentBinaryStore ?? throw new ArgumentNullException(nameof(documentBinaryStore));
    }

    public async Task<StoredFederationDonationApplicationEvidenceFile> SaveAsync(
        Guid federationDonationApplicationId,
        string originalFileName,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var storedFile = await documentBinaryStore.SaveAsync(
            DocumentAreaCodes.FederationApplicationEvidences,
            federationDonationApplicationId.ToString("N"),
            originalFileName,
            contentType,
            content,
            cancellationToken);

        return new StoredFederationDonationApplicationEvidenceFile(
            storedFile.RelativePath,
            storedFile.OriginalFileName,
            storedFile.ContentType,
            storedFile.SizeBytes,
            storedFile.UploadedUtc,
            storedFile.Sha256Hex);
    }

    public Task<FederationDonationApplicationEvidenceDownload?> OpenReadAsync(
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
            DocumentAreaCodes.FederationApplicationEvidences,
            relativePath,
            cancellationToken);
    }

    private async Task<FederationDonationApplicationEvidenceDownload?> OpenReadInternalAsync(
        string relativePath,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        var download = await documentBinaryStore.OpenReadAsync(
            DocumentAreaCodes.FederationApplicationEvidences,
            relativePath,
            originalFileName,
            contentType,
            cancellationToken);

        return download is null
            ? null
            : new FederationDonationApplicationEvidenceDownload(download.Content, download.OriginalFileName, download.ContentType);
    }
}
