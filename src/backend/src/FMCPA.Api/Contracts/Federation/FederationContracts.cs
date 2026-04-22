using Microsoft.AspNetCore.Http;

namespace FMCPA.Api.Contracts.Federation;

public sealed record FederationActionSummaryResponse(
    Guid Id,
    string ActionTypeCode,
    string ActionTypeName,
    string CounterpartyOrInstitution,
    DateOnly ActionDate,
    string Objective,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    bool StatusAlertsEnabledByDefault,
    int ParticipantCount,
    string AlertState,
    string? Notes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc);

public sealed record FederationActionDetailResponse(
    Guid Id,
    string ActionTypeCode,
    string ActionTypeName,
    string CounterpartyOrInstitution,
    DateOnly ActionDate,
    string Objective,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    bool StatusAlertsEnabledByDefault,
    string AlertState,
    string? Notes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc,
    IReadOnlyList<FederationActionParticipantResponse> Participants);

public sealed record CreateFederationActionRequest(
    string ActionTypeCode,
    string CounterpartyOrInstitution,
    DateOnly ActionDate,
    string Objective,
    int StatusCatalogEntryId,
    string? Notes);

public sealed record CreateFederationActionParticipantRequest(
    Guid ContactId,
    string ParticipantSide,
    string? Notes);

public sealed record FederationActionParticipantResponse(
    Guid Id,
    Guid FederationActionId,
    Guid ContactId,
    string ParticipantSide,
    string ContactTypeCode,
    string ContactTypeName,
    string ParticipantName,
    string? OrganizationOrDependency,
    string? RoleTitle,
    string? Notes,
    DateTimeOffset CreatedUtc);

public sealed record FederationDonationSummaryResponse(
    Guid Id,
    string DonorName,
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
    int CommissionCount,
    string AlertState,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc);

public sealed record FederationDonationDetailResponse(
    Guid Id,
    string DonorName,
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
    int CommissionCount,
    int EvidenceCount,
    string AlertState,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc,
    IReadOnlyList<FederationDonationApplicationResponse> Applications);

public sealed record CreateFederationDonationRequest(
    string DonorName,
    DateOnly DonationDate,
    string DonationType,
    decimal BaseAmount,
    string Reference,
    string? Notes,
    int StatusCatalogEntryId);

public sealed record CreateFederationDonationApplicationRequest(
    string BeneficiaryOrDestinationName,
    DateOnly ApplicationDate,
    decimal AppliedAmount,
    int StatusCatalogEntryId,
    string? VerificationDetails,
    string? ClosingDetails);

public sealed record FederationDonationApplicationResponse(
    Guid Id,
    Guid FederationDonationId,
    string BeneficiaryOrDestinationName,
    DateOnly ApplicationDate,
    decimal AppliedAmount,
    int StatusCatalogEntryId,
    string StatusCode,
    string StatusName,
    bool StatusIsClosed,
    string? VerificationDetails,
    string? ClosingDetails,
    int EvidenceCount,
    int CommissionCount,
    IReadOnlyList<FederationDonationApplicationEvidenceResponse> Evidences,
    IReadOnlyList<FederationDonationApplicationCommissionResponse> Commissions,
    DateTimeOffset CreatedUtc);

public sealed class CreateFederationDonationApplicationEvidenceRequest
{
    public int EvidenceTypeId { get; init; }

    public string? Description { get; init; }

    public IFormFile? File { get; init; }
}

public sealed record FederationDonationApplicationEvidenceResponse(
    Guid Id,
    Guid FederationDonationApplicationId,
    int EvidenceTypeId,
    string EvidenceTypeCode,
    string EvidenceTypeName,
    string? Description,
    string OriginalFileName,
    string? ContentType,
    long FileSizeBytes,
    DateTimeOffset UploadedUtc);

public sealed record CreateFederationDonationApplicationCommissionRequest(
    int CommissionTypeId,
    string RecipientCategory,
    Guid? RecipientContactId,
    string RecipientName,
    decimal BaseAmount,
    decimal CommissionAmount,
    string? Notes);

public sealed record FederationDonationApplicationCommissionResponse(
    Guid Id,
    Guid FederationDonationApplicationId,
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

public sealed record FederationActionAlertResponse(
    Guid ActionId,
    string ActionTypeCode,
    string ActionTypeName,
    string CounterpartyOrInstitution,
    DateOnly ActionDate,
    string StatusCode,
    string StatusName,
    string AlertState);

public sealed record FederationDonationAlertResponse(
    Guid DonationId,
    string DonorName,
    string DonationType,
    string StatusCode,
    string StatusName,
    decimal BaseAmount,
    decimal AppliedAmountTotal,
    decimal RemainingAmount,
    decimal AppliedPercentage,
    string AlertState);

public sealed record FederationModuleAlertsResponse(
    IReadOnlyList<FederationActionAlertResponse> Actions,
    IReadOnlyList<FederationDonationAlertResponse> Donations);
