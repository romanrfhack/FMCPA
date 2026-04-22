using Microsoft.AspNetCore.Http;

namespace FMCPA.Api.Contracts.Markets;

public sealed record MarketSummaryResponse(
    Guid Id,
    string Name,
    string Borough,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    bool StatusAlertsEnabledByDefault,
    Guid? SecretaryGeneralContactId,
    string SecretaryGeneralName,
    string? Notes,
    int TenantCount,
    int IssueCount,
    int ActiveTenantAlertsCount,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc);

public sealed record MarketDetailResponse(
    Guid Id,
    string Name,
    string Borough,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    bool StatusAlertsEnabledByDefault,
    Guid? SecretaryGeneralContactId,
    string SecretaryGeneralName,
    string? Notes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc,
    IReadOnlyList<MarketTenantResponse> Tenants,
    IReadOnlyList<MarketIssueResponse> Issues);

public sealed record CreateMarketRequest(
    string Name,
    string Borough,
    int StatusCatalogEntryId,
    Guid? SecretaryGeneralContactId,
    string SecretaryGeneralName,
    string? Notes);

public sealed class CreateMarketTenantRequest
{
    public Guid? ContactId { get; init; }

    public string TenantName { get; init; } = string.Empty;

    public string CertificateNumber { get; init; } = string.Empty;

    public DateOnly CertificateValidityTo { get; init; }

    public string BusinessLine { get; init; } = string.Empty;

    public string? MobilePhone { get; init; }

    public string? WhatsAppPhone { get; init; }

    public string? Email { get; init; }

    public string? Notes { get; init; }

    public IFormFile? CertificateFile { get; init; }
}

public sealed record MarketTenantResponse(
    Guid Id,
    Guid MarketId,
    Guid? ContactId,
    string TenantName,
    string CertificateNumber,
    DateOnly CertificateValidityTo,
    string BusinessLine,
    string? MobilePhone,
    string? WhatsAppPhone,
    string? Email,
    string? Notes,
    bool HasDigitalCertificate,
    string? CertificateOriginalFileName,
    string? CertificateContentType,
    long? CertificateFileSizeBytes,
    DateTimeOffset? CertificateUploadedUtc,
    string CertificateAlertState,
    int DaysUntilExpiration,
    bool AlertsSuppressed,
    DateTimeOffset CreatedUtc);

public sealed record CreateMarketIssueRequest(
    string IssueType,
    string Description,
    DateOnly IssueDate,
    string AdvanceSummary,
    int StatusCatalogEntryId,
    string? FollowUpOrResolution,
    string? FinalSatisfaction);

public sealed record MarketIssueResponse(
    Guid Id,
    Guid MarketId,
    string IssueType,
    string Description,
    DateOnly IssueDate,
    string AdvanceSummary,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    string? FollowUpOrResolution,
    string? FinalSatisfaction,
    DateTimeOffset CreatedUtc);

public sealed record MarketTenantAlertResponse(
    Guid MarketId,
    string MarketName,
    string MarketStatusCode,
    Guid TenantId,
    string TenantName,
    string CertificateNumber,
    DateOnly CertificateValidityTo,
    int DaysUntilExpiration,
    string AlertState);
