using Microsoft.AspNetCore.Http;

namespace FMCPA.Api.Contracts.Donations;

public sealed record DonationSummaryResponse(
    Guid Id,
    string DonorEntityName,
    DateOnly DonationDate,
    string DonationType,
    decimal BaseAmount,
    string Reference,
    string? Notes,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    bool StatusAlertsEnabledByDefault,
    decimal AppliedAmountTotal,
    decimal RemainingAmount,
    decimal AppliedPercentage,
    int ApplicationCount,
    int EvidenceCount,
    string AlertState,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc);

public sealed record DonationDetailResponse(
    Guid Id,
    string DonorEntityName,
    DateOnly DonationDate,
    string DonationType,
    decimal BaseAmount,
    string Reference,
    string? Notes,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    bool StatusAlertsEnabledByDefault,
    decimal AppliedAmountTotal,
    decimal RemainingAmount,
    decimal AppliedPercentage,
    string AlertState,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc,
    IReadOnlyList<DonationApplicationResponse> Applications);

public sealed record CreateDonationRequest(
    string DonorEntityName,
    DateOnly DonationDate,
    string DonationType,
    decimal BaseAmount,
    string Reference,
    string? Notes,
    int StatusCatalogEntryId);

public sealed record DonationProgressResponse(
    Guid DonationId,
    decimal BaseAmount,
    decimal AppliedAmountTotal,
    decimal RemainingAmount,
    decimal AppliedPercentage,
    int ApplicationCount);

public sealed record DonationDocumentaryStatusResponse(
    Guid DonationId,
    int TotalApplications,
    int ApplicationsWithEvidence,
    int ApplicationsMissingEvidence,
    string DocumentaryStatusCode,
    string DocumentaryStatusLabel,
    bool IsMinimumEvidenceComplete,
    IReadOnlyList<DonationApplicationDocumentaryStatusResponse> ApplicationStatuses);

public sealed record DonationApplicationDocumentaryStatusResponse(
    Guid ApplicationId,
    string BeneficiaryName,
    DateOnly ApplicationDate,
    decimal AppliedAmount,
    int EvidenceCount,
    int ActiveDocumentCount,
    string RequirementStatus,
    string? MissingReasonCode,
    IReadOnlyList<string> RequiredDocumentClassCodes,
    string? RouteHint);

public sealed record DonationTransparencyReportResponse(
    Guid DonationId,
    string DonorEntityName,
    DateOnly DonationDate,
    string DonationType,
    string Reference,
    string? Notes,
    DateTimeOffset ReportGeneratedUtc,
    DonationTransparencyFinancialSummaryResponse FinancialSummary,
    DonationTransparencyOperationalStatusResponse OperationalStatus,
    DonationTransparencyDocumentarySummaryResponse DocumentarySummary,
    IReadOnlyList<DonationTransparencyApplicationResponse> Applications,
    DonationTransparencyReadinessResponse PresentationReadiness,
    IReadOnlyList<string> ScopeNotes);

public sealed record DonationTransparencyFinancialSummaryResponse(
    decimal BaseAmount,
    decimal AppliedAmountTotal,
    decimal RemainingAmount,
    decimal AppliedPercentage,
    int ApplicationCount);

public sealed record DonationTransparencyOperationalStatusResponse(
    string DonationStatusCode,
    string DonationStatusName,
    bool StatusIsClosed,
    string FinancialStatusLabel,
    string OperationalStatusLabel);

public sealed record DonationTransparencyDocumentarySummaryResponse(
    string DocumentaryStatusCode,
    string DocumentaryStatusLabel,
    int TotalApplications,
    int ApplicationsWithEvidence,
    int ApplicationsMissingEvidence,
    bool IsMinimumEvidenceComplete);

public sealed record DonationTransparencyApplicationResponse(
    Guid ApplicationId,
    string BeneficiaryName,
    DateOnly ApplicationDate,
    string ResponsibleName,
    decimal AppliedAmount,
    decimal PercentageOfDonation,
    string StatusCode,
    string StatusName,
    string? VerificationDetails,
    string? ClosingDetails,
    int EvidenceCount,
    int ActiveDocumentCount,
    string RequirementStatus,
    string? MissingReasonCode,
    IReadOnlyList<DonationTransparencyEvidenceResponse> Evidences);

public sealed record DonationTransparencyEvidenceResponse(
    Guid EvidenceId,
    string EvidenceTypeName,
    string OriginalFileName,
    string? Description,
    DateTimeOffset UploadedUtc,
    string DownloadUrl);

public sealed record DonationTransparencyReadinessResponse(
    string ReadinessCode,
    string ReadinessLabel,
    IReadOnlyList<string> Reasons);

public sealed record CreateDonationApplicationRequest(
    string BeneficiaryName,
    Guid? ResponsibleContactId,
    string ResponsibleName,
    DateOnly ApplicationDate,
    decimal AppliedAmount,
    int StatusCatalogEntryId,
    string? VerificationDetails,
    string? ClosingDetails);

public sealed record DonationApplicationResponse(
    Guid Id,
    Guid DonationId,
    string BeneficiaryName,
    Guid? ResponsibleContactId,
    string ResponsibleName,
    DateOnly ApplicationDate,
    decimal AppliedAmount,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    string? VerificationDetails,
    string? ClosingDetails,
    int EvidenceCount,
    IReadOnlyList<DonationApplicationEvidenceResponse> Evidences,
    DateTimeOffset CreatedUtc);

public sealed class CreateDonationApplicationEvidenceRequest
{
    public int EvidenceTypeId { get; init; }

    public string? Description { get; init; }

    public IFormFile? File { get; init; }
}

public sealed record DonationApplicationEvidenceResponse(
    Guid Id,
    Guid DonationApplicationId,
    int EvidenceTypeId,
    string EvidenceTypeCode,
    string EvidenceTypeName,
    string? Description,
    string OriginalFileName,
    string? ContentType,
    long FileSizeBytes,
    DateTimeOffset UploadedUtc);

public sealed record DonationAlertResponse(
    Guid DonationId,
    string DonorEntityName,
    string DonationType,
    string StatusCode,
    string StatusName,
    decimal BaseAmount,
    decimal AppliedAmountTotal,
    decimal RemainingAmount,
    decimal AppliedPercentage,
    string AlertState);
