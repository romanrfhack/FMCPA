namespace FMCPA.Api.Contracts.Closeout;

public sealed record DashboardSummaryResponse(
    DashboardMarketsSummaryResponse Markets,
    DashboardDonationsSummaryResponse Donations,
    DashboardFinancialsSummaryResponse Financials,
    DashboardFederationSummaryResponse Federation,
    DashboardTotalsSummaryResponse Totals);

public sealed record DashboardMarketsSummaryResponse(
    int TotalMarkets,
    int ActiveMarkets,
    int ClosedOrArchivedMarkets,
    int TenantCount,
    int ActiveIssueCount,
    int CertificateAlertCount);

public sealed record DashboardDonationsSummaryResponse(
    int TotalDonations,
    int NotAppliedCount,
    int PartiallyAppliedCount,
    int ClosedDonationsCount,
    decimal BaseAmountTotal,
    decimal AppliedAmountTotal,
    int ActiveAlertCount);

public sealed record DashboardFinancialsSummaryResponse(
    int TotalPermits,
    int ClosedPermitsCount,
    int CreditCount,
    int CommissionCount,
    int DueSoonCount,
    int ExpiredCount,
    int RenewalCount,
    int ActiveAlertCount);

public sealed record DashboardFederationSummaryResponse(
    int TotalActions,
    int ClosedActionsCount,
    int ActionAlertCount,
    int TotalDonations,
    int ClosedDonationsCount,
    int DonationAlertCount,
    int CommissionCount,
    int EvidenceCount);

public sealed record DashboardTotalsSummaryResponse(
    int ActiveAlertCount,
    int ClosedRecordsCount,
    int CommissionCount,
    int EvidenceCount);

public sealed record DashboardAlertItemResponse(
    string AlertKey,
    string ModuleCode,
    string ModuleName,
    string Category,
    string Title,
    string Subtitle,
    string Detail,
    string AlertState,
    string? Reference,
    DateOnly? RelevantDate,
    int? DaysUntilTarget,
    string NavigationPath);

public sealed record DashboardAlertsResponse(
    IReadOnlyList<DashboardAlertItemResponse> MarketCertificates,
    IReadOnlyList<DashboardAlertItemResponse> Donations,
    IReadOnlyList<DashboardAlertItemResponse> FinancialPermits,
    IReadOnlyList<DashboardAlertItemResponse> FederationActions,
    IReadOnlyList<DashboardAlertItemResponse> FederationDonations);

public sealed record ConsolidatedCommissionListResponse(
    IReadOnlyList<ConsolidatedCommissionItemResponse> Items,
    int TotalCount,
    decimal TotalBaseAmount,
    decimal TotalCommissionAmount);

public sealed record ConsolidatedCommissionItemResponse(
    Guid CommissionId,
    string SourceModuleCode,
    string SourceModuleName,
    string OriginEntityType,
    Guid OriginEntityId,
    DateOnly OperationDate,
    int CommissionTypeId,
    string CommissionTypeCode,
    string CommissionTypeName,
    string RecipientCategory,
    string RecipientName,
    decimal BaseAmount,
    decimal CommissionAmount,
    string OriginReference,
    string OriginPrimaryName,
    string OriginSecondaryName,
    string? Notes,
    DateTimeOffset CreatedUtc,
    string NavigationPath);

public sealed record BitacoraEntryResponse(
    string EventKey,
    DateTimeOffset OccurredUtc,
    string ModuleCode,
    string ModuleName,
    string EntityType,
    string EntityId,
    string ActionType,
    string Title,
    string Detail,
    string? RelatedStatusCode,
    bool IsCloseEvent,
    string? CloseEventSource,
    string? Reference,
    string NavigationPath,
    string? MetadataJson);

public sealed record ClosedItemResponse(
    string RecordKey,
    string ModuleCode,
    string ModuleName,
    string ItemType,
    string ItemId,
    string Title,
    string Subtitle,
    string? Reference,
    string StatusCode,
    string StatusName,
    DateTimeOffset HistoricalTimestampUtc,
    string HistoricalTimestampSource,
    bool HasFormalCloseEvent,
    string NavigationPath);

public sealed record LegacyCloseNormalizationResponse(
    bool DryRun,
    int ScannedClosedCount,
    int EligibleCount,
    int NormalizedCount,
    int SkippedCount,
    IReadOnlyList<LegacyCloseNormalizationItemResponse> Items);

public sealed record LegacyCloseNormalizationItemResponse(
    string RecordKey,
    string ModuleCode,
    string ModuleName,
    string ItemType,
    string ItemId,
    string Title,
    string? Reference,
    string StatusCode,
    string StatusName,
    DateTimeOffset HistoricalTimestampUtc,
    string HistoricalTimestampSource,
    string Outcome,
    string NavigationPath,
    Guid? CreatedEventId);

public sealed record CloseRecordRequest(
    string? Reason);

public sealed record CloseRecordResponse(
    Guid EventId,
    string ModuleCode,
    string ModuleName,
    string EntityType,
    string EntityId,
    string StatusCode,
    string StatusName,
    DateTimeOffset ClosedUtc,
    string? Reason);

public sealed record DocumentIntegritySummaryResponse(
    int TotalDocumentRecords,
    int ValidCount,
    int MissingFileCount,
    int SizeMismatchCount,
    int InvalidPathCount,
    int MissingDocumentRecordCount,
    int OrphanedDocumentRecordCount,
    int MetadataMismatchCount);

public sealed record DocumentIntegrityIssueResponse(
    string IssueKey,
    string ModuleCode,
    string ModuleName,
    string DocumentAreaCode,
    string EntityType,
    string EntityId,
    string IntegrityState,
    string Title,
    string Detail,
    string? OriginalFileName,
    string? StoredRelativePath,
    string NavigationPath);

public sealed record DocumentRecordResponse(
    string DocumentKey,
    string ModuleCode,
    string ModuleName,
    string DocumentAreaCode,
    string EntityType,
    string EntityId,
    string OriginalFileName,
    string StoredRelativePath,
    string ContentType,
    long SizeBytes,
    long? ActualSizeBytes,
    string IntegrityState,
    bool HasDocumentRecord,
    bool IsLegacyBackfill,
    DateTimeOffset CreatedUtc,
    string? Sha256Hex,
    string NavigationPath);

public sealed record DocumentIntegrityResponse(
    DocumentIntegritySummaryResponse Summary,
    IReadOnlyList<DocumentIntegrityIssueResponse> Issues,
    IReadOnlyList<DocumentRecordResponse> Records);
