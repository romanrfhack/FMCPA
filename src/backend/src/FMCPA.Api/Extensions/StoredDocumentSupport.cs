using FMCPA.Application.Abstractions.Storage;
using FMCPA.Domain.Entities.Documents;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Extensions;

internal static class StoredDocumentSupport
{
    public static StoredDocument CreateStoredDocument(
        string moduleCode,
        string documentAreaCode,
        string entityType,
        Guid entityId,
        string originalFileName,
        string storedRelativePath,
        string? contentType,
        long sizeBytes,
        DateTimeOffset createdUtc,
        string? sha256Hex,
        bool isLegacyBackfill = false)
    {
        return new StoredDocument(
            moduleCode,
            documentAreaCode,
            entityType,
            entityId,
            originalFileName,
            storedRelativePath,
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim(),
            sizeBytes,
            createdUtc,
            sha256Hex,
            isLegacyBackfill);
    }

    public static async Task<StoredDocument?> FindStoredDocumentAsync(
        PlatformDbContext dbContext,
        string documentAreaCode,
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var normalizedAreaCode = documentAreaCode.Trim().ToUpperInvariant();
        var normalizedEntityType = entityType.Trim().ToUpperInvariant();

        return await dbContext.StoredDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.DocumentAreaCode == normalizedAreaCode
                        && item.EntityType == normalizedEntityType
                        && item.EntityId == entityId,
                cancellationToken);
    }
}
