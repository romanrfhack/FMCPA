namespace FMCPA.Application.Abstractions.Storage;

public interface IDocumentBinaryStore
{
    Task<StoredBinaryFile> SaveAsync(
        string documentAreaCode,
        string directoryKey,
        string originalFileName,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken);

    Task<DocumentBinaryDownload?> OpenReadAsync(
        string documentAreaCode,
        string relativePath,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken);

    Task<DocumentBinaryInspection> InspectAsync(
        string documentAreaCode,
        string relativePath,
        long expectedSizeBytes,
        CancellationToken cancellationToken);

    Task DeleteIfExistsAsync(
        string documentAreaCode,
        string relativePath,
        CancellationToken cancellationToken);
}

public sealed record StoredBinaryFile(
    string RelativePath,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedUtc,
    string? Sha256Hex);

public sealed record DocumentBinaryDownload(
    Stream Content,
    string OriginalFileName,
    string ContentType);

public sealed record DocumentBinaryInspection(
    bool Exists,
    string IntegrityState,
    long? ActualSizeBytes);
