namespace FMCPA.Api.Contracts.Shared;

public sealed record ContactInterventionResponse(
    Guid Id,
    Guid ContactId,
    string ContactName,
    string? ContactTypeName,
    string ModuleKey,
    string OriginType,
    string OriginId,
    string OriginDisplayName,
    string Subject,
    string HelpType,
    string Outcome,
    string? Notes,
    DateTimeOffset OccurredUtc,
    Guid CreatedByUserId,
    string? CreatedByUserName,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc,
    DateTimeOffset? ArchivedUtc);

public sealed record CreateContactInterventionRequest(
    string? ModuleKey,
    string? OriginType,
    string? OriginId,
    string? OriginDisplayName,
    string? Subject,
    string? HelpType,
    string? Outcome,
    string? Notes,
    DateTimeOffset? OccurredUtc);
