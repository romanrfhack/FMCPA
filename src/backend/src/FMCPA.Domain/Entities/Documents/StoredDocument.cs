namespace FMCPA.Domain.Entities.Documents;

public sealed class StoredDocument
{
    public const string ActiveStatusCode = "ACTIVE";
    public const string ArchivedStatusCode = "ARCHIVED";

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
        bool isLegacyBackfill,
        string? documentClassCode = null,
        string? businessPurpose = null,
        bool isPrimaryDocument = false,
        string? classificationNotes = null,
        string? retentionPolicyCode = null,
        DateTimeOffset? retentionUntilUtc = null,
        string? replacementGroupKey = null)
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
        StatusCode = ActiveStatusCode;
        DocumentClassCode = DocumentClassCodes.NormalizeOrDefault(documentClassCode);
        BusinessPurpose = NormalizeOptional(businessPurpose, maxLength: 500);
        IsPrimaryDocument = isPrimaryDocument;
        ClassificationNotes = NormalizeOptional(classificationNotes, maxLength: 500);
        RetentionPolicyCode = DocumentRetentionPolicyCodes.NormalizeOrDefaultPolicy(
            retentionPolicyCode ?? DocumentRetentionPolicyCodes.ResolvePolicyForDocumentClass(DocumentClassCode));
        RetentionUntilUtc = retentionUntilUtc
                            ?? DocumentRetentionPolicyCodes.CalculateRetentionUntilUtc(CreatedUtc, RetentionPolicyCode);
        RetentionReviewStatusCode = DocumentRetentionReviewStatusCodes.Pending;
        ReplacementGroupKey = NormalizeOptional(replacementGroupKey, maxLength: 220)
                              ?? BuildReplacementGroupKey(DocumentAreaCode, EntityType, EntityId);
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

    public string DocumentClassCode { get; private set; } = DocumentClassCodes.Other;

    public string? BusinessPurpose { get; private set; }

    public bool IsPrimaryDocument { get; private set; }

    public string? ClassificationNotes { get; private set; }

    public string RetentionPolicyCode { get; private set; } = DocumentRetentionPolicyCodes.GenericReview;

    public DateTimeOffset RetentionUntilUtc { get; private set; }

    public string? RetentionOverridePolicyCode { get; private set; }

    public DateTimeOffset? RetentionOverrideUntilUtc { get; private set; }

    public string? RetentionOverrideReason { get; private set; }

    public bool HasRetentionOverride => RetentionOverridePolicyCode is not null || RetentionOverrideUntilUtc is not null;

    public string RetentionReviewStatusCode { get; private set; } = DocumentRetentionReviewStatusCodes.Pending;

    public DateTimeOffset? LastRetentionReviewUtc { get; private set; }

    public DateTimeOffset? NextRetentionReviewUtc { get; private set; }

    public string? RetentionReviewNotes { get; private set; }

    public bool IsAdministrativeHold { get; private set; }

    public string? HoldReason { get; private set; }

    public DateTimeOffset? HoldPlacedUtc { get; private set; }

    public DateTimeOffset? HoldReleasedUtc { get; private set; }

    public string? HoldPlacedBy { get; private set; }

    public string StatusCode { get; private set; } = ActiveStatusCode;

    public DateTimeOffset? ArchivedUtc { get; private set; }

    public string? ArchiveReason { get; private set; }

    public bool IsArchived => string.Equals(StatusCode, ArchivedStatusCode, StringComparison.Ordinal);

    public Guid? ReplacedDocumentId { get; private set; }

    public Guid? SupersededByDocumentId { get; private set; }

    public string ReplacementGroupKey { get; private set; } = string.Empty;

    public bool IsSuperseded => SupersededByDocumentId is not null;

    public bool Archive(string? reason, DateTimeOffset archivedUtc)
    {
        if (IsArchived)
        {
            return false;
        }

        StatusCode = ArchivedStatusCode;
        ArchivedUtc = archivedUtc;
        ArchiveReason = NormalizeOptional(reason, maxLength: 500);
        return true;
    }

    public bool Restore()
    {
        if (!IsArchived)
        {
            return false;
        }

        StatusCode = ActiveStatusCode;
        ArchivedUtc = null;
        ArchiveReason = null;
        return true;
    }

    public void MarkReplaces(Guid replacedDocumentId)
    {
        if (replacedDocumentId == Guid.Empty)
        {
            throw new ArgumentException("The replaced document identifier is required.", nameof(replacedDocumentId));
        }

        if (replacedDocumentId == Id)
        {
            throw new ArgumentException("A document cannot replace itself.", nameof(replacedDocumentId));
        }

        ReplacedDocumentId = replacedDocumentId;
    }

    public bool MarkSupersededBy(Guid supersedingDocumentId, DateTimeOffset supersededUtc, string? reason)
    {
        if (supersedingDocumentId == Guid.Empty)
        {
            throw new ArgumentException("The superseding document identifier is required.", nameof(supersedingDocumentId));
        }

        if (supersedingDocumentId == Id)
        {
            throw new ArgumentException("A document cannot supersede itself.", nameof(supersedingDocumentId));
        }

        SupersededByDocumentId = supersedingDocumentId;
        IsPrimaryDocument = false;

        if (!IsArchived)
        {
            Archive(reason, supersededUtc);
        }
        else if (string.IsNullOrWhiteSpace(ArchiveReason))
        {
            ArchiveReason = NormalizeOptional(reason, maxLength: 500);
        }

        return true;
    }

    public void ReplaceBinaryMetadata(
        string originalFileName,
        string storedRelativePath,
        string contentType,
        long sizeBytes,
        DateTimeOffset createdUtc,
        string? sha256Hex,
        string? documentClassCode = null,
        string? businessPurpose = null,
        bool isPrimaryDocument = false,
        string? classificationNotes = null)
    {
        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "The document size is required.");
        }

        OriginalFileName = NormalizeRequired(originalFileName, nameof(originalFileName));
        StoredRelativePath = NormalizeRelativePath(storedRelativePath, nameof(storedRelativePath));
        ContentType = NormalizeRequired(contentType, nameof(contentType));
        SizeBytes = sizeBytes;
        CreatedUtc = createdUtc;
        Sha256Hex = NormalizeOptionalHex(sha256Hex);
        IsLegacyBackfill = false;
        StatusCode = ActiveStatusCode;
        ArchivedUtc = null;
        ArchiveReason = null;
        DocumentClassCode = DocumentClassCodes.NormalizeOrDefault(documentClassCode);
        BusinessPurpose = NormalizeOptional(businessPurpose, maxLength: 500);
        IsPrimaryDocument = isPrimaryDocument;
        ClassificationNotes = NormalizeOptional(classificationNotes, maxLength: 500);
        RetentionPolicyCode = DocumentRetentionPolicyCodes.ResolvePolicyForDocumentClass(DocumentClassCode);
        RetentionUntilUtc = DocumentRetentionPolicyCodes.CalculateRetentionUntilUtc(CreatedUtc, RetentionPolicyCode);
        RetentionOverridePolicyCode = null;
        RetentionOverrideUntilUtc = null;
        RetentionOverrideReason = null;
        RetentionReviewStatusCode = DocumentRetentionReviewStatusCodes.Pending;
        LastRetentionReviewUtc = null;
        NextRetentionReviewUtc = null;
        RetentionReviewNotes = null;
        ReplacedDocumentId = null;
        SupersededByDocumentId = null;
        ReplacementGroupKey = BuildReplacementGroupKey(DocumentAreaCode, EntityType, EntityId);
    }

    public void UpdateMetadata(
        string documentClassCode,
        string? businessPurpose,
        bool isPrimaryDocument,
        string? classificationNotes)
    {
        var previousDocumentClassCode = DocumentClassCode;
        DocumentClassCode = DocumentClassCodes.NormalizeOrDefault(documentClassCode);
        BusinessPurpose = NormalizeOptional(businessPurpose, maxLength: 500);
        IsPrimaryDocument = isPrimaryDocument;
        ClassificationNotes = NormalizeOptional(classificationNotes, maxLength: 500);

        if (!string.Equals(previousDocumentClassCode, DocumentClassCode, StringComparison.Ordinal))
        {
            RetentionPolicyCode = DocumentRetentionPolicyCodes.ResolvePolicyForDocumentClass(DocumentClassCode);
            RetentionUntilUtc = DocumentRetentionPolicyCodes.CalculateRetentionUntilUtc(CreatedUtc, RetentionPolicyCode);
        }
    }

    public string ResolveRetentionStatusCode(DateTimeOffset referenceUtc)
    {
        return DocumentRetentionPolicyCodes.ResolveRetentionStatusCode(ResolveEffectiveRetentionUntilUtc(), referenceUtc);
    }

    public string ResolveEffectiveRetentionPolicyCode()
    {
        return RetentionOverridePolicyCode ?? RetentionPolicyCode;
    }

    public DateTimeOffset ResolveEffectiveRetentionUntilUtc()
    {
        return RetentionOverrideUntilUtc ?? RetentionUntilUtc;
    }

    public void SetRetentionOverride(string? retentionOverridePolicyCode, DateTimeOffset? retentionOverrideUntilUtc, string reason)
    {
        var normalizedPolicy = string.IsNullOrWhiteSpace(retentionOverridePolicyCode)
            ? null
            : DocumentRetentionPolicyCodes.NormalizeOrDefaultPolicy(retentionOverridePolicyCode);
        var effectivePolicyCode = normalizedPolicy ?? RetentionPolicyCode;

        RetentionOverridePolicyCode = normalizedPolicy;
        RetentionOverrideUntilUtc = retentionOverrideUntilUtc
                                    ?? DocumentRetentionPolicyCodes.CalculateRetentionUntilUtc(CreatedUtc, effectivePolicyCode);
        RetentionOverrideReason = NormalizeOptional(reason, maxLength: 500);
        ResetRetentionReview();
    }

    public bool ClearRetentionOverride()
    {
        var hadOverride = HasRetentionOverride;
        RetentionOverridePolicyCode = null;
        RetentionOverrideUntilUtc = null;
        RetentionOverrideReason = null;
        ResetRetentionReview();
        return hadOverride;
    }

    public void SetAdministrativeHold(string reason, DateTimeOffset placedUtc, string? placedBy)
    {
        IsAdministrativeHold = true;
        HoldReason = NormalizeOptional(reason, maxLength: 500);
        HoldPlacedUtc = placedUtc;
        HoldReleasedUtc = null;
        HoldPlacedBy = NormalizeOptional(placedBy, maxLength: 256);
    }

    public bool ClearAdministrativeHold(DateTimeOffset releasedUtc)
    {
        if (!IsAdministrativeHold)
        {
            return false;
        }

        IsAdministrativeHold = false;
        HoldReason = null;
        HoldPlacedUtc = null;
        HoldPlacedBy = null;
        HoldReleasedUtc = releasedUtc;
        return true;
    }

    public void MarkRetentionReviewed(string? notes, DateTimeOffset reviewedUtc)
    {
        RetentionReviewStatusCode = DocumentRetentionReviewStatusCodes.Completed;
        LastRetentionReviewUtc = reviewedUtc;
        NextRetentionReviewUtc = null;
        RetentionReviewNotes = NormalizeOptional(notes, maxLength: 500);
    }

    private void ResetRetentionReview()
    {
        RetentionReviewStatusCode = DocumentRetentionReviewStatusCodes.Pending;
        LastRetentionReviewUtc = null;
        NextRetentionReviewUtc = null;
        RetentionReviewNotes = null;
    }

    public void DeferRetentionReview(DateTimeOffset nextReviewUtc, string? notes, DateTimeOffset reviewedUtc)
    {
        RetentionReviewStatusCode = DocumentRetentionReviewStatusCodes.Deferred;
        LastRetentionReviewUtc = reviewedUtc;
        NextRetentionReviewUtc = nextReviewUtc;
        RetentionReviewNotes = NormalizeOptional(notes, maxLength: 500);
    }

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

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength].Trim();
    }

    private static string BuildReplacementGroupKey(string documentAreaCode, string entityType, Guid entityId)
    {
        return $"{documentAreaCode.Trim().ToUpperInvariant()}:{entityType.Trim().ToUpperInvariant()}:{entityId:N}";
    }
}
