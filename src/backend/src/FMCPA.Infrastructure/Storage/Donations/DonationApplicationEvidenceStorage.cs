using FMCPA.Application.Abstractions.Storage;

namespace FMCPA.Infrastructure.Storage.Donations;

public sealed class DonationApplicationEvidenceStorage : IDonationApplicationEvidenceStorage
{
    private readonly IDocumentBinaryStore documentBinaryStore;

    public DonationApplicationEvidenceStorage(IDocumentBinaryStore documentBinaryStore)
    {
        this.documentBinaryStore = documentBinaryStore ?? throw new ArgumentNullException(nameof(documentBinaryStore));
    }

    public async Task<StoredDonationApplicationEvidenceFile> SaveAsync(
        Guid donationApplicationId,
        string originalFileName,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var storedFile = await documentBinaryStore.SaveAsync(
            DocumentAreaCodes.DonationsApplicationEvidences,
            donationApplicationId.ToString("N"),
            originalFileName,
            contentType,
            content,
            cancellationToken);

        return new StoredDonationApplicationEvidenceFile(
            storedFile.RelativePath,
            storedFile.OriginalFileName,
            storedFile.ContentType,
            storedFile.SizeBytes,
            storedFile.UploadedUtc,
            storedFile.Sha256Hex);
    }

    public Task<DonationApplicationEvidenceDownload?> OpenReadAsync(
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
            DocumentAreaCodes.DonationsApplicationEvidences,
            relativePath,
            cancellationToken);
    }

    private async Task<DonationApplicationEvidenceDownload?> OpenReadInternalAsync(
        string relativePath,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        var download = await documentBinaryStore.OpenReadAsync(
            DocumentAreaCodes.DonationsApplicationEvidences,
            relativePath,
            originalFileName,
            contentType,
            cancellationToken);

        return download is null
            ? null
            : new DonationApplicationEvidenceDownload(download.Content, download.OriginalFileName, download.ContentType);
    }
}
