namespace FMCPA.Domain.Entities.Documents;

public sealed class StoredDocument
{
    private StoredDocument()
    {
    }

    public StoredDocument(
        string moduleCode,
        string documentAreaCode,
        string entityType,
        Guid entityId,
        string originalFileName,
        string storedRelativePath,
        string contentType,
        long sizeBytes,
        DateTimeOffset createdUtc,
        string? sha256Hex,
        bool isLegacyBackfill)
    {
        if (entityId == Guid.Empty)
        {
            throw new ArgumentException("The document entity identifier is required.", nameof(entityId));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "The document size is required.");
        }

        Id = Guid.NewGuid();
        ModuleCode = NormalizeCode(moduleCode, nameof(moduleCode));
        DocumentAreaCode = NormalizeCode(documentAreaCode, nameof(documentAreaCode));
        EntityType = NormalizeCode(entityType, nameof(entityType));
        EntityId = entityId;
        OriginalFileName = NormalizeRequired(originalFileName, nameof(originalFileName));
        StoredRelativePath = NormalizeRelativePath(storedRelativePath, nameof(storedRelativePath));
        ContentType = NormalizeRequired(contentType, nameof(contentType));
        SizeBytes = sizeBytes;
        CreatedUtc = createdUtc;
        Sha256Hex = NormalizeOptionalHex(sha256Hex);
        IsLegacyBackfill = isLegacyBackfill;
    }

    public Guid Id { get; private set; }

    public string ModuleCode { get; private set; } = string.Empty;

    public string DocumentAreaCode { get; private set; } = string.Empty;

    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    public string OriginalFileName { get; private set; } = string.Empty;

    public string StoredRelativePath { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public string? Sha256Hex { get; private set; }

    public bool IsLegacyBackfill { get; private set; }

    private static string NormalizeCode(string value, string paramName)
    {
        return NormalizeRequired(value, paramName).ToUpperInvariant();
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required stored document value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string NormalizeRelativePath(string value, string paramName)
    {
        return NormalizeRequired(value, paramName)
            .Replace('\\', '/');
    }

    private static string? NormalizeOptionalHex(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }
}
