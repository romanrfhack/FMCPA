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
    string? Notes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc,
    IReadOnlyList<FinancialCreditResponse> Credits);

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
