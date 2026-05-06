using FMCPA.Api.Contracts.AdminSecurity;
using FMCPA.Api.Contracts.Documents;

namespace FMCPA.Api.Contracts.Operations;

public sealed record OperationsSummaryResponse(
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<OperationsSeverityConventionResponse> SeverityConvention,
    IReadOnlyList<OperationsKpiResponse> BusinessKpis,
    DocumentSummaryResponse? Documents,
    SecurityActivitySummaryResponse? Security);

public sealed record OperationsKpiResponse(
    string KpiCode,
    string CategoryCode,
    string CategoryName,
    string? ModuleCode,
    string? ModuleName,
    string Label,
    int Count,
    decimal? Amount,
    string SeverityCode,
    string RouteHint);

public sealed record OperationsWorkQueueResponse(
    int TotalCount,
    int ReturnedCount,
    int Skip,
    int Take,
    IReadOnlyList<OperationsSeverityConventionResponse> SeverityConvention,
    IReadOnlyList<OperationsWorkQueueItemResponse> Items);

public sealed record OperationsWorkQueueItemResponse(
    string WorkItemKey,
    string CategoryCode,
    string TypeCode,
    string SeverityCode,
    string ModuleCode,
    string ModuleName,
    string Title,
    string Summary,
    string ReasonCode,
    string RouteHint,
    string? SourceItemKey,
    string? EntityType,
    string? EntityId,
    Guid? DocumentId,
    DateTimeOffset? RelevantUtc);

public sealed record OperationsSeverityConventionResponse(
    string SeverityCode,
    int SortOrder,
    string Description);
