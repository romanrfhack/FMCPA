using System.Buffers;
using System.Security.Cryptography;
using FMCPA.Application.Abstractions.Storage;
using Microsoft.Extensions.Configuration;

namespace FMCPA.Infrastructure.Storage.Shared;

public sealed class LocalDocumentBinaryStore : IDocumentBinaryStore
{
    private const string MissingFileState = "MISSING_FILE";
    private const string InvalidPathState = "INVALID_PATH";
    private const string SizeMismatchState = "SIZE_MISMATCH";
    private const string ValidState = "VALID";

    private readonly IConfiguration configuration;

    public LocalDocumentBinaryStore(IConfiguration configuration)
    {
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<StoredBinaryFile> SaveAsync(
        string documentAreaCode,
        string directoryKey,
        string originalFileName,
        string? contentType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var sanitizedOriginalFileName = Path.GetFileName(originalFileName);
        var extension = Path.GetExtension(sanitizedOriginalFileName);
        var normalizedDirectoryKey = NormalizeDirectoryKey(directoryKey);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(normalizedDirectoryKey, storedFileName).Replace('\\', '/');
        var absolutePath = ResolveAbsolutePath(documentAreaCode, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var targetStream = new FileStream(
            absolutePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        long totalBytes = 0;

        try
        {
            while (true)
            {
                var bytesRead = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                hash.AppendData(buffer, 0, bytesRead);
                await targetStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalBytes += bytesRead;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return new StoredBinaryFile(
            relativePath,
            sanitizedOriginalFileName,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
            totalBytes,
            DateTimeOffset.UtcNow,
            Convert.ToHexString(hash.GetHashAndReset()));
    }

    public Task<DocumentBinaryDownload?> OpenReadAsync(
        string documentAreaCode,
        string relativePath,
        string originalFileName,
        string? contentType,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryResolveAbsolutePath(documentAreaCode, relativePath, out var absolutePath))
        {
            return Task.FromResult<DocumentBinaryDownload?>(null);
        }

        if (!File.Exists(absolutePath))
        {
            return Task.FromResult<DocumentBinaryDownload?>(null);
        }

        Stream stream = new FileStream(
            absolutePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult<DocumentBinaryDownload?>(
            new DocumentBinaryDownload(
                stream,
                Path.GetFileName(originalFileName),
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim()));
    }

    public Task<DocumentBinaryInspection> InspectAsync(
        string documentAreaCode,
        string relativePath,
        long expectedSizeBytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryResolveAbsolutePath(documentAreaCode, relativePath, out var absolutePath))
        {
            return Task.FromResult(new DocumentBinaryInspection(false, InvalidPathState, null));
        }

        if (!File.Exists(absolutePath))
        {
            return Task.FromResult(new DocumentBinaryInspection(false, MissingFileState, null));
        }

        var actualSizeBytes = new FileInfo(absolutePath).Length;
        if (expectedSizeBytes > 0 && actualSizeBytes != expectedSizeBytes)
        {
            return Task.FromResult(new DocumentBinaryInspection(true, SizeMismatchState, actualSizeBytes));
        }

        return Task.FromResult(new DocumentBinaryInspection(true, ValidState, actualSizeBytes));
    }

    public Task DeleteIfExistsAsync(
        string documentAreaCode,
        string relativePath,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!TryResolveAbsolutePath(documentAreaCode, relativePath, out var absolutePath))
        {
            return Task.CompletedTask;
        }

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private string ResolveAbsolutePath(string documentAreaCode, string relativePath)
    {
        if (!TryResolveAbsolutePath(documentAreaCode, relativePath, out var absolutePath))
        {
            throw new InvalidOperationException("The document path could not be resolved safely.");
        }

        return absolutePath;
    }

    private bool TryResolveAbsolutePath(string documentAreaCode, string relativePath, out string absolutePath)
    {
        absolutePath = string.Empty;
        var normalizedRelativePath = NormalizeRelativePath(relativePath);
        if (normalizedRelativePath is null)
        {
            return false;
        }

        var rootPath = ResolveRootPath(documentAreaCode);
        var candidatePath = Path.GetFullPath(Path.Combine(rootPath, normalizedRelativePath));
        if (!candidatePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        absolutePath = candidatePath;
        return true;
    }

    private string ResolveRootPath(string documentAreaCode)
    {
        var configuredPath = documentAreaCode.Trim().ToUpperInvariant() switch
        {
            DocumentAreaCodes.MarketsTenantCertificates => configuration["Storage:Markets:MarketTenantCertificatesPath"],
            DocumentAreaCodes.DonationsApplicationEvidences => configuration["Storage:Donations:ApplicationEvidencePath"],
            DocumentAreaCodes.FederationApplicationEvidences => configuration["Storage:Federation:ApplicationEvidencePath"],
            _ => null
        };

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            throw new InvalidOperationException("The document storage path is required.");
        }

        var rootPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(Directory.GetCurrentDirectory(), configuredPath);

        return Path.GetFullPath(rootPath);
    }

    private static string NormalizeDirectoryKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Guid.NewGuid().ToString("N");
        }

        var sanitized = value.Trim()
            .Replace('\\', '/')
            .Replace("..", string.Empty)
            .Trim('/');

        return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N") : sanitized;
    }

    private static string? NormalizeRelativePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(normalized))
        {
            return null;
        }

        var segments = normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0 || segments.Any(segment => segment == ".."))
        {
            return null;
        }

        return Path.Combine(segments);
    }
}
