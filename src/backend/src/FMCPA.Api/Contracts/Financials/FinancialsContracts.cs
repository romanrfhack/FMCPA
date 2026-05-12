namespace FMCPA.Api.Contracts.Financials;

public sealed record FinancialPermitSummaryResponse(
    Guid Id,
    string FinancialName,
    string InstitutionOrDependency,
    string PlaceOrStand,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string Schedule,
    string NegotiatedTerms,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    bool StatusAlertsEnabledByDefault,
    int DaysUntilExpiration,
    string AlertState,
    Guid? RenewedFromPermitId,
    Guid CurrentRootPermitId,
    bool IsCurrentVersion,
    int RenewalSequence,
    int CreditCount,
    int CommissionCount,
    string? Notes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc);

public sealed record FinancialPermitDetailResponse(
    Guid Id,
    string FinancialName,
    string InstitutionOrDependency,
    string PlaceOrStand,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string Schedule,
    string NegotiatedTerms,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    bool StatusAlertsEnabledByDefault,
    int DaysUntilExpiration,
    string AlertState,
    Guid? RenewedFromPermitId,
    Guid CurrentRootPermitId,
    bool IsCurrentVersion,
    int RenewalSequence,
    string? Notes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc,
    IReadOnlyList<FinancialPermitRenewalHistoryResponse> RenewalHistory,
    IReadOnlyList<FinancialCreditResponse> Credits);

public sealed record FinancialPermitRenewalHistoryResponse(
    Guid Id,
    Guid? RenewedFromPermitId,
    Guid CurrentRootPermitId,
    bool IsCurrentVersion,
    int RenewalSequence,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string PlaceOrStand,
    string Schedule,
    string StatusCode,
    string StatusName,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc);

public sealed record FinancialPermitRenewalChainResponse(
    Guid CurrentRootPermitId,
    Guid CurrentPermitId,
    FinancialPermitRenewalChainPermitResponse CurrentPermit,
    IReadOnlyList<FinancialPermitRenewalChainPermitResponse> Permits,
    FinancialPermitRenewalChainSummaryResponse Summary,
    IReadOnlyList<FinancialCreditResponse> Credits);

public sealed record FinancialPermitRenewalChainPermitResponse(
    Guid Id,
    Guid? RenewedFromPermitId,
    Guid CurrentRootPermitId,
    bool IsCurrentVersion,
    int RenewalSequence,
    string FinancialName,
    string InstitutionOrDependency,
    string PlaceOrStand,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string Schedule,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    int DaysUntilExpiration,
    string AlertState,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc);

public sealed record FinancialPermitRenewalChainSummaryResponse(
    int PermitsCount,
    int TotalCreditsCount,
    decimal TotalCreditsAmount,
    int TotalCommissionsCount,
    decimal TotalCommissionsAmount,
    decimal TotalPromoterCommission,
    decimal TotalAdminCommission,
    decimal TotalThirdPartyCommission,
    DateOnly? OperationFrom,
    DateOnly? OperationTo);

public sealed record FinancialPermitCurrentResolutionResponse(
    Guid PermitId,
    Guid CurrentRootPermitId,
    int RenewalSequence,
    string FinancialName,
    string InstitutionOrDependency,
    string PlaceOrStand,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string Schedule,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    int DaysUntilExpiration,
    string AlertState,
    string Summary);

public sealed record FinancialPermitContextResolutionResponse(
    string FinancialName,
    string InstitutionOrDependency,
    string PlaceOrStand,
    FinancialPermitContextPermitResponse? CurrentPermit,
    Guid? CurrentRootPermitId,
    FinancialPermitContextPermitResponse? LastKnownPermit,
    string SuggestionCode,
    string SuggestionMessage,
    string RouteHint);

public sealed record FinancialContextCardResponse(
    FinancialPermitContextResolutionResponse Resolution,
    FinancialContextCardRenewalChainSummaryResponse? RenewalChainSummary,
    FinancialContextCardCreditSummaryResponse? CreditSummary,
    FinancialContextCardCommissionSummaryResponse? CommissionSummary,
    IReadOnlyList<string> AvailableActions);

public sealed record FinancialContextCardRenewalChainSummaryResponse(
    Guid CurrentPermitId,
    string CurrentPermitSummary,
    int TotalPermitsCount,
    int CurrentRenewalSequence,
    DateOnly? PeriodFrom,
    DateOnly? PeriodTo);

public sealed record FinancialContextCardCreditSummaryResponse(
    int TotalCreditsCount,
    decimal TotalCreditsAmount);

public sealed record FinancialContextCardCommissionSummaryResponse(
    int TotalCommissionsCount,
    decimal TotalCommissionsAmount,
    decimal TotalPromoterCommission,
    decimal TotalAdminCommission,
    decimal TotalThirdPartyCommission);

public sealed record FinancialPermitContextPermitResponse(
    Guid PermitId,
    Guid CurrentRootPermitId,
    Guid? RenewedFromPermitId,
    bool IsCurrentVersion,
    int RenewalSequence,
    string FinancialName,
    string InstitutionOrDependency,
    string PlaceOrStand,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string Schedule,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    int DaysUntilExpiration,
    string AlertState,
    string Summary);

public sealed record FinancialPermitRenewalDraftResponse(
    Guid SourcePermitId,
    Guid RenewalTargetPermitId,
    Guid CurrentRootPermitId,
    bool SourceIsCurrentVersion,
    int SourceRenewalSequence,
    int RenewalTargetSequence,
    string FinancialName,
    string InstitutionOrDependency,
    string PlaceOrStand,
    string Schedule,
    string NegotiatedTerms,
    string? Notes,
    DateOnly PreviousValidFrom,
    DateOnly PreviousValidTo,
    DateOnly NewStartDate,
    DateOnly NewEndDate,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    string SuggestionCode,
    string SuggestionMessage,
    IReadOnlyList<string> FieldsToConfirm);

public sealed record FinancialPermitCurrentResolutionAmbiguousResponse(
    string Message,
    string ReasonCode,
    string FinancialName,
    string InstitutionOrDependency,
    string PlaceOrStand,
    IReadOnlyList<FinancialPermitCurrentResolutionResponse> Matches);

public sealed record CreateFinancialPermitRequest(
    string FinancialName,
    string InstitutionOrDependency,
    string PlaceOrStand,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string Schedule,
    string NegotiatedTerms,
    int StatusCatalogEntryId,
    string? Notes);

public sealed record RenewFinancialPermitRequest(
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string? PlaceOrStand,
    string? Schedule,
    string? NegotiatedTerms,
    string? Notes);

public sealed record FinancialPermitAlertResponse(
    Guid PermitId,
    string FinancialName,
    string InstitutionOrDependency,
    string PlaceOrStand,
    string StatusCode,
    string StatusName,
    DateOnly ValidTo,
    int DaysUntilExpiration,
    string AlertState);

public sealed record CreateFinancialCreditRequest(
    Guid? PromoterContactId,
    string PromoterName,
    Guid? BeneficiaryContactId,
    string BeneficiaryName,
    string? PhoneNumber,
    string? WhatsAppPhone,
    DateOnly AuthorizationDate,
    decimal Amount,
    string? Notes);

public sealed record FinancialCreditResponse(
    Guid Id,
    Guid FinancialPermitId,
    Guid? PromoterContactId,
    string PromoterName,
    Guid? BeneficiaryContactId,
    string BeneficiaryName,
    string? PhoneNumber,
    string? WhatsAppPhone,
    DateOnly AuthorizationDate,
    decimal Amount,
    string? Notes,
    int CommissionCount,
    IReadOnlyList<FinancialCreditCommissionResponse> Commissions,
    DateTimeOffset CreatedUtc);

public sealed record CreateFinancialCreditCommissionRequest(
    int CommissionTypeId,
    string RecipientCategory,
    Guid? RecipientContactId,
    string RecipientName,
    decimal BaseAmount,
    decimal CommissionAmount,
    string? Notes);

public sealed record FinancialCreditCommissionResponse(
    Guid Id,
    Guid FinancialCreditId,
    int CommissionTypeId,
    string CommissionTypeCode,
    string CommissionTypeName,
    string RecipientCategory,
    Guid? RecipientContactId,
    string RecipientName,
    decimal BaseAmount,
    decimal CommissionAmount,
    string? Notes,
    DateTimeOffset CreatedUtc);

public sealed record FinancialPermitOperationBlockedResponse(
    string Message,
    string ReasonCode,
    Guid PermitId,
    Guid CurrentRootPermitId,
    Guid? CurrentPermitId);

public sealed record FinancialPermitActiveConflictResponse(
    string Message,
    string ReasonCode,
    Guid ConflictingPermitId,
    Guid CurrentRootPermitId,
    string FinancialName,
    string InstitutionOrDependency,
    string PlaceOrStand,
    DateOnly ConflictingValidFrom,
    DateOnly ConflictingValidTo);
