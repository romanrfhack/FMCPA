namespace FMCPA.Api.Contracts.Documents;

public sealed record DocumentCatalogListResponse(
    int TotalCount,
    int ReturnedCount,
    int Skip,
    int Take,
    IReadOnlyList<DocumentCatalogItemResponse> Items);

public sealed record DocumentCatalogItemResponse(
    Guid Id,
    string ModuleCode,
    string ModuleName,
    string DocumentAreaCode,
    string EntityType,
    Guid EntityId,
    DocumentOriginContextResponse OriginContext,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedUtc,
    string DocumentClassCode,
    string? BusinessPurpose,
    bool IsPrimaryDocument,
    string? ClassificationNotes,
    string RetentionPolicyCode,
    DateTimeOffset RetentionUntilUtc,
    string RetentionStatusCode,
    string RetentionBaselinePolicyCode,
    DateTimeOffset RetentionBaselineUntilUtc,
    string RetentionEffectivePolicyCode,
    DateTimeOffset RetentionEffectiveUntilUtc,
    bool HasRetentionOverride,
    string? RetentionOverridePolicyCode,
    DateTimeOffset? RetentionOverrideUntilUtc,
    string? RetentionOverrideReason,
    string RetentionReviewStatusCode,
    DateTimeOffset? LastRetentionReviewUtc,
    DateTimeOffset? NextRetentionReviewUtc,
    string? RetentionReviewNotes,
    bool IsAdministrativeHold,
    string? HoldReason,
    DateTimeOffset? HoldPlacedUtc,
    DateTimeOffset? HoldReleasedUtc,
    string? HoldPlacedBy,
    string DocumentOperationalStatusCode,
    string DocumentOperationalSeverityCode,
    string StatusCode,
    DateTimeOffset? ArchivedUtc,
    string? ArchiveReason,
    bool IsSuperseded,
    Guid? SupersededByDocumentId,
    Guid? ReplacedDocumentId,
    string ReplacementGroupKey,
    string IntegrityState,
    long? ActualSizeBytes,
    bool HasChecksum,
    bool IsLegacyBackfill,
    string DetailUrl,
    string DownloadUrl);

public sealed record DocumentCatalogDetailResponse(
    Guid Id,
    string ModuleCode,
    string ModuleName,
    string DocumentAreaCode,
    string EntityType,
    Guid EntityId,
    DocumentOriginContextResponse OriginContext,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedUtc,
    string DocumentClassCode,
    string? BusinessPurpose,
    bool IsPrimaryDocument,
    string? ClassificationNotes,
    string RetentionPolicyCode,
    DateTimeOffset RetentionUntilUtc,
    string RetentionStatusCode,
    string RetentionBaselinePolicyCode,
    DateTimeOffset RetentionBaselineUntilUtc,
    string RetentionEffectivePolicyCode,
    DateTimeOffset RetentionEffectiveUntilUtc,
    bool HasRetentionOverride,
    string? RetentionOverridePolicyCode,
    DateTimeOffset? RetentionOverrideUntilUtc,
    string? RetentionOverrideReason,
    string RetentionReviewStatusCode,
    DateTimeOffset? LastRetentionReviewUtc,
    DateTimeOffset? NextRetentionReviewUtc,
    string? RetentionReviewNotes,
    bool IsAdministrativeHold,
    string? HoldReason,
    DateTimeOffset? HoldPlacedUtc,
    DateTimeOffset? HoldReleasedUtc,
    string? HoldPlacedBy,
    string DocumentOperationalStatusCode,
    string DocumentOperationalSeverityCode,
    string StatusCode,
    DateTimeOffset? ArchivedUtc,
    string? ArchiveReason,
    bool IsSuperseded,
    Guid? SupersededByDocumentId,
    Guid? ReplacedDocumentId,
    string ReplacementGroupKey,
    string IntegrityState,
    long? ActualSizeBytes,
    bool HasChecksum,
    string? Sha256Hex,
    bool IsLegacyBackfill,
    string DownloadUrl);

public sealed record DocumentOriginContextResponse(
    string ModuleCode,
    string ModuleName,
    string EntityType,
    Guid EntityId,
    string DisplayName,
    string? Summary,
    string? RouteHint);

public sealed record DocumentTimelineResponse(
    int TotalCount,
    IReadOnlyList<DocumentTimelineEventResponse> Items);

public sealed record DocumentTimelineEventResponse(
    Guid? AuditEventId,
    Guid DocumentId,
    string DocumentOriginalFileName,
    string ModuleCode,
    string ModuleName,
    string DocumentAreaCode,
    string EntityType,
    Guid EntityId,
    DateTimeOffset OccurredUtc,
    string EventType,
    string Title,
    string Detail,
    Guid? RelatedDocumentId,
    string? RelatedDocumentOriginalFileName,
    string DocumentStatusCode,
    bool IsCurrentDocument,
    bool IsSuperseded);

public sealed record DocumentWorkQueueResponse(
    int TotalCount,
    int ReturnedCount,
    int Skip,
    int Take,
    IReadOnlyList<DocumentWorkQueueItemResponse> Items);

public sealed record DocumentWorkQueueItemResponse(
    string WorkItemKey,
    string WorkItemType,
    string SeverityCode,
    string ModuleCode,
    string ModuleName,
    string EntityType,
    Guid EntityId,
    DocumentOriginContextResponse OriginContext,
    Guid? DocumentId,
    string? DocumentOriginalFileName,
    string Title,
    string Summary,
    string ReasonCode,
    string CurrentStatusCode,
    string? DocumentOperationalStatusCode,
    string? DocumentOperationalSeverityCode,
    string RouteHint,
    string? DocumentDetailUrl,
    string? RemediationHint);

public sealed record DocumentSummaryResponse(
    int TotalDocuments,
    int ActiveDocuments,
    int ArchivedDocuments,
    int IntegrityIssuesCount,
    int IncompleteEntitiesCount,
    int ReviewDueCount,
    int ExpiredRetentionCount,
    int AdministrativeHoldCount,
    IReadOnlyList<DocumentSummaryModuleResponse> Modules,
    IReadOnlyList<DocumentSummaryClassResponse> DocumentClasses,
    IReadOnlyList<DocumentSummaryOperationalStatusResponse> OperationalStatuses,
    IReadOnlyList<DocumentSummaryWorkQueueCategoryResponse> WorkQueueCategories);

public sealed record DocumentSummaryModuleResponse(
    string ModuleCode,
    string ModuleName,
    int TotalDocuments,
    int ActiveDocuments,
    int ArchivedDocuments,
    int IntegrityIssuesCount,
    int IncompleteEntitiesCount,
    int ReviewDueCount,
    int ExpiredRetentionCount,
    int AdministrativeHoldCount);

public sealed record DocumentSummaryClassResponse(
    string DocumentClassCode,
    int TotalDocuments,
    int ActiveDocuments,
    int ArchivedDocuments);

public sealed record DocumentSummaryOperationalStatusResponse(
    string DocumentOperationalStatusCode,
    string DocumentOperationalSeverityCode,
    int TotalCount);

public sealed record DocumentSummaryWorkQueueCategoryResponse(
    string WorkItemType,
    string ReasonCode,
    string SeverityCode,
    int TotalCount);

public sealed record DocumentCompletenessListResponse(
    int TotalCount,
    int ReturnedCount,
    int Skip,
    int Take,
    IReadOnlyList<DocumentCompletenessResponse> Items);

public sealed record DocumentRuleListResponse(
    int TotalCount,
    IReadOnlyList<DocumentRuleResponse> Items);

public sealed record DocumentRuleResponse(
    string RuleCode,
    string ModuleCode,
    string ModuleName,
    string EntityType,
    string CoveredDocumentEntityType,
    string DocumentAreaCode,
    IReadOnlyList<string> RequiredDocumentClassCodes,
    int MinimumRequiredCount,
    string RequiredDocumentCode,
    string RequiredDocumentDescription,
    string MissingReasonCode,
    string MissingReasonDescription,
    string RemediationHint);

public sealed record DocumentRequirementResponse(
    string ModuleCode,
    string ModuleName,
    string EntityType,
    Guid EntityId,
    DocumentOriginContextResponse OriginContext,
    bool AppliesRule,
    string? RuleCode,
    string StatusCode,
    bool? IsComplete,
    string? RequiredDocumentCode,
    string? RequiredDocumentDescription,
    IReadOnlyList<string> RequiredDocumentClassCodes,
    int MinimumRequiredCount,
    int CurrentDocumentCount,
    string? MissingReasonCode,
    string? MissingReasonDescription,
    string? RemediationHint,
    IReadOnlyList<Guid> CoveringDocumentIds);

public sealed record DocumentCompletenessResponse(
    string ModuleCode,
    string ModuleName,
    string EntityType,
    Guid EntityId,
    DocumentOriginContextResponse OriginContext,
    string RuleCode,
    string StatusCode,
    bool IsComplete,
    string RequiredDocumentCode,
    string RequiredDocumentDescription,
    IReadOnlyList<string> RequiredDocumentClassCodes,
    int MinimumRequiredCount,
    string? MissingReasonCode,
    string? MissingReasonDescription,
    string RemediationHint,
    int RelatedDocumentCount,
    IReadOnlyList<Guid> CoveringDocumentIds);

public sealed record ArchiveStoredDocumentRequest(string? Reason);

public sealed record SetStoredDocumentHoldRequest(string? Reason);

public sealed record UpdateStoredDocumentMetadataRequest(
    string? DocumentClassCode,
    string? BusinessPurpose,
    bool IsPrimaryDocument,
    string? ClassificationNotes);

public sealed record UpdateStoredDocumentRetentionReviewRequest(
    string? RetentionReviewStatusCode,
    DateTimeOffset? NextRetentionReviewUtc,
    string? RetentionReviewNotes);

public sealed record UpdateStoredDocumentRetentionOverrideRequest(
    string? RetentionOverridePolicyCode,
    DateTimeOffset? RetentionOverrideUntilUtc,
    string? RetentionOverrideReason);
