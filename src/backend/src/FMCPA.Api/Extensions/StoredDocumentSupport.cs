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
        bool isLegacyBackfill = false,
        string? documentClassCode = null,
        string? businessPurpose = null,
        bool? isPrimaryDocument = null,
        string? classificationNotes = null)
    {
        var classification = ResolveDefaultClassification(documentAreaCode, contentType);

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
            isLegacyBackfill,
            documentClassCode ?? classification.DocumentClassCode,
            businessPurpose ?? classification.BusinessPurpose,
            isPrimaryDocument ?? classification.IsPrimaryDocument,
            classificationNotes);
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
            .Where(item => item.DocumentAreaCode == normalizedAreaCode
                           && item.EntityType == normalizedEntityType
                           && item.EntityId == entityId)
            .OrderBy(item => item.StatusCode == StoredDocument.ActiveStatusCode ? 0 : 1)
            .ThenBy(item => item.SupersededByDocumentId == null ? 0 : 1)
            .ThenByDescending(item => item.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static StoredDocumentClassification ResolveDefaultClassification(
        string documentAreaCode,
        string? contentType)
    {
        var normalizedAreaCode = documentAreaCode.Trim().ToUpperInvariant();
        return normalizedAreaCode switch
        {
            DocumentAreaCodes.MarketsTenantCertificates => new(
                DocumentClassCodes.Certificate,
                "Acreditar la cedula digitalizada del locatario.",
                IsPrimaryDocument: true),
            DocumentAreaCodes.DonationsApplicationEvidences => new(
                ResolveEvidenceClassCode(contentType),
                "Soportar la aplicacion documental de una donacion.",
                IsPrimaryDocument: false),
            DocumentAreaCodes.FederationApplicationEvidences => new(
                ResolveEvidenceClassCode(contentType),
                "Soportar la aplicacion documental de Federacion.",
                IsPrimaryDocument: false),
            _ => new(
                DocumentClassCodes.Other,
                BusinessPurpose: null,
                IsPrimaryDocument: false)
        };
    }

    private static string ResolveEvidenceClassCode(string? contentType)
    {
        var normalizedContentType = NormalizeContentType(contentType);
        return string.Equals(normalizedContentType, "image/jpeg", StringComparison.Ordinal)
               || string.Equals(normalizedContentType, "image/png", StringComparison.Ordinal)
            ? DocumentClassCodes.PhotoEvidence
            : DocumentClassCodes.SupportingDocument;
    }

    private static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return string.Empty;
        }

        var normalized = contentType.Trim();
        var separatorIndex = normalized.IndexOf(';', StringComparison.Ordinal);
        return (separatorIndex >= 0 ? normalized[..separatorIndex] : normalized).Trim().ToLowerInvariant();
    }

    private sealed record StoredDocumentClassification(
        string DocumentClassCode,
        string? BusinessPurpose,
        bool IsPrimaryDocument);
}
