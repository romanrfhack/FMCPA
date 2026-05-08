using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FMCPA.Api.Auth;
using FMCPA.Api.Contracts.Documents;
using FMCPA.Api.DocumentRules;
using FMCPA.Api.Extensions;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Domain.Entities.Audit;
using FMCPA.Domain.Entities.Documents;
using FMCPA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using static FMCPA.Api.DocumentRules.DocumentRuleRegistry;

namespace FMCPA.Api.Endpoints;

public static class DocumentCatalogEndpoints
{
    private const int DefaultTake = 50;
    private const int MaxTake = 200;
    private const string ValidIntegrityState = "VALID";
    private const string DocumentsNavigationPath = "/documents";
    private const string StoredDocumentEntityType = "STORED_DOCUMENT";
    private const string CompleteStatusCode = "COMPLETE";
    private const string IncompleteStatusCode = "INCOMPLETE";
    private const string WorkItemCompletenessPending = "COMPLETENESS_PENDING";
    private const string WorkItemDocumentIntegrityIssue = "DOCUMENT_INTEGRITY_ISSUE";
    private const string WorkItemRetentionReview = "RETENTION_REVIEW";
    private const string SeverityHigh = "HIGH";
    private const string SeverityMedium = "MEDIUM";
    private const string SeverityLow = "LOW";
    private const string CsvContentType = "text/csv; charset=utf-8";
    private const string ExportActionKindView = "VIEW";
    private const string ExportActionKindRemediate = "REMEDIATE";
    private const string ExportActionKindReview = "REVIEW";

    private static readonly IReadOnlySet<string> DocumentTimelineAuditActionTypes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "DOCUMENT_ARCHIVED",
            "DOCUMENT_RESTORED",
            "DOCUMENT_METADATA_UPDATED",
            "DOCUMENT_PRIMARY_CHANGED",
            "DOCUMENT_RETENTION_REVIEWED",
            "DOCUMENT_RETENTION_REVIEW_DEFERRED",
            "DOCUMENT_RETENTION_OVERRIDE_SET",
            "DOCUMENT_RETENTION_OVERRIDE_CLEARED",
            "DOCUMENT_HOLD_SET",
            "DOCUMENT_HOLD_CLEARED",
            "DOCUMENT_REPLACED"
        };

    private static readonly IReadOnlySet<string> WorkQueueItemTypes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            WorkItemCompletenessPending,
            WorkItemDocumentIntegrityIssue,
            WorkItemRetentionReview
        };

    private static readonly IReadOnlySet<string> WorkQueueSeverityCodes =
        new HashSet<string>(StringComparer.Ordinal)
        {
            SeverityHigh,
            SeverityMedium,
            SeverityLow
        };

    private static readonly IReadOnlyDictionary<string, ModuleDocumentAccess> ModuleAccess =
        new Dictionary<string, ModuleDocumentAccess>(StringComparer.OrdinalIgnoreCase)
        {
            ["MARKETS"] = new("MARKETS", "Mercados", PlatformPermissionCodes.MarketsRead),
            ["DONATARIAS"] = new("DONATARIAS", "Donatarias", PlatformPermissionCodes.DonationsRead),
            ["FEDERATION"] = new("FEDERATION", "Federacion", PlatformPermissionCodes.FederationRead)
        };

    public static IEndpointRouteBuilder MapDocumentCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents")
            .WithTags("Documents")
            .RequireAuthorization();

        group.MapGet(
            "/summary",
            async (
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var allowedModuleCodes = ResolveAllowedModuleCodes(httpContext.User);
                if (allowedModuleCodes.Count == 0)
                {
                    return Results.Ok(new DocumentSummaryResponse(
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        0,
                        [],
                        [],
                        [],
                        []));
                }

                return Results.Ok(await BuildDocumentSummaryAsync(
                    dbContext,
                    documentBinaryStore,
                    allowedModuleCodes,
                    cancellationToken));
            });

        group.MapGet(
            string.Empty,
            async (
                string? moduleCode,
                string? entityType,
                Guid? entityId,
                string? documentAreaCode,
                string? integrityState,
                string? documentOperationalStatusCode,
                string? documentClassCode,
                string? retentionPolicyCode,
                string? retentionStatusCode,
                string? statusCode,
                bool? includeArchived,
                DateTimeOffset? fromUtc,
                DateTimeOffset? toUtc,
                int? skip,
                int? take,
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var allowedModuleCodes = ResolveAllowedModuleCodes(httpContext.User);
                var requestedModuleCode = NormalizeOptionalCode(moduleCode);
                if (requestedModuleCode is not null)
                {
                    if (!allowedModuleCodes.Contains(requestedModuleCode, StringComparer.Ordinal))
                    {
                        return Results.Ok(new DocumentCatalogListResponse(
                            0,
                            0,
                            NormalizeSkip(skip),
                            NormalizeTake(take),
                            []));
                    }

                    allowedModuleCodes = [requestedModuleCode];
                }

                if (allowedModuleCodes.Count == 0)
                {
                    return Results.Ok(new DocumentCatalogListResponse(
                        0,
                        0,
                        NormalizeSkip(skip),
                        NormalizeTake(take),
                        []));
                }

                var normalizedEntityType = NormalizeOptionalCode(entityType);
                var normalizedDocumentAreaCode = NormalizeOptionalCode(documentAreaCode);
                var normalizedIntegrityState = NormalizeOptionalCode(integrityState);
                var normalizedOperationalStatusCode = NormalizeOptionalCode(documentOperationalStatusCode);
                var normalizedDocumentClassCode = NormalizeOptionalCode(documentClassCode);
                var normalizedRetentionPolicyCode = NormalizeOptionalCode(retentionPolicyCode);
                var normalizedRetentionStatusCode = NormalizeOptionalCode(retentionStatusCode);
                var normalizedStatusCode = NormalizeOptionalCode(statusCode);
                var normalizedSkip = NormalizeSkip(skip);
                var normalizedTake = NormalizeTake(take);

                if (normalizedDocumentClassCode is not null && !DocumentClassCodes.Supported.Contains(normalizedDocumentClassCode))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["documentClassCode"] = ["DocumentClassCode must be CERTIFICATE, SIGNED_DOCUMENT, SUPPORTING_DOCUMENT, PHOTO_EVIDENCE, VIDEO_EVIDENCE or OTHER."]
                        });
                }

                if (normalizedOperationalStatusCode is not null
                    && !DocumentOperationalStatusCodes.SupportedStatuses.Contains(normalizedOperationalStatusCode))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["documentOperationalStatusCode"] = ["DocumentOperationalStatusCode must be ACTIVE_OK, INTEGRITY_ISSUE, ON_HOLD, REVIEW_DUE, RETENTION_EXPIRED, ARCHIVED or SUPERSEDED."]
                        });
                }

                if (normalizedRetentionPolicyCode is not null
                    && !DocumentRetentionPolicyCodes.SupportedPolicies.Contains(normalizedRetentionPolicyCode))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["retentionPolicyCode"] = ["RetentionPolicyCode must be CERTIFICATE_REVIEW, SIGNED_LONG_TERM, EVIDENCE_MEDIUM_TERM or GENERIC_REVIEW."]
                        });
                }

                if (normalizedRetentionStatusCode is not null
                    && !DocumentRetentionPolicyCodes.SupportedStatuses.Contains(normalizedRetentionStatusCode))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["retentionStatusCode"] = ["RetentionStatusCode must be ACTIVE_RETENTION, REVIEW_DUE or EXPIRED_RETENTION."]
                        });
                }

                if (normalizedStatusCode is not null && !IsSupportedStatusCode(normalizedStatusCode))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["statusCode"] = ["StatusCode must be ACTIVE or ARCHIVED."]
                        });
                }

                var query = dbContext.StoredDocuments
                    .AsNoTracking()
                    .Where(document => allowedModuleCodes.Contains(document.ModuleCode));

                if (normalizedEntityType is not null)
                {
                    query = query.Where(document => document.EntityType == normalizedEntityType);
                }

                if (entityId is not null)
                {
                    query = query.Where(document => document.EntityId == entityId.Value);
                }

                if (normalizedDocumentAreaCode is not null)
                {
                    query = query.Where(document => document.DocumentAreaCode == normalizedDocumentAreaCode);
                }

                if (normalizedDocumentClassCode is not null)
                {
                    query = query.Where(document => document.DocumentClassCode == normalizedDocumentClassCode);
                }

                if (normalizedRetentionPolicyCode is not null)
                {
                    query = ApplyRetentionPolicyFilter(query, normalizedRetentionPolicyCode);
                }

                if (normalizedRetentionStatusCode is not null)
                {
                    query = ApplyRetentionStatusFilter(query, normalizedRetentionStatusCode, DateTimeOffset.UtcNow);
                }

                if (normalizedStatusCode is not null)
                {
                    query = query.Where(document => document.StatusCode == normalizedStatusCode);
                }
                else if (includeArchived != true)
                {
                    query = query.Where(document => document.StatusCode == StoredDocument.ActiveStatusCode);
                }

                if (fromUtc is not null)
                {
                    query = query.Where(document => document.CreatedUtc >= fromUtc.Value);
                }

                if (toUtc is not null)
                {
                    query = query.Where(document => document.CreatedUtc <= toUtc.Value);
                }

                var orderedQuery = query
                    .OrderByDescending(document => document.CreatedUtc)
                    .ThenBy(document => document.OriginalFileName);

                if (normalizedIntegrityState is null && normalizedOperationalStatusCode is null)
                {
                    var totalCount = await orderedQuery.CountAsync(cancellationToken);
                    var pageDocuments = await orderedQuery
                        .Skip(normalizedSkip)
                        .Take(normalizedTake)
                        .ToListAsync(cancellationToken);
                    var pageItems = await BuildItemsAsync(pageDocuments, documentBinaryStore, dbContext, cancellationToken);

                    return Results.Ok(new DocumentCatalogListResponse(
                        totalCount,
                        pageItems.Count,
                        normalizedSkip,
                        normalizedTake,
                        pageItems));
                }

                var candidateDocuments = await orderedQuery.ToListAsync(cancellationToken);
                var inspectedItems = await BuildItemsAsync(candidateDocuments, documentBinaryStore, dbContext, cancellationToken);
                var filteredItems = inspectedItems
                    .Where(item => normalizedIntegrityState is null
                                   || string.Equals(item.IntegrityState, normalizedIntegrityState, StringComparison.Ordinal))
                    .Where(item => normalizedOperationalStatusCode is null
                                   || string.Equals(item.DocumentOperationalStatusCode, normalizedOperationalStatusCode, StringComparison.Ordinal))
                    .ToArray();
                var items = filteredItems
                    .Skip(normalizedSkip)
                    .Take(normalizedTake)
                    .ToArray();

                return Results.Ok(new DocumentCatalogListResponse(
                    filteredItems.Length,
                    items.Length,
                    normalizedSkip,
                    normalizedTake,
                    items));
            });

        group.MapGet(
            "/export",
            async (
                string? moduleCode,
                string? entityType,
                Guid? entityId,
                string? documentAreaCode,
                string? integrityState,
                string? documentOperationalStatusCode,
                string? documentClassCode,
                string? retentionPolicyCode,
                string? retentionStatusCode,
                string? statusCode,
                bool? includeArchived,
                DateTimeOffset? fromUtc,
                DateTimeOffset? toUtc,
                int? skip,
                int? take,
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                return await ExportDocumentCatalogAsync(
                    moduleCode,
                    entityType,
                    entityId,
                    documentAreaCode,
                    integrityState,
                    documentOperationalStatusCode,
                    documentClassCode,
                    retentionPolicyCode,
                    retentionStatusCode,
                    statusCode,
                    includeArchived,
                    fromUtc,
                    toUtc,
                    skip,
                    take,
                    dbContext,
                    documentBinaryStore,
                    httpContext,
                    cancellationToken);
            });

        group.MapGet(
            "/by-entity",
            async (
                string? moduleCode,
                string? entityType,
                Guid? entityId,
                bool? includeArchived,
                int? skip,
                int? take,
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var errors = new Dictionary<string, string[]>();
                var normalizedModuleCode = NormalizeOptionalCode(moduleCode);
                var normalizedEntityType = NormalizeOptionalCode(entityType);
                var normalizedSkip = NormalizeSkip(skip);
                var normalizedTake = NormalizeTake(take);

                if (normalizedModuleCode is null || !ModuleAccess.ContainsKey(normalizedModuleCode))
                {
                    errors["moduleCode"] = ["ModuleCode must be MARKETS, DONATARIAS or FEDERATION."];
                }

                if (normalizedEntityType is null)
                {
                    errors["entityType"] = ["EntityType is required."];
                }
                else if (normalizedModuleCode is not null
                         && ModuleAccess.ContainsKey(normalizedModuleCode)
                         && DocumentRuleRegistry.FindByEntity(normalizedModuleCode, normalizedEntityType) is null)
                {
                    errors["entityType"] = [$"EntityType is not supported for document completeness. Supported combinations are {DocumentRuleRegistry.SupportedEntityCombinationsDescription()}."];
                }

                if (entityId is null || entityId == Guid.Empty)
                {
                    errors["entityId"] = ["EntityId is required."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                if (!HasReadAccess(httpContext.User, normalizedModuleCode!))
                {
                    return DocumentAccessDenied();
                }

                var query = BuildDocumentsByEntityQuery(
                    dbContext,
                    normalizedModuleCode!,
                    normalizedEntityType!,
                    entityId!.Value);

                if (includeArchived != true)
                {
                    query = query.Where(document => document.StatusCode == StoredDocument.ActiveStatusCode);
                }

                var orderedQuery = query
                    .OrderByDescending(document => document.CreatedUtc)
                    .ThenBy(document => document.OriginalFileName);
                var totalCount = await orderedQuery.CountAsync(cancellationToken);
                var documents = await orderedQuery
                    .Skip(normalizedSkip)
                    .Take(normalizedTake)
                    .ToListAsync(cancellationToken);
                var items = await BuildItemsAsync(documents, documentBinaryStore, dbContext, cancellationToken);

                return Results.Ok(new DocumentCatalogListResponse(
                    totalCount,
                    items.Count,
                    normalizedSkip,
                    normalizedTake,
                    items));
            });

        group.MapGet(
            "/timeline/by-entity",
            async (
                string? moduleCode,
                string? entityType,
                Guid? entityId,
                int? take,
                PlatformDbContext dbContext,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var errors = new Dictionary<string, string[]>();
                var normalizedModuleCode = NormalizeOptionalCode(moduleCode);
                var normalizedEntityType = NormalizeOptionalCode(entityType);
                var normalizedTake = NormalizeTake(take);

                if (normalizedModuleCode is null || !ModuleAccess.ContainsKey(normalizedModuleCode))
                {
                    errors["moduleCode"] = ["ModuleCode must be MARKETS, DONATARIAS or FEDERATION."];
                }

                if (normalizedEntityType is null)
                {
                    errors["entityType"] = ["EntityType is required."];
                }
                else if (normalizedModuleCode is not null
                         && ModuleAccess.ContainsKey(normalizedModuleCode)
                         && DocumentRuleRegistry.FindByEntity(normalizedModuleCode, normalizedEntityType) is null)
                {
                    errors["entityType"] = [$"EntityType is not supported for document timeline. Supported combinations are {DocumentRuleRegistry.SupportedEntityCombinationsDescription()}."];
                }

                if (entityId is null || entityId == Guid.Empty)
                {
                    errors["entityId"] = ["EntityId is required."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                if (!HasReadAccess(httpContext.User, normalizedModuleCode!))
                {
                    return DocumentAccessDenied();
                }

                var documents = await BuildDocumentsByEntityQuery(
                        dbContext,
                        normalizedModuleCode!,
                        normalizedEntityType!,
                        entityId!.Value)
                    .OrderBy(document => document.CreatedUtc)
                    .ThenBy(document => document.OriginalFileName)
                    .Take(normalizedTake)
                    .ToListAsync(cancellationToken);

                return Results.Ok(await BuildTimelineResponseAsync(dbContext, documents, cancellationToken));
            });

        group.MapGet(
            "/rules",
            (HttpContext httpContext) =>
            {
                var allowedModuleCodes = ResolveAllowedModuleCodes(httpContext.User);
                var rules = DocumentRuleRegistry.FindByModules(allowedModuleCodes)
                    .OrderBy(rule => rule.ModuleCode, StringComparer.Ordinal)
                    .ThenBy(rule => rule.EntityType, StringComparer.Ordinal)
                    .Select(BuildRuleResponse)
                    .ToArray();

                return Results.Ok(new DocumentRuleListResponse(rules.Length, rules));
            });

        group.MapGet(
            "/requirements/by-entity",
            async (
                string? moduleCode,
                string? entityType,
                Guid? entityId,
                PlatformDbContext dbContext,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var errors = new Dictionary<string, string[]>();
                var normalizedModuleCode = NormalizeOptionalCode(moduleCode);
                var normalizedEntityType = NormalizeOptionalCode(entityType);

                if (normalizedModuleCode is null || !ModuleAccess.ContainsKey(normalizedModuleCode))
                {
                    errors["moduleCode"] = ["ModuleCode must be MARKETS, DONATARIAS or FEDERATION."];
                }

                if (normalizedEntityType is null)
                {
                    errors["entityType"] = ["EntityType is required."];
                }

                if (entityId is null || entityId == Guid.Empty)
                {
                    errors["entityId"] = ["EntityId is required."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                if (!HasReadAccess(httpContext.User, normalizedModuleCode!))
                {
                    return DocumentAccessDenied();
                }

                var rule = DocumentRuleRegistry.FindByEntity(normalizedModuleCode!, normalizedEntityType!);
                if (rule is null)
                {
                    return Results.Ok(BuildNotApplicableRequirementResponse(
                        normalizedModuleCode!,
                        normalizedEntityType!,
                        entityId!.Value));
                }

                var completeness = await BuildCompletenessByEntityAsync(
                    dbContext,
                    normalizedModuleCode!,
                    normalizedEntityType!,
                    entityId!.Value,
                    cancellationToken);

                return completeness is null
                    ? Results.NotFound()
                    : Results.Ok(BuildRequirementResponse(completeness));
            });

        group.MapGet(
            "/completeness/by-entity",
            async (
                string? moduleCode,
                string? entityType,
                Guid? entityId,
                PlatformDbContext dbContext,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var errors = new Dictionary<string, string[]>();
                var normalizedModuleCode = NormalizeOptionalCode(moduleCode);
                var normalizedEntityType = NormalizeOptionalCode(entityType);

                if (normalizedModuleCode is null || !ModuleAccess.ContainsKey(normalizedModuleCode))
                {
                    errors["moduleCode"] = ["ModuleCode must be MARKETS, DONATARIAS or FEDERATION."];
                }

                if (normalizedEntityType is null)
                {
                    errors["entityType"] = ["EntityType is required."];
                }
                else if (normalizedModuleCode is not null
                         && ModuleAccess.ContainsKey(normalizedModuleCode)
                         && DocumentRuleRegistry.FindByEntity(normalizedModuleCode, normalizedEntityType) is null)
                {
                    errors["entityType"] = [$"EntityType is not supported for document completeness. Supported combinations are {DocumentRuleRegistry.SupportedEntityCombinationsDescription()}."];
                }

                if (entityId is null || entityId == Guid.Empty)
                {
                    errors["entityId"] = ["EntityId is required."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                if (!HasReadAccess(httpContext.User, normalizedModuleCode!))
                {
                    return DocumentAccessDenied();
                }

                var completeness = await BuildCompletenessByEntityAsync(
                    dbContext,
                    normalizedModuleCode!,
                    normalizedEntityType!,
                    entityId!.Value,
                    cancellationToken);

                return completeness is null
                    ? Results.NotFound()
                    : Results.Ok(completeness);
            });

        group.MapGet(
            "/pending",
            async (
                string? moduleCode,
                int? skip,
                int? take,
                PlatformDbContext dbContext,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var normalizedModuleCode = NormalizeOptionalCode(moduleCode);
                var normalizedSkip = NormalizeSkip(skip);
                var normalizedTake = NormalizeTake(take);
                var allowedModuleCodes = ResolveAllowedModuleCodes(httpContext.User);

                if (normalizedModuleCode is not null)
                {
                    if (!ModuleAccess.ContainsKey(normalizedModuleCode))
                    {
                        return Results.ValidationProblem(
                            new Dictionary<string, string[]>
                            {
                                ["moduleCode"] = ["ModuleCode must be MARKETS, DONATARIAS or FEDERATION."]
                            });
                    }

                    if (!allowedModuleCodes.Contains(normalizedModuleCode, StringComparer.Ordinal))
                    {
                        return Results.Ok(new DocumentCompletenessListResponse(
                            0,
                            0,
                            normalizedSkip,
                            normalizedTake,
                            []));
                    }

                    allowedModuleCodes = [normalizedModuleCode];
                }

                var pendingItems = new List<DocumentCompletenessResponse>();
                foreach (var rule in DocumentRuleRegistry.FindByModules(allowedModuleCodes))
                {
                    pendingItems.AddRange(await BuildPendingCompletenessAsync(dbContext, rule, cancellationToken));
                }

                var orderedItems = pendingItems
                    .OrderBy(item => item.ModuleCode, StringComparer.Ordinal)
                    .ThenBy(item => item.OriginContext.DisplayName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.EntityId)
                    .ToArray();
                var pageItems = orderedItems
                    .Skip(normalizedSkip)
                    .Take(normalizedTake)
                    .ToArray();

                return Results.Ok(new DocumentCompletenessListResponse(
                    orderedItems.Length,
                    pageItems.Length,
                    normalizedSkip,
                    normalizedTake,
                    pageItems));
            });

        group.MapGet(
            "/work-queue",
            async (
                string? moduleCode,
                string? workItemType,
                string? severityCode,
                int? skip,
                int? take,
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var normalizedModuleCode = NormalizeOptionalCode(moduleCode);
                var normalizedWorkItemType = NormalizeOptionalCode(workItemType);
                var normalizedSeverityCode = NormalizeOptionalCode(severityCode);
                var normalizedSkip = NormalizeSkip(skip);
                var normalizedTake = NormalizeTake(take);
                var errors = new Dictionary<string, string[]>();

                if (normalizedModuleCode is not null && !ModuleAccess.ContainsKey(normalizedModuleCode))
                {
                    errors["moduleCode"] = ["ModuleCode must be MARKETS, DONATARIAS or FEDERATION."];
                }

                if (normalizedWorkItemType is not null && !WorkQueueItemTypes.Contains(normalizedWorkItemType))
                {
                    errors["workItemType"] = ["WorkItemType must be COMPLETENESS_PENDING, DOCUMENT_INTEGRITY_ISSUE or RETENTION_REVIEW."];
                }

                if (normalizedSeverityCode is not null && !WorkQueueSeverityCodes.Contains(normalizedSeverityCode))
                {
                    errors["severityCode"] = ["SeverityCode must be HIGH, MEDIUM or LOW."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var allowedModuleCodes = ResolveAllowedModuleCodes(httpContext.User);
                if (normalizedModuleCode is not null)
                {
                    if (!allowedModuleCodes.Contains(normalizedModuleCode, StringComparer.Ordinal))
                    {
                        return Results.Ok(new DocumentWorkQueueResponse(
                            0,
                            0,
                            normalizedSkip,
                            normalizedTake,
                            []));
                    }

                    allowedModuleCodes = [normalizedModuleCode];
                }

                if (allowedModuleCodes.Count == 0)
                {
                    return Results.Ok(new DocumentWorkQueueResponse(
                        0,
                        0,
                        normalizedSkip,
                        normalizedTake,
                        []));
                }

                var workItems = await BuildDocumentWorkQueueAsync(
                    dbContext,
                    documentBinaryStore,
                    allowedModuleCodes,
                    cancellationToken);

                if (normalizedWorkItemType is not null)
                {
                    workItems = workItems
                        .Where(item => item.WorkItemType == normalizedWorkItemType)
                        .ToArray();
                }

                if (normalizedSeverityCode is not null)
                {
                    workItems = workItems
                        .Where(item => item.SeverityCode == normalizedSeverityCode)
                        .ToArray();
                }

                var orderedItems = workItems
                    .OrderBy(item => ResolveSeveritySortOrder(item.SeverityCode))
                    .ThenBy(item => item.ModuleCode, StringComparer.Ordinal)
                    .ThenBy(item => item.WorkItemType, StringComparer.Ordinal)
                    .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(item => item.WorkItemKey, StringComparer.Ordinal)
                    .ToArray();
                var pageItems = orderedItems
                    .Skip(normalizedSkip)
                    .Take(normalizedTake)
                    .ToArray();

                return Results.Ok(new DocumentWorkQueueResponse(
                    orderedItems.Length,
                    pageItems.Length,
                    normalizedSkip,
                    normalizedTake,
                    pageItems));
            });

        group.MapGet(
            "/work-queue/export",
            async (
                string? moduleCode,
                string? workItemType,
                string? severityCode,
                int? skip,
                int? take,
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                return await ExportDocumentWorkQueueAsync(
                    moduleCode,
                    workItemType,
                    severityCode,
                    skip,
                    take,
                    dbContext,
                    documentBinaryStore,
                    httpContext,
                    cancellationToken);
            });

        group.MapGet(
            "/review-queue",
            async (
                string? moduleCode,
                string? retentionReviewStatusCode,
                int? skip,
                int? take,
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                CancellationToken cancellationToken) =>
            {
                var normalizedModuleCode = NormalizeOptionalCode(moduleCode);
                var normalizedReviewStatusCode = NormalizeOptionalCode(retentionReviewStatusCode);
                var normalizedSkip = NormalizeSkip(skip);
                var normalizedTake = NormalizeTake(take);

                if (normalizedModuleCode is not null && !ModuleAccess.ContainsKey(normalizedModuleCode))
                {
                    return Results.Ok(new DocumentCatalogListResponse(
                        0,
                        0,
                        normalizedSkip,
                        normalizedTake,
                        []));
                }

                if (normalizedReviewStatusCode is not null
                    && !DocumentRetentionReviewStatusCodes.Supported.Contains(normalizedReviewStatusCode))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["retentionReviewStatusCode"] = ["RetentionReviewStatusCode must be REVIEW_PENDING, REVIEW_COMPLETED or REVIEW_DEFERRED."]
                        });
                }

                var moduleCodes = normalizedModuleCode is null
                    ? ModuleAccess.Keys.ToArray()
                    : [normalizedModuleCode];
                var nowUtc = DateTimeOffset.UtcNow;
                var query = dbContext.StoredDocuments
                    .AsNoTracking()
                    .Where(document => moduleCodes.Contains(document.ModuleCode));

                query = ApplyRetentionReviewQueueFilter(query, normalizedReviewStatusCode, nowUtc);

                var orderedQuery = query
                    .OrderBy(document => document.NextRetentionReviewUtc ?? document.RetentionOverrideUntilUtc ?? document.RetentionUntilUtc)
                    .ThenByDescending(document => document.CreatedUtc)
                    .ThenBy(document => document.OriginalFileName);
                var totalCount = await orderedQuery.CountAsync(cancellationToken);
                var documents = await orderedQuery
                    .Skip(normalizedSkip)
                    .Take(normalizedTake)
                    .ToListAsync(cancellationToken);
                var items = await BuildItemsAsync(documents, documentBinaryStore, dbContext, cancellationToken);

                return Results.Ok(new DocumentCatalogListResponse(
                    totalCount,
                    items.Count,
                    normalizedSkip,
                    normalizedTake,
                    items));
            })
            .RequireUsersAdminAccess();

        group.MapGet(
            "/review-queue/export",
            async (
                string? moduleCode,
                string? retentionReviewStatusCode,
                int? skip,
                int? take,
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                return await ExportDocumentRetentionReviewQueueAsync(
                    moduleCode,
                    retentionReviewStatusCode,
                    skip,
                    take,
                    dbContext,
                    documentBinaryStore,
                    httpContext,
                    cancellationToken);
            })
            .RequireUsersAdminAccess();

        group.MapGet(
            "/{documentId:guid}/timeline",
            async (Guid documentId, PlatformDbContext dbContext, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var document = await dbContext.StoredDocuments
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

                if (document is null)
                {
                    return Results.NotFound();
                }

                if (!HasReadAccess(httpContext.User, document.ModuleCode))
                {
                    return DocumentAccessDenied();
                }

                return Results.Ok(await BuildTimelineResponseAsync(dbContext, [document], cancellationToken));
            });

        group.MapGet(
            "/{documentId:guid}",
            async (Guid documentId, PlatformDbContext dbContext, IDocumentBinaryStore documentBinaryStore, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var document = await dbContext.StoredDocuments
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

                if (document is null)
                {
                    return Results.NotFound();
                }

                if (!HasReadAccess(httpContext.User, document.ModuleCode))
                {
                    return DocumentAccessDenied();
                }

                var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
                return Results.Ok(await BuildDetailAsync(document, inspection, dbContext, cancellationToken));
            });

        group.MapPatch(
            "/{documentId:guid}/metadata",
            async (Guid documentId, UpdateStoredDocumentMetadataRequest? request, PlatformDbContext dbContext, IDocumentBinaryStore documentBinaryStore, ClaimsPrincipal principal, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var document = await dbContext.StoredDocuments
                    .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

                if (document is null)
                {
                    return Results.NotFound();
                }

                if (!HasReadAccess(httpContext.User, document.ModuleCode))
                {
                    return DocumentAccessDenied();
                }

                var errors = ValidateMetadataRequest(request, document);
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var wasPrimary = document.IsPrimaryDocument;
                var demotedPrimaryIds = Array.Empty<Guid>();
                if (request!.IsPrimaryDocument)
                {
                    var previousPrimaries = await dbContext.StoredDocuments
                        .Where(item => item.Id != document.Id
                                       && item.DocumentAreaCode == document.DocumentAreaCode
                                       && item.EntityType == document.EntityType
                                       && item.EntityId == document.EntityId
                                       && item.StatusCode == StoredDocument.ActiveStatusCode
                                       && item.SupersededByDocumentId == null
                                       && item.IsPrimaryDocument)
                        .ToListAsync(cancellationToken);

                    demotedPrimaryIds = previousPrimaries.Select(item => item.Id).ToArray();
                    foreach (var previousPrimary in previousPrimaries)
                    {
                        previousPrimary.UpdateMetadata(
                            previousPrimary.DocumentClassCode,
                            previousPrimary.BusinessPurpose,
                            isPrimaryDocument: false,
                            previousPrimary.ClassificationNotes);
                    }
                }

                document.UpdateMetadata(
                    request.DocumentClassCode!,
                    request.BusinessPurpose,
                    request.IsPrimaryDocument,
                    request.ClassificationNotes);

                dbContext.AuditEvents.Add(
                    CreateMetadataAuditEvent(
                        principal,
                        document,
                        "DOCUMENT_METADATA_UPDATED",
                        "Metadata documental actualizada",
                        $"La metadata del documento '{document.OriginalFileName}' fue actualizada.",
                        demotedPrimaryIds));

                if (wasPrimary != document.IsPrimaryDocument || demotedPrimaryIds.Length > 0)
                {
                    dbContext.AuditEvents.Add(
                        CreateMetadataAuditEvent(
                            principal,
                            document,
                            "DOCUMENT_PRIMARY_CHANGED",
                            "Documento principal actualizado",
                            $"La marca de documento principal cambio para '{document.OriginalFileName}'.",
                            demotedPrimaryIds));
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
                return Results.Ok(await BuildDetailAsync(document, inspection, dbContext, cancellationToken));
            })
            .RequireUsersAdminAccess();

        group.MapPatch(
            "/{documentId:guid}/retention-review",
            async (Guid documentId, UpdateStoredDocumentRetentionReviewRequest? request, PlatformDbContext dbContext, IDocumentBinaryStore documentBinaryStore, ClaimsPrincipal principal, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var document = await dbContext.StoredDocuments
                    .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

                if (document is null)
                {
                    return Results.NotFound();
                }

                if (!HasReadAccess(httpContext.User, document.ModuleCode))
                {
                    return DocumentAccessDenied();
                }

                var nowUtc = DateTimeOffset.UtcNow;
                var errors = ValidateRetentionReviewRequest(request, document, nowUtc);
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var normalizedReviewStatusCode = NormalizeOptionalCode(request!.RetentionReviewStatusCode)!;
                var actionType = normalizedReviewStatusCode == DocumentRetentionReviewStatusCodes.Completed
                    ? "DOCUMENT_RETENTION_REVIEWED"
                    : "DOCUMENT_RETENTION_REVIEW_DEFERRED";
                var title = normalizedReviewStatusCode == DocumentRetentionReviewStatusCodes.Completed
                    ? "Revision de retencion completada"
                    : "Revision de retencion diferida";
                var detail = normalizedReviewStatusCode == DocumentRetentionReviewStatusCodes.Completed
                    ? $"El documento '{document.OriginalFileName}' fue marcado como revisado para retencion."
                    : $"La revision de retencion del documento '{document.OriginalFileName}' fue diferida.";

                if (normalizedReviewStatusCode == DocumentRetentionReviewStatusCodes.Completed)
                {
                    document.MarkRetentionReviewed(request.RetentionReviewNotes, nowUtc);
                }
                else
                {
                    document.DeferRetentionReview(request.NextRetentionReviewUtc!.Value, request.RetentionReviewNotes, nowUtc);
                }

                dbContext.AuditEvents.Add(
                    CreateRetentionReviewAuditEvent(
                        principal,
                        document,
                        actionType,
                        title,
                        detail));

                await dbContext.SaveChangesAsync(cancellationToken);

                var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
                return Results.Ok(await BuildDetailAsync(document, inspection, dbContext, cancellationToken));
            })
            .RequireUsersAdminAccess();

        group.MapPatch(
            "/{documentId:guid}/retention-override",
            async (Guid documentId, UpdateStoredDocumentRetentionOverrideRequest? request, PlatformDbContext dbContext, IDocumentBinaryStore documentBinaryStore, ClaimsPrincipal principal, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var document = await dbContext.StoredDocuments
                    .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

                if (document is null)
                {
                    return Results.NotFound();
                }

                if (!HasReadAccess(httpContext.User, document.ModuleCode))
                {
                    return DocumentAccessDenied();
                }

                var errors = ValidateRetentionOverrideRequest(request);
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                document.SetRetentionOverride(
                    request!.RetentionOverridePolicyCode,
                    request.RetentionOverrideUntilUtc,
                    request.RetentionOverrideReason!);

                dbContext.AuditEvents.Add(
                    CreateRetentionOverrideAuditEvent(
                        principal,
                        document,
                        "DOCUMENT_RETENTION_OVERRIDE_SET",
                        "Override de retencion documental aplicado",
                        $"Se aplico override de retencion al documento '{document.OriginalFileName}'."));

                await dbContext.SaveChangesAsync(cancellationToken);

                var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
                return Results.Ok(await BuildDetailAsync(document, inspection, dbContext, cancellationToken));
            })
            .RequireUsersAdminAccess();

        group.MapDelete(
            "/{documentId:guid}/retention-override",
            async (Guid documentId, PlatformDbContext dbContext, IDocumentBinaryStore documentBinaryStore, ClaimsPrincipal principal, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var document = await dbContext.StoredDocuments
                    .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

                if (document is null)
                {
                    return Results.NotFound();
                }

                if (!HasReadAccess(httpContext.User, document.ModuleCode))
                {
                    return DocumentAccessDenied();
                }

                var hadOverride = document.ClearRetentionOverride();
                if (hadOverride)
                {
                    dbContext.AuditEvents.Add(
                        CreateRetentionOverrideAuditEvent(
                            principal,
                            document,
                            "DOCUMENT_RETENTION_OVERRIDE_CLEARED",
                            "Override de retencion documental limpiado",
                            $"Se limpio el override de retencion del documento '{document.OriginalFileName}'."));
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
                return Results.Ok(await BuildDetailAsync(document, inspection, dbContext, cancellationToken));
            })
            .RequireUsersAdminAccess();

        group.MapPost(
            "/{documentId:guid}/hold",
            async (Guid documentId, SetStoredDocumentHoldRequest? request, PlatformDbContext dbContext, IDocumentBinaryStore documentBinaryStore, ClaimsPrincipal principal, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var errors = ValidateHoldRequest(request);
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var document = await dbContext.StoredDocuments
                    .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

                if (document is null)
                {
                    return Results.NotFound();
                }

                if (!HasReadAccess(httpContext.User, document.ModuleCode))
                {
                    return DocumentAccessDenied();
                }

                var actorUserName = principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? "unknown";
                document.SetAdministrativeHold(
                    request!.Reason!,
                    DateTimeOffset.UtcNow,
                    actorUserName);

                dbContext.AuditEvents.Add(
                    CreateHoldAuditEvent(
                        principal,
                        document,
                        "DOCUMENT_HOLD_SET",
                        "Hold administrativo aplicado",
                        $"Se activo hold administrativo para el documento '{document.OriginalFileName}'.",
                        request.Reason));

                await dbContext.SaveChangesAsync(cancellationToken);

                var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
                return Results.Ok(await BuildDetailAsync(document, inspection, dbContext, cancellationToken));
            })
            .RequireUsersAdminAccess();

        group.MapDelete(
            "/{documentId:guid}/hold",
            async (Guid documentId, PlatformDbContext dbContext, IDocumentBinaryStore documentBinaryStore, ClaimsPrincipal principal, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var document = await dbContext.StoredDocuments
                    .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

                if (document is null)
                {
                    return Results.NotFound();
                }

                if (!HasReadAccess(httpContext.User, document.ModuleCode))
                {
                    return DocumentAccessDenied();
                }

                var previousReason = document.HoldReason;
                var cleared = document.ClearAdministrativeHold(DateTimeOffset.UtcNow);
                if (cleared)
                {
                    dbContext.AuditEvents.Add(
                        CreateHoldAuditEvent(
                            principal,
                            document,
                            "DOCUMENT_HOLD_CLEARED",
                            "Hold administrativo limpiado",
                            $"Se limpio el hold administrativo del documento '{document.OriginalFileName}'.",
                            previousReason));
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
                return Results.Ok(await BuildDetailAsync(document, inspection, dbContext, cancellationToken));
            })
            .RequireUsersAdminAccess();

        group.MapPost(
            "/{documentId:guid}/archive",
            async (Guid documentId, ArchiveStoredDocumentRequest? request, PlatformDbContext dbContext, IDocumentBinaryStore documentBinaryStore, ClaimsPrincipal principal, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                if (request?.Reason is { Length: > 500 })
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["reason"] = ["Reason must be 500 characters or fewer."]
                        });
                }

                var document = await dbContext.StoredDocuments
                    .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

                if (document is null)
                {
                    return Results.NotFound();
                }

                if (!HasReadAccess(httpContext.User, document.ModuleCode))
                {
                    return DocumentAccessDenied();
                }

                var archived = document.Archive(request?.Reason, DateTimeOffset.UtcNow);
                if (archived)
                {
                    dbContext.AuditEvents.Add(
                        CreateLifecycleAuditEvent(
                            principal,
                            document,
                            "DOCUMENT_ARCHIVED",
                            "Documento archivado",
                            $"El documento '{document.OriginalFileName}' fue archivado logicamente.",
                            request?.Reason));
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
                return Results.Ok(await BuildDetailAsync(document, inspection, dbContext, cancellationToken));
            })
            .RequireUsersAdminAccess();

        group.MapPost(
            "/{documentId:guid}/restore",
            async (Guid documentId, PlatformDbContext dbContext, IDocumentBinaryStore documentBinaryStore, ClaimsPrincipal principal, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var document = await dbContext.StoredDocuments
                    .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

                if (document is null)
                {
                    return Results.NotFound();
                }

                if (!HasReadAccess(httpContext.User, document.ModuleCode))
                {
                    return DocumentAccessDenied();
                }

                if (document.IsSuperseded)
                {
                    return Results.Conflict(
                        new
                        {
                            message = "El documento fue reemplazado por otro vigente y no puede restaurarse como documento activo."
                        });
                }

                var hasCurrentActiveReplacement = await dbContext.StoredDocuments
                    .AnyAsync(
                        item => item.Id != document.Id
                                && item.DocumentAreaCode == document.DocumentAreaCode
                                && item.EntityType == document.EntityType
                                && item.EntityId == document.EntityId
                                && item.StatusCode == StoredDocument.ActiveStatusCode
                                && item.SupersededByDocumentId == null,
                        cancellationToken);

                if (hasCurrentActiveReplacement)
                {
                    return Results.Conflict(
                        new
                        {
                            message = "Ya existe un documento vigente para la misma entidad y area documental."
                        });
                }

                var restored = document.Restore();
                if (restored)
                {
                    dbContext.AuditEvents.Add(
                        CreateLifecycleAuditEvent(
                            principal,
                            document,
                            "DOCUMENT_RESTORED",
                            "Documento restaurado",
                            $"El documento '{document.OriginalFileName}' fue restaurado a vigente.",
                            reason: null));
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
                return Results.Ok(await BuildDetailAsync(document, inspection, dbContext, cancellationToken));
            })
            .RequireUsersAdminAccess();

        group.MapGet(
            "/{documentId:guid}/download",
            async (Guid documentId, PlatformDbContext dbContext, IDocumentBinaryStore documentBinaryStore, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                var document = await dbContext.StoredDocuments
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

                if (document is null)
                {
                    return Results.NotFound();
                }

                if (!HasReadAccess(httpContext.User, document.ModuleCode))
                {
                    return DocumentAccessDenied();
                }

                var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
                if (!string.Equals(inspection.IntegrityState, ValidIntegrityState, StringComparison.Ordinal))
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Inconsistencia documental detectada",
                        detail: $"El documento no puede descargarse. Estado de integridad: {inspection.IntegrityState}.");
                }

                var download = await documentBinaryStore.OpenReadAsync(
                    document.DocumentAreaCode,
                    document.StoredRelativePath,
                    document.OriginalFileName,
                    document.ContentType,
                    cancellationToken);

                if (download is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Inconsistencia documental detectada",
                        detail: "El documento no se encuentra disponible en el storage local.");
                }

                return DocumentDownloadResponseSupport.File(httpContext, download.Content, download.ContentType, download.OriginalFileName);
            });

        return app;
    }

    private static async Task<IResult> ExportDocumentCatalogAsync(
        string? moduleCode,
        string? entityType,
        Guid? entityId,
        string? documentAreaCode,
        string? integrityState,
        string? documentOperationalStatusCode,
        string? documentClassCode,
        string? retentionPolicyCode,
        string? retentionStatusCode,
        string? statusCode,
        bool? includeArchived,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int? skip,
        int? take,
        PlatformDbContext dbContext,
        IDocumentBinaryStore documentBinaryStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var allowedModuleCodes = ResolveAllowedModuleCodes(httpContext.User);
        var requestedModuleCode = NormalizeOptionalCode(moduleCode);
        var normalizedSkip = NormalizeSkip(skip);
        var normalizedTake = NormalizeTake(take);

        if (requestedModuleCode is not null)
        {
            if (!allowedModuleCodes.Contains(requestedModuleCode, StringComparer.Ordinal))
            {
                return BuildCsvResponse(httpContext, "documents-catalog", BuildCatalogExportCsv([]));
            }

            allowedModuleCodes = [requestedModuleCode];
        }

        if (allowedModuleCodes.Count == 0)
        {
            return BuildCsvResponse(httpContext, "documents-catalog", BuildCatalogExportCsv([]));
        }

        var normalizedEntityType = NormalizeOptionalCode(entityType);
        var normalizedDocumentAreaCode = NormalizeOptionalCode(documentAreaCode);
        var normalizedIntegrityState = NormalizeOptionalCode(integrityState);
        var normalizedOperationalStatusCode = NormalizeOptionalCode(documentOperationalStatusCode);
        var normalizedDocumentClassCode = NormalizeOptionalCode(documentClassCode);
        var normalizedRetentionPolicyCode = NormalizeOptionalCode(retentionPolicyCode);
        var normalizedRetentionStatusCode = NormalizeOptionalCode(retentionStatusCode);
        var normalizedStatusCode = NormalizeOptionalCode(statusCode);

        var errors = ValidateCatalogFilters(
            normalizedDocumentClassCode,
            normalizedOperationalStatusCode,
            normalizedRetentionPolicyCode,
            normalizedRetentionStatusCode,
            normalizedStatusCode);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var query = dbContext.StoredDocuments
            .AsNoTracking()
            .Where(document => allowedModuleCodes.Contains(document.ModuleCode));

        if (normalizedEntityType is not null)
        {
            query = query.Where(document => document.EntityType == normalizedEntityType);
        }

        if (entityId is not null)
        {
            query = query.Where(document => document.EntityId == entityId.Value);
        }

        if (normalizedDocumentAreaCode is not null)
        {
            query = query.Where(document => document.DocumentAreaCode == normalizedDocumentAreaCode);
        }

        if (normalizedDocumentClassCode is not null)
        {
            query = query.Where(document => document.DocumentClassCode == normalizedDocumentClassCode);
        }

        if (normalizedRetentionPolicyCode is not null)
        {
            query = ApplyRetentionPolicyFilter(query, normalizedRetentionPolicyCode);
        }

        if (normalizedRetentionStatusCode is not null)
        {
            query = ApplyRetentionStatusFilter(query, normalizedRetentionStatusCode, DateTimeOffset.UtcNow);
        }

        if (normalizedStatusCode is not null)
        {
            query = query.Where(document => document.StatusCode == normalizedStatusCode);
        }
        else if (includeArchived != true)
        {
            query = query.Where(document => document.StatusCode == StoredDocument.ActiveStatusCode);
        }

        if (fromUtc is not null)
        {
            query = query.Where(document => document.CreatedUtc >= fromUtc.Value);
        }

        if (toUtc is not null)
        {
            query = query.Where(document => document.CreatedUtc <= toUtc.Value);
        }

        var orderedQuery = query
            .OrderByDescending(document => document.CreatedUtc)
            .ThenBy(document => document.OriginalFileName);

        if (normalizedIntegrityState is null && normalizedOperationalStatusCode is null)
        {
            var documents = await orderedQuery
                .Skip(normalizedSkip)
                .Take(normalizedTake)
                .ToListAsync(cancellationToken);
            var items = await BuildItemsAsync(documents, documentBinaryStore, dbContext, cancellationToken);
            return BuildCsvResponse(httpContext, "documents-catalog", BuildCatalogExportCsv(items));
        }

        var candidateDocuments = await orderedQuery.ToListAsync(cancellationToken);
        var inspectedItems = await BuildItemsAsync(candidateDocuments, documentBinaryStore, dbContext, cancellationToken);
        var filteredItems = inspectedItems
            .Where(item => normalizedIntegrityState is null
                           || string.Equals(item.IntegrityState, normalizedIntegrityState, StringComparison.Ordinal))
            .Where(item => normalizedOperationalStatusCode is null
                           || string.Equals(item.DocumentOperationalStatusCode, normalizedOperationalStatusCode, StringComparison.Ordinal))
            .Skip(normalizedSkip)
            .Take(normalizedTake)
            .ToArray();

        return BuildCsvResponse(httpContext, "documents-catalog", BuildCatalogExportCsv(filteredItems));
    }

    private static async Task<IResult> ExportDocumentWorkQueueAsync(
        string? moduleCode,
        string? workItemType,
        string? severityCode,
        int? skip,
        int? take,
        PlatformDbContext dbContext,
        IDocumentBinaryStore documentBinaryStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var normalizedModuleCode = NormalizeOptionalCode(moduleCode);
        var normalizedWorkItemType = NormalizeOptionalCode(workItemType);
        var normalizedSeverityCode = NormalizeOptionalCode(severityCode);
        var normalizedSkip = NormalizeSkip(skip);
        var normalizedTake = NormalizeTake(take);
        var errors = ValidateWorkQueueFilters(normalizedModuleCode, normalizedWorkItemType, normalizedSeverityCode);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var allowedModuleCodes = ResolveAllowedModuleCodes(httpContext.User);
        if (normalizedModuleCode is not null)
        {
            if (!allowedModuleCodes.Contains(normalizedModuleCode, StringComparer.Ordinal))
            {
                return BuildCsvResponse(httpContext, "documents-work-queue", BuildWorkQueueExportCsv([]));
            }

            allowedModuleCodes = [normalizedModuleCode];
        }

        if (allowedModuleCodes.Count == 0)
        {
            return BuildCsvResponse(httpContext, "documents-work-queue", BuildWorkQueueExportCsv([]));
        }

        var workItems = await BuildDocumentWorkQueueAsync(
            dbContext,
            documentBinaryStore,
            allowedModuleCodes,
            cancellationToken);

        if (normalizedWorkItemType is not null)
        {
            workItems = workItems
                .Where(item => item.WorkItemType == normalizedWorkItemType)
                .ToArray();
        }

        if (normalizedSeverityCode is not null)
        {
            workItems = workItems
                .Where(item => item.SeverityCode == normalizedSeverityCode)
                .ToArray();
        }

        var items = workItems
            .OrderBy(item => ResolveSeveritySortOrder(item.SeverityCode))
            .ThenBy(item => item.ModuleCode, StringComparer.Ordinal)
            .ThenBy(item => item.WorkItemType, StringComparer.Ordinal)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.WorkItemKey, StringComparer.Ordinal)
            .Skip(normalizedSkip)
            .Take(normalizedTake)
            .ToArray();

        return BuildCsvResponse(httpContext, "documents-work-queue", BuildWorkQueueExportCsv(items));
    }

    private static async Task<IResult> ExportDocumentRetentionReviewQueueAsync(
        string? moduleCode,
        string? retentionReviewStatusCode,
        int? skip,
        int? take,
        PlatformDbContext dbContext,
        IDocumentBinaryStore documentBinaryStore,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var normalizedModuleCode = NormalizeOptionalCode(moduleCode);
        var normalizedReviewStatusCode = NormalizeOptionalCode(retentionReviewStatusCode);
        var normalizedSkip = NormalizeSkip(skip);
        var normalizedTake = NormalizeTake(take);

        if (normalizedModuleCode is not null && !ModuleAccess.ContainsKey(normalizedModuleCode))
        {
            return BuildCsvResponse(httpContext, "documents-review-queue", BuildReviewQueueExportCsv([]));
        }

        if (normalizedReviewStatusCode is not null
            && !DocumentRetentionReviewStatusCodes.Supported.Contains(normalizedReviewStatusCode))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["retentionReviewStatusCode"] = ["RetentionReviewStatusCode must be REVIEW_PENDING, REVIEW_COMPLETED or REVIEW_DEFERRED."]
                });
        }

        var moduleCodes = normalizedModuleCode is null
            ? ModuleAccess.Keys.ToArray()
            : [normalizedModuleCode];
        var nowUtc = DateTimeOffset.UtcNow;
        var query = dbContext.StoredDocuments
            .AsNoTracking()
            .Where(document => moduleCodes.Contains(document.ModuleCode));

        query = ApplyRetentionReviewQueueFilter(query, normalizedReviewStatusCode, nowUtc);

        var documents = await query
            .OrderBy(document => document.NextRetentionReviewUtc ?? document.RetentionOverrideUntilUtc ?? document.RetentionUntilUtc)
            .ThenByDescending(document => document.CreatedUtc)
            .ThenBy(document => document.OriginalFileName)
            .Skip(normalizedSkip)
            .Take(normalizedTake)
            .ToListAsync(cancellationToken);
        var items = await BuildItemsAsync(documents, documentBinaryStore, dbContext, cancellationToken);

        return BuildCsvResponse(httpContext, "documents-review-queue", BuildReviewQueueExportCsv(items));
    }

    private static Dictionary<string, string[]> ValidateCatalogFilters(
        string? normalizedDocumentClassCode,
        string? normalizedOperationalStatusCode,
        string? normalizedRetentionPolicyCode,
        string? normalizedRetentionStatusCode,
        string? normalizedStatusCode)
    {
        var errors = new Dictionary<string, string[]>();

        if (normalizedDocumentClassCode is not null && !DocumentClassCodes.Supported.Contains(normalizedDocumentClassCode))
        {
            errors["documentClassCode"] = ["DocumentClassCode must be CERTIFICATE, SIGNED_DOCUMENT, SUPPORTING_DOCUMENT, PHOTO_EVIDENCE, VIDEO_EVIDENCE or OTHER."];
        }

        if (normalizedOperationalStatusCode is not null
            && !DocumentOperationalStatusCodes.SupportedStatuses.Contains(normalizedOperationalStatusCode))
        {
            errors["documentOperationalStatusCode"] = ["DocumentOperationalStatusCode must be ACTIVE_OK, INTEGRITY_ISSUE, ON_HOLD, REVIEW_DUE, RETENTION_EXPIRED, ARCHIVED or SUPERSEDED."];
        }

        if (normalizedRetentionPolicyCode is not null
            && !DocumentRetentionPolicyCodes.SupportedPolicies.Contains(normalizedRetentionPolicyCode))
        {
            errors["retentionPolicyCode"] = ["RetentionPolicyCode must be CERTIFICATE_REVIEW, SIGNED_LONG_TERM, EVIDENCE_MEDIUM_TERM or GENERIC_REVIEW."];
        }

        if (normalizedRetentionStatusCode is not null
            && !DocumentRetentionPolicyCodes.SupportedStatuses.Contains(normalizedRetentionStatusCode))
        {
            errors["retentionStatusCode"] = ["RetentionStatusCode must be ACTIVE_RETENTION, REVIEW_DUE or EXPIRED_RETENTION."];
        }

        if (normalizedStatusCode is not null && !IsSupportedStatusCode(normalizedStatusCode))
        {
            errors["statusCode"] = ["StatusCode must be ACTIVE or ARCHIVED."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateWorkQueueFilters(
        string? normalizedModuleCode,
        string? normalizedWorkItemType,
        string? normalizedSeverityCode)
    {
        var errors = new Dictionary<string, string[]>();

        if (normalizedModuleCode is not null && !ModuleAccess.ContainsKey(normalizedModuleCode))
        {
            errors["moduleCode"] = ["ModuleCode must be MARKETS, DONATARIAS or FEDERATION."];
        }

        if (normalizedWorkItemType is not null && !WorkQueueItemTypes.Contains(normalizedWorkItemType))
        {
            errors["workItemType"] = ["WorkItemType must be COMPLETENESS_PENDING, DOCUMENT_INTEGRITY_ISSUE or RETENTION_REVIEW."];
        }

        if (normalizedSeverityCode is not null && !WorkQueueSeverityCodes.Contains(normalizedSeverityCode))
        {
            errors["severityCode"] = ["SeverityCode must be HIGH, MEDIUM or LOW."];
        }

        return errors;
    }

    private static string BuildCatalogExportCsv(IReadOnlyList<DocumentCatalogItemResponse> items)
    {
        return BuildCsv(
            [
                "documentId",
                "moduleCode",
                "moduleName",
                "documentAreaCode",
                "entityType",
                "entityId",
                "originDisplayName",
                "originSummary",
                "originRouteHint",
                "originalFileName",
                "documentClassCode",
                "statusCode",
                "documentOperationalStatusCode",
                "documentOperationalSeverityCode",
                "integrityState",
                "retentionPolicyCode",
                "retentionStatusCode",
                "retentionEffectivePolicyCode",
                "retentionEffectiveUntilUtc",
                "retentionReviewStatusCode",
                "isAdministrativeHold",
                "isSuperseded",
                "createdUtc"
            ],
            items.Select(item => new string?[]
            {
                item.Id.ToString(),
                item.ModuleCode,
                item.ModuleName,
                item.DocumentAreaCode,
                item.EntityType,
                item.EntityId.ToString(),
                item.OriginContext.DisplayName,
                item.OriginContext.Summary,
                item.OriginContext.RouteHint,
                item.OriginalFileName,
                item.DocumentClassCode,
                item.StatusCode,
                item.DocumentOperationalStatusCode,
                item.DocumentOperationalSeverityCode,
                item.IntegrityState,
                item.RetentionPolicyCode,
                item.RetentionStatusCode,
                item.RetentionEffectivePolicyCode,
                FormatCsvDate(item.RetentionEffectiveUntilUtc),
                item.RetentionReviewStatusCode,
                FormatCsvBool(item.IsAdministrativeHold),
                FormatCsvBool(item.IsSuperseded),
                FormatCsvDate(item.CreatedUtc)
            }));
    }

    private static string BuildWorkQueueExportCsv(IReadOnlyList<DocumentWorkQueueItemResponse> items)
    {
        return BuildCsv(
            [
                "workItemKey",
                "workItemType",
                "severity",
                "moduleCode",
                "moduleName",
                "entityType",
                "entityId",
                "originDisplayName",
                "documentId",
                "title",
                "summary",
                "reasonCode",
                "currentStatusCode",
                "documentOperationalStatusCode",
                "actionKind",
                "routeHint",
                "remediationHint"
            ],
            items.Select(item => new string?[]
            {
                item.WorkItemKey,
                item.WorkItemType,
                item.SeverityCode,
                item.ModuleCode,
                item.ModuleName,
                item.EntityType,
                item.EntityId.ToString(),
                item.OriginContext.DisplayName,
                item.DocumentId?.ToString(),
                item.Title,
                item.Summary,
                item.ReasonCode,
                item.CurrentStatusCode,
                item.DocumentOperationalStatusCode,
                ResolveWorkQueueExportActionKind(item),
                item.RouteHint,
                item.RemediationHint
            }));
    }

    private static string BuildReviewQueueExportCsv(IReadOnlyList<DocumentCatalogItemResponse> items)
    {
        return BuildCsv(
            [
                "documentId",
                "moduleCode",
                "moduleName",
                "entityType",
                "entityId",
                "originDisplayName",
                "documentClassCode",
                "retentionStatusCode",
                "retentionReviewStatusCode",
                "retentionEffectivePolicyCode",
                "retentionEffectiveUntilUtc",
                "nextRetentionReviewUtc",
                "isAdministrativeHold",
                "documentOperationalStatusCode",
                "integrityState",
                "createdUtc"
            ],
            items.Select(item => new string?[]
            {
                item.Id.ToString(),
                item.ModuleCode,
                item.ModuleName,
                item.EntityType,
                item.EntityId.ToString(),
                item.OriginContext.DisplayName,
                item.DocumentClassCode,
                item.RetentionStatusCode,
                item.RetentionReviewStatusCode,
                item.RetentionEffectivePolicyCode,
                FormatCsvDate(item.RetentionEffectiveUntilUtc),
                FormatCsvDate(item.NextRetentionReviewUtc),
                FormatCsvBool(item.IsAdministrativeHold),
                item.DocumentOperationalStatusCode,
                item.IntegrityState,
                FormatCsvDate(item.CreatedUtc)
            }));
    }

    private static string BuildCsv(IReadOnlyList<string> headers, IEnumerable<string?[]> rows)
    {
        var builder = new StringBuilder();
        AppendCsvRow(builder, headers);

        foreach (var row in rows)
        {
            AppendCsvRow(builder, row);
        }

        return builder.ToString();
    }

    private static void AppendCsvRow(StringBuilder builder, IEnumerable<string?> values)
    {
        var isFirst = true;
        foreach (var value in values)
        {
            if (!isFirst)
            {
                builder.Append(',');
            }

            builder.Append(EscapeCsvValue(value));
            isFirst = false;
        }

        builder.AppendLine();
    }

    private static string EscapeCsvValue(string? value)
    {
        var normalized = (value ?? string.Empty)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

        if (normalized.Length > 0 && normalized[0] is '=' or '+' or '-' or '@')
        {
            normalized = $"'{normalized}";
        }

        return normalized.Contains('"') || normalized.Contains(',') || normalized.Contains(' ')
            ? $"\"{normalized.Replace("\"", "\"\"")}\""
            : normalized;
    }

    private static IResult BuildCsvResponse(HttpContext httpContext, string fileNamePrefix, string csv)
    {
        httpContext.Response.Headers.CacheControl = "no-store";
        httpContext.Response.Headers.Pragma = "no-cache";
        httpContext.Response.Headers.Expires = "0";

        var fileName = $"{fileNamePrefix}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.csv";
        return Results.File(
            Encoding.UTF8.GetBytes(csv),
            CsvContentType,
            fileDownloadName: fileName);
    }

    private static string ResolveWorkQueueExportActionKind(DocumentWorkQueueItemResponse item)
    {
        return item.WorkItemType switch
        {
            WorkItemCompletenessPending => ExportActionKindRemediate,
            WorkItemDocumentIntegrityIssue or WorkItemRetentionReview => ExportActionKindReview,
            _ => ExportActionKindView
        };
    }

    private static string FormatCsvDate(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("O");
    }

    private static string? FormatCsvDate(DateTimeOffset? value)
    {
        return value is null ? null : FormatCsvDate(value.Value);
    }

    private static string FormatCsvBool(bool value)
    {
        return value ? "true" : "false";
    }

    private static async Task<IReadOnlyList<DocumentCatalogItemResponse>> BuildItemsAsync(
        IReadOnlyList<StoredDocument> documents,
        IDocumentBinaryStore documentBinaryStore,
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var items = new List<DocumentCatalogItemResponse>(documents.Count);
        var referenceUtc = DateTimeOffset.UtcNow;
        var originContexts = await ResolveOriginContextsAsync(documents, dbContext, cancellationToken);

        foreach (var document in documents)
        {
            var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
            items.Add(BuildItem(
                document,
                inspection,
                originContexts.GetValueOrDefault(document.Id) ?? BuildFallbackOriginContext(document),
                referenceUtc));
        }

        return items;
    }

    private static Task<DocumentBinaryInspection> InspectAsync(
        StoredDocument document,
        IDocumentBinaryStore documentBinaryStore,
        CancellationToken cancellationToken)
    {
        return documentBinaryStore.InspectAsync(
            document.DocumentAreaCode,
            document.StoredRelativePath,
            document.SizeBytes,
            cancellationToken);
    }

    private static DocumentCatalogItemResponse BuildItem(
        StoredDocument document,
        DocumentBinaryInspection inspection,
        DocumentOriginContextResponse originContext,
        DateTimeOffset referenceUtc)
    {
        var moduleName = ResolveModuleName(document.ModuleCode);
        var effectiveRetentionPolicyCode = document.ResolveEffectiveRetentionPolicyCode();
        var effectiveRetentionUntilUtc = document.ResolveEffectiveRetentionUntilUtc();
        var operationalStatusCode = DocumentOperationalStatusCodes.Resolve(document, inspection.IntegrityState, referenceUtc);
        var operationalSeverityCode = DocumentOperationalStatusCodes.ResolveSeverity(operationalStatusCode);

        return new DocumentCatalogItemResponse(
            document.Id,
            document.ModuleCode,
            moduleName,
            document.DocumentAreaCode,
            document.EntityType,
            document.EntityId,
            originContext,
            document.OriginalFileName,
            document.ContentType,
            document.SizeBytes,
            document.CreatedUtc,
            document.DocumentClassCode,
            document.BusinessPurpose,
            document.IsPrimaryDocument,
            document.ClassificationNotes,
            effectiveRetentionPolicyCode,
            effectiveRetentionUntilUtc,
            document.ResolveRetentionStatusCode(referenceUtc),
            document.RetentionPolicyCode,
            document.RetentionUntilUtc,
            effectiveRetentionPolicyCode,
            effectiveRetentionUntilUtc,
            document.HasRetentionOverride,
            document.RetentionOverridePolicyCode,
            document.RetentionOverrideUntilUtc,
            document.RetentionOverrideReason,
            document.RetentionReviewStatusCode,
            document.LastRetentionReviewUtc,
            document.NextRetentionReviewUtc,
            document.RetentionReviewNotes,
            document.IsAdministrativeHold,
            document.HoldReason,
            document.HoldPlacedUtc,
            document.HoldReleasedUtc,
            document.HoldPlacedBy,
            operationalStatusCode,
            operationalSeverityCode,
            document.StatusCode,
            document.ArchivedUtc,
            document.ArchiveReason,
            document.IsSuperseded,
            document.SupersededByDocumentId,
            document.ReplacedDocumentId,
            document.ReplacementGroupKey,
            inspection.IntegrityState,
            inspection.ActualSizeBytes,
            !string.IsNullOrWhiteSpace(document.Sha256Hex),
            document.IsLegacyBackfill,
            $"/api/documents/{document.Id}",
            $"/api/documents/{document.Id}/download");
    }

    private static async Task<DocumentCatalogDetailResponse> BuildDetailAsync(
        StoredDocument document,
        DocumentBinaryInspection inspection,
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var referenceUtc = DateTimeOffset.UtcNow;
        var originContexts = await ResolveOriginContextsAsync([document], dbContext, cancellationToken);
        var originContext = originContexts.GetValueOrDefault(document.Id) ?? BuildFallbackOriginContext(document);
        var effectiveRetentionPolicyCode = document.ResolveEffectiveRetentionPolicyCode();
        var effectiveRetentionUntilUtc = document.ResolveEffectiveRetentionUntilUtc();
        var operationalStatusCode = DocumentOperationalStatusCodes.Resolve(document, inspection.IntegrityState, referenceUtc);
        var operationalSeverityCode = DocumentOperationalStatusCodes.ResolveSeverity(operationalStatusCode);

        return new DocumentCatalogDetailResponse(
            document.Id,
            document.ModuleCode,
            ResolveModuleName(document.ModuleCode),
            document.DocumentAreaCode,
            document.EntityType,
            document.EntityId,
            originContext,
            document.OriginalFileName,
            document.ContentType,
            document.SizeBytes,
            document.CreatedUtc,
            document.DocumentClassCode,
            document.BusinessPurpose,
            document.IsPrimaryDocument,
            document.ClassificationNotes,
            effectiveRetentionPolicyCode,
            effectiveRetentionUntilUtc,
            document.ResolveRetentionStatusCode(referenceUtc),
            document.RetentionPolicyCode,
            document.RetentionUntilUtc,
            effectiveRetentionPolicyCode,
            effectiveRetentionUntilUtc,
            document.HasRetentionOverride,
            document.RetentionOverridePolicyCode,
            document.RetentionOverrideUntilUtc,
            document.RetentionOverrideReason,
            document.RetentionReviewStatusCode,
            document.LastRetentionReviewUtc,
            document.NextRetentionReviewUtc,
            document.RetentionReviewNotes,
            document.IsAdministrativeHold,
            document.HoldReason,
            document.HoldPlacedUtc,
            document.HoldReleasedUtc,
            document.HoldPlacedBy,
            operationalStatusCode,
            operationalSeverityCode,
            document.StatusCode,
            document.ArchivedUtc,
            document.ArchiveReason,
            document.IsSuperseded,
            document.SupersededByDocumentId,
            document.ReplacedDocumentId,
            document.ReplacementGroupKey,
            inspection.IntegrityState,
            inspection.ActualSizeBytes,
            !string.IsNullOrWhiteSpace(document.Sha256Hex),
            document.Sha256Hex,
            document.IsLegacyBackfill,
            $"/api/documents/{document.Id}/download");
    }

    private static async Task<DocumentTimelineResponse> BuildTimelineResponseAsync(
        PlatformDbContext dbContext,
        IReadOnlyList<StoredDocument> documents,
        CancellationToken cancellationToken)
    {
        if (documents.Count == 0)
        {
            return new DocumentTimelineResponse(0, []);
        }

        var documentIds = documents.Select(document => document.Id).Distinct().ToArray();
        var relatedDocumentIds = documents
            .SelectMany(document => new[] { document.ReplacedDocumentId, document.SupersededByDocumentId })
            .Where(documentId => documentId is not null && !documentIds.Contains(documentId.Value))
            .Select(documentId => documentId!.Value)
            .Distinct()
            .ToArray();

        var relatedDocuments = relatedDocumentIds.Length == 0
            ? []
            : await dbContext.StoredDocuments
                .AsNoTracking()
                .Where(document => relatedDocumentIds.Contains(document.Id))
                .ToListAsync(cancellationToken);

        var documentsById = documents
            .Concat(relatedDocuments)
            .GroupBy(document => document.Id)
            .ToDictionary(group => group.Key, group => group.First());
        var documentIdTexts = documentIds.Select(documentId => documentId.ToString()).ToArray();
        var timelineActionTypes = DocumentTimelineAuditActionTypes.ToArray();
        var auditEvents = await dbContext.AuditEvents
            .AsNoTracking()
            .Where(auditEvent =>
                auditEvent.EntityType == StoredDocumentEntityType
                && documentIdTexts.Contains(auditEvent.EntityId)
                && timelineActionTypes.Contains(auditEvent.ActionType))
            .ToListAsync(cancellationToken);

        var events = new List<DocumentTimelineEventResponse>();
        foreach (var document in documents)
        {
            events.Add(BuildSyntheticTimelineEvent(
                document,
                "DOCUMENT_UPLOADED",
                "Documento cargado",
                $"El documento '{document.OriginalFileName}' fue cargado en el area {document.DocumentAreaCode}.",
                document.CreatedUtc,
                relatedDocumentId: null,
                documentsById));

            if (document.SupersededByDocumentId is Guid supersedingDocumentId)
            {
                events.Add(BuildSyntheticTimelineEvent(
                    document,
                    "DOCUMENT_SUPERSEDED",
                    "Documento sustituido",
                    $"El documento '{document.OriginalFileName}' fue sustituido por otro documento vigente.",
                    document.ArchivedUtc ?? document.CreatedUtc,
                    supersedingDocumentId,
                    documentsById));
            }
        }

        foreach (var auditEvent in auditEvents)
        {
            if (!Guid.TryParse(auditEvent.EntityId, out var documentId)
                || !documentsById.TryGetValue(documentId, out var document))
            {
                continue;
            }

            events.Add(BuildAuditTimelineEvent(auditEvent, document, documentsById));
        }

        var orderedEvents = events
            .OrderBy(item => item.OccurredUtc)
            .ThenBy(item => item.EventType, StringComparer.Ordinal)
            .ThenBy(item => item.DocumentOriginalFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new DocumentTimelineResponse(orderedEvents.Length, orderedEvents);
    }

    private static DocumentTimelineEventResponse BuildSyntheticTimelineEvent(
        StoredDocument document,
        string eventType,
        string title,
        string detail,
        DateTimeOffset occurredUtc,
        Guid? relatedDocumentId,
        IReadOnlyDictionary<Guid, StoredDocument> documentsById)
    {
        var relatedDocumentName = relatedDocumentId is Guid value && documentsById.TryGetValue(value, out var relatedDocument)
            ? relatedDocument.OriginalFileName
            : null;

        return new DocumentTimelineEventResponse(
            AuditEventId: null,
            document.Id,
            document.OriginalFileName,
            document.ModuleCode,
            ResolveModuleName(document.ModuleCode),
            document.DocumentAreaCode,
            document.EntityType,
            document.EntityId,
            occurredUtc,
            eventType,
            title,
            detail,
            relatedDocumentId,
            relatedDocumentName,
            document.StatusCode,
            IsCurrentDocument(document),
            document.IsSuperseded);
    }

    private static DocumentTimelineEventResponse BuildAuditTimelineEvent(
        AuditEvent auditEvent,
        StoredDocument document,
        IReadOnlyDictionary<Guid, StoredDocument> documentsById)
    {
        var relatedDocumentId = string.Equals(auditEvent.ActionType, "DOCUMENT_REPLACED", StringComparison.Ordinal)
            ? document.ReplacedDocumentId
            : null;
        var relatedDocumentName = relatedDocumentId is Guid value && documentsById.TryGetValue(value, out var relatedDocument)
            ? relatedDocument.OriginalFileName
            : null;

        return new DocumentTimelineEventResponse(
            auditEvent.Id,
            document.Id,
            document.OriginalFileName,
            document.ModuleCode,
            ResolveModuleName(document.ModuleCode),
            document.DocumentAreaCode,
            document.EntityType,
            document.EntityId,
            auditEvent.OccurredUtc,
            auditEvent.ActionType,
            auditEvent.Title,
            auditEvent.Detail,
            relatedDocumentId,
            relatedDocumentName,
            document.StatusCode,
            IsCurrentDocument(document),
            document.IsSuperseded);
    }

    private static bool IsCurrentDocument(StoredDocument document)
    {
        return string.Equals(document.StatusCode, StoredDocument.ActiveStatusCode, StringComparison.Ordinal)
               && !document.IsSuperseded;
    }

    internal static async Task<DocumentSummaryResponse> BuildDocumentSummaryAsync(
        PlatformDbContext dbContext,
        IDocumentBinaryStore documentBinaryStore,
        IReadOnlyList<string> allowedModuleCodes,
        CancellationToken cancellationToken)
    {
        var documents = await dbContext.StoredDocuments
            .AsNoTracking()
            .Where(document => allowedModuleCodes.Contains(document.ModuleCode))
            .ToListAsync(cancellationToken);
        var workItems = await BuildDocumentWorkQueueAsync(
            dbContext,
            documentBinaryStore,
            allowedModuleCodes,
            cancellationToken);

        var modules = allowedModuleCodes
            .Select(moduleCode =>
            {
                var moduleDocuments = documents
                    .Where(document => string.Equals(document.ModuleCode, moduleCode, StringComparison.Ordinal))
                    .ToArray();
                var moduleWorkItems = workItems
                    .Where(item => string.Equals(item.ModuleCode, moduleCode, StringComparison.Ordinal))
                    .ToArray();

                return new DocumentSummaryModuleResponse(
                    moduleCode,
                    ResolveModuleName(moduleCode),
                    moduleDocuments.Length,
                    moduleDocuments.Count(document => string.Equals(document.StatusCode, StoredDocument.ActiveStatusCode, StringComparison.Ordinal)),
                    moduleDocuments.Count(document => string.Equals(document.StatusCode, StoredDocument.ArchivedStatusCode, StringComparison.Ordinal)),
                    moduleWorkItems.Count(item => string.Equals(item.WorkItemType, WorkItemDocumentIntegrityIssue, StringComparison.Ordinal)),
                    moduleWorkItems.Count(item => string.Equals(item.WorkItemType, WorkItemCompletenessPending, StringComparison.Ordinal)),
                    moduleWorkItems.Count(item =>
                        string.Equals(item.WorkItemType, WorkItemRetentionReview, StringComparison.Ordinal)
                        && string.Equals(item.ReasonCode, DocumentRetentionPolicyCodes.ReviewDue, StringComparison.Ordinal)),
                    moduleWorkItems.Count(item =>
                        string.Equals(item.WorkItemType, WorkItemRetentionReview, StringComparison.Ordinal)
                        && string.Equals(item.ReasonCode, DocumentRetentionPolicyCodes.ExpiredRetention, StringComparison.Ordinal)),
                    moduleDocuments.Count(document => document.IsAdministrativeHold));
            })
            .OrderBy(item => item.ModuleCode, StringComparer.Ordinal)
            .ToArray();

        var documentClasses = documents
            .GroupBy(document => document.DocumentClassCode, StringComparer.Ordinal)
            .Select(group => new DocumentSummaryClassResponse(
                group.Key,
                group.Count(),
                group.Count(document => string.Equals(document.StatusCode, StoredDocument.ActiveStatusCode, StringComparison.Ordinal)),
                group.Count(document => string.Equals(document.StatusCode, StoredDocument.ArchivedStatusCode, StringComparison.Ordinal))))
            .OrderBy(item => item.DocumentClassCode, StringComparer.Ordinal)
            .ToArray();

        var operationalStatuses = await BuildOperationalStatusSummaryAsync(
            documents,
            documentBinaryStore,
            cancellationToken);

        var workQueueCategories = workItems
            .GroupBy(
                item => new
                {
                    item.WorkItemType,
                    item.ReasonCode,
                    item.SeverityCode
                })
            .Select(group => new DocumentSummaryWorkQueueCategoryResponse(
                group.Key.WorkItemType,
                group.Key.ReasonCode,
                group.Key.SeverityCode,
                group.Count()))
            .OrderBy(item => ResolveSeveritySortOrder(item.SeverityCode))
            .ThenBy(item => item.WorkItemType, StringComparer.Ordinal)
            .ThenBy(item => item.ReasonCode, StringComparer.Ordinal)
            .ToArray();

        return new DocumentSummaryResponse(
            documents.Count,
            documents.Count(document => string.Equals(document.StatusCode, StoredDocument.ActiveStatusCode, StringComparison.Ordinal)),
            documents.Count(document => string.Equals(document.StatusCode, StoredDocument.ArchivedStatusCode, StringComparison.Ordinal)),
            workItems.Count(item => string.Equals(item.WorkItemType, WorkItemDocumentIntegrityIssue, StringComparison.Ordinal)),
            workItems.Count(item => string.Equals(item.WorkItemType, WorkItemCompletenessPending, StringComparison.Ordinal)),
            workItems.Count(item =>
                string.Equals(item.WorkItemType, WorkItemRetentionReview, StringComparison.Ordinal)
                && string.Equals(item.ReasonCode, DocumentRetentionPolicyCodes.ReviewDue, StringComparison.Ordinal)),
            workItems.Count(item =>
                string.Equals(item.WorkItemType, WorkItemRetentionReview, StringComparison.Ordinal)
                && string.Equals(item.ReasonCode, DocumentRetentionPolicyCodes.ExpiredRetention, StringComparison.Ordinal)),
            documents.Count(document => document.IsAdministrativeHold),
            modules,
            documentClasses,
            operationalStatuses,
            workQueueCategories);
    }

    private static async Task<IReadOnlyList<DocumentSummaryOperationalStatusResponse>> BuildOperationalStatusSummaryAsync(
        IReadOnlyList<StoredDocument> documents,
        IDocumentBinaryStore documentBinaryStore,
        CancellationToken cancellationToken)
    {
        var referenceUtc = DateTimeOffset.UtcNow;
        var statusCodes = new List<string>(documents.Count);

        foreach (var document in documents)
        {
            var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
            statusCodes.Add(DocumentOperationalStatusCodes.Resolve(document, inspection.IntegrityState, referenceUtc));
        }

        return statusCodes
            .GroupBy(statusCode => statusCode, StringComparer.Ordinal)
            .Select(group => new DocumentSummaryOperationalStatusResponse(
                group.Key,
                DocumentOperationalStatusCodes.ResolveSeverity(group.Key),
                group.Count()))
            .OrderBy(item => DocumentOperationalStatusCodes.ResolveSortOrder(item.DocumentOperationalStatusCode))
            .ToArray();
    }

    internal static async Task<IReadOnlyList<DocumentWorkQueueItemResponse>> BuildDocumentWorkQueueAsync(
        PlatformDbContext dbContext,
        IDocumentBinaryStore documentBinaryStore,
        IReadOnlyList<string> allowedModuleCodes,
        CancellationToken cancellationToken)
    {
        var items = new List<DocumentWorkQueueItemResponse>();

        foreach (var rule in DocumentRuleRegistry.FindByModules(allowedModuleCodes))
        {
            var pendingCompleteness = await BuildPendingCompletenessAsync(dbContext, rule, cancellationToken);
            items.AddRange(pendingCompleteness.Select(BuildCompletenessWorkQueueItem));
        }

        var documents = await dbContext.StoredDocuments
            .AsNoTracking()
            .Where(document => allowedModuleCodes.Contains(document.ModuleCode))
            .OrderByDescending(document => document.CreatedUtc)
            .ToListAsync(cancellationToken);
        var originContexts = await ResolveOriginContextsAsync(documents, dbContext, cancellationToken);

        foreach (var document in documents)
        {
            var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
            if (string.Equals(inspection.IntegrityState, ValidIntegrityState, StringComparison.Ordinal))
            {
                continue;
            }

            items.Add(BuildIntegrityWorkQueueItem(
                document,
                inspection,
                originContexts.GetValueOrDefault(document.Id) ?? BuildFallbackOriginContext(document)));
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var retentionQuery = dbContext.StoredDocuments
            .AsNoTracking()
            .Where(document => allowedModuleCodes.Contains(document.ModuleCode));
        var retentionDocuments = await ApplyRetentionReviewQueueFilter(retentionQuery, retentionReviewStatusCode: null, nowUtc)
            .OrderBy(document => document.NextRetentionReviewUtc ?? document.RetentionOverrideUntilUtc ?? document.RetentionUntilUtc)
            .ThenByDescending(document => document.CreatedUtc)
            .ToListAsync(cancellationToken);
        var retentionOriginContexts = await ResolveOriginContextsAsync(retentionDocuments, dbContext, cancellationToken);

        foreach (var document in retentionDocuments)
        {
            var inspection = await InspectAsync(document, documentBinaryStore, cancellationToken);
            items.Add(BuildRetentionWorkQueueItem(
                document,
                inspection,
                retentionOriginContexts.GetValueOrDefault(document.Id) ?? BuildFallbackOriginContext(document),
                nowUtc));
        }

        return items;
    }

    private static DocumentWorkQueueItemResponse BuildCompletenessWorkQueueItem(DocumentCompletenessResponse completeness)
    {
        return new DocumentWorkQueueItemResponse(
            $"{WorkItemCompletenessPending}:{completeness.ModuleCode}:{completeness.EntityType}:{completeness.EntityId:N}",
            WorkItemCompletenessPending,
            SeverityHigh,
            completeness.ModuleCode,
            completeness.ModuleName,
            completeness.EntityType,
            completeness.EntityId,
            completeness.OriginContext,
            DocumentId: null,
            DocumentOriginalFileName: null,
            $"Falta documentacion requerida: {completeness.OriginContext.DisplayName}",
            completeness.MissingReasonDescription
                ?? $"La entidad no cumple el requisito {completeness.RequiredDocumentCode}.",
            completeness.MissingReasonCode ?? "MISSING_REQUIRED_DOCUMENT",
            completeness.StatusCode,
            DocumentOperationalStatusCode: null,
            DocumentOperationalSeverityCode: null,
            completeness.OriginContext.RouteHint ?? DocumentsNavigationPath,
            DocumentDetailUrl: null,
            completeness.RemediationHint,
            RelevantUtc: null);
    }

    private static DocumentWorkQueueItemResponse BuildIntegrityWorkQueueItem(
        StoredDocument document,
        DocumentBinaryInspection inspection,
        DocumentOriginContextResponse originContext)
    {
        var operationalStatusCode = DocumentOperationalStatusCodes.Resolve(document, inspection.IntegrityState, DateTimeOffset.UtcNow);

        return new DocumentWorkQueueItemResponse(
            $"{WorkItemDocumentIntegrityIssue}:{document.Id:N}",
            WorkItemDocumentIntegrityIssue,
            SeverityHigh,
            document.ModuleCode,
            ResolveModuleName(document.ModuleCode),
            originContext.EntityType,
            originContext.EntityId,
            originContext,
            document.Id,
            document.OriginalFileName,
            $"Integridad documental requiere revision: {document.OriginalFileName}",
            $"El documento reporta estado de integridad {inspection.IntegrityState}.",
            inspection.IntegrityState,
            inspection.IntegrityState,
            operationalStatusCode,
            DocumentOperationalStatusCodes.ResolveSeverity(operationalStatusCode),
            originContext.RouteHint ?? DocumentsNavigationPath,
            $"/api/documents/{document.Id}",
            "Revisar metadata/storage local desde el detalle documental; no se descarga hasta recuperar integridad VALID.",
            RelevantUtc: null);
    }

    private static DocumentWorkQueueItemResponse BuildRetentionWorkQueueItem(
        StoredDocument document,
        DocumentBinaryInspection inspection,
        DocumentOriginContextResponse originContext,
        DateTimeOffset referenceUtc)
    {
        var retentionStatusCode = document.ResolveRetentionStatusCode(referenceUtc);
        var severityCode = string.Equals(retentionStatusCode, DocumentRetentionPolicyCodes.ExpiredRetention, StringComparison.Ordinal)
            ? SeverityMedium
            : SeverityLow;
        var reasonCode = string.Equals(retentionStatusCode, DocumentRetentionPolicyCodes.ExpiredRetention, StringComparison.Ordinal)
            ? DocumentRetentionPolicyCodes.ExpiredRetention
            : DocumentRetentionPolicyCodes.ReviewDue;
        var operationalStatusCode = DocumentOperationalStatusCodes.Resolve(document, inspection.IntegrityState, referenceUtc);

        return new DocumentWorkQueueItemResponse(
            $"{WorkItemRetentionReview}:{document.Id:N}",
            WorkItemRetentionReview,
            severityCode,
            document.ModuleCode,
            ResolveModuleName(document.ModuleCode),
            originContext.EntityType,
            originContext.EntityId,
            originContext,
            document.Id,
            document.OriginalFileName,
            $"Revision de retencion pendiente: {document.OriginalFileName}",
            $"Politica efectiva {document.ResolveEffectiveRetentionPolicyCode()}; retener hasta {document.ResolveEffectiveRetentionUntilUtc():yyyy-MM-dd}.",
            reasonCode,
            retentionStatusCode,
            operationalStatusCode,
            DocumentOperationalStatusCodes.ResolveSeverity(operationalStatusCode),
            originContext.RouteHint ?? DocumentsNavigationPath,
            $"/api/documents/{document.Id}",
            "Revisar la retencion operativa; ADMIN puede marcar revisado o diferir en la bandeja de revision documental.",
            document.NextRetentionReviewUtc ?? document.ResolveEffectiveRetentionUntilUtc());
    }

    private static int ResolveSeveritySortOrder(string severityCode)
    {
        return severityCode switch
        {
            SeverityHigh => 0,
            SeverityMedium => 1,
            SeverityLow => 2,
            _ => 3
        };
    }

    private static DocumentRuleResponse BuildRuleResponse(DocumentRuleDescriptor rule)
    {
        return new DocumentRuleResponse(
            rule.RuleCode,
            rule.ModuleCode,
            ResolveModuleName(rule.ModuleCode),
            rule.EntityType,
            rule.CoveredDocumentEntityType,
            rule.DocumentAreaCode,
            rule.RequiredDocumentClassCodes,
            rule.MinimumRequiredCount,
            rule.RequiredDocumentCode,
            rule.RequiredDocumentDescription,
            rule.MissingReasonCode,
            rule.MissingReasonDescription,
            rule.RemediationHint);
    }

    private static DocumentRequirementResponse BuildRequirementResponse(DocumentCompletenessResponse completeness)
    {
        return new DocumentRequirementResponse(
            completeness.ModuleCode,
            completeness.ModuleName,
            completeness.EntityType,
            completeness.EntityId,
            completeness.OriginContext,
            AppliesRule: true,
            completeness.RuleCode,
            completeness.StatusCode,
            completeness.IsComplete,
            completeness.RequiredDocumentCode,
            completeness.RequiredDocumentDescription,
            completeness.RequiredDocumentClassCodes,
            completeness.MinimumRequiredCount,
            completeness.RelatedDocumentCount,
            completeness.MissingReasonCode,
            completeness.MissingReasonDescription,
            completeness.RemediationHint,
            completeness.CoveringDocumentIds);
    }

    private static DocumentRequirementResponse BuildNotApplicableRequirementResponse(
        string moduleCode,
        string entityType,
        Guid entityId)
    {
        return new DocumentRequirementResponse(
            moduleCode,
            ResolveModuleName(moduleCode),
            entityType,
            entityId,
            new DocumentOriginContextResponse(
                moduleCode,
                ResolveModuleName(moduleCode),
                entityType,
                entityId,
                $"{entityType} {entityId.ToString("N")[..8]}",
                "Sin regla documental minima configurada para esta entidad.",
                ResolveModuleRouteHint(moduleCode)),
            AppliesRule: false,
            RuleCode: null,
            StatusCode: "NOT_APPLICABLE",
            IsComplete: null,
            RequiredDocumentCode: null,
            RequiredDocumentDescription: null,
            RequiredDocumentClassCodes: [],
            MinimumRequiredCount: 0,
            CurrentDocumentCount: 0,
            MissingReasonCode: null,
            MissingReasonDescription: null,
            RemediationHint: null,
            CoveringDocumentIds: []);
    }

    private static IQueryable<StoredDocument> BuildDocumentsByEntityQuery(
        PlatformDbContext dbContext,
        string moduleCode,
        string entityType,
        Guid entityId)
    {
        var documents = dbContext.StoredDocuments
            .AsNoTracking()
            .Where(document => document.ModuleCode == moduleCode);

        if (string.Equals(moduleCode, "MARKETS", StringComparison.Ordinal)
            && string.Equals(entityType, MarketTenantEntityType, StringComparison.Ordinal))
        {
            return documents.Where(document =>
                document.EntityType == MarketTenantEntityType
                && document.EntityId == entityId);
        }

        if (string.Equals(moduleCode, "DONATARIAS", StringComparison.Ordinal)
            && string.Equals(entityType, DonationApplicationEntityType, StringComparison.Ordinal))
        {
            var evidenceIds = dbContext.DonationApplicationEvidences
                .AsNoTracking()
                .Where(evidence => evidence.DonationApplicationId == entityId)
                .Select(evidence => evidence.Id);

            return documents.Where(document =>
                (document.EntityType == DonationApplicationEntityType && document.EntityId == entityId)
                || (document.EntityType == DonationEvidenceEntityType && evidenceIds.Contains(document.EntityId)));
        }

        if (string.Equals(moduleCode, "FEDERATION", StringComparison.Ordinal)
            && string.Equals(entityType, FederationDonationApplicationEntityType, StringComparison.Ordinal))
        {
            var evidenceIds = dbContext.FederationDonationApplicationEvidences
                .AsNoTracking()
                .Where(evidence => evidence.FederationDonationApplicationId == entityId)
                .Select(evidence => evidence.Id);

            return documents.Where(document =>
                (document.EntityType == FederationDonationApplicationEntityType && document.EntityId == entityId)
                || (document.EntityType == FederationDonationEvidenceEntityType && evidenceIds.Contains(document.EntityId)));
        }

        return documents.Where(document =>
            document.EntityType == entityType
            && document.EntityId == entityId);
    }

    private static async Task<DocumentCompletenessResponse?> BuildCompletenessByEntityAsync(
        PlatformDbContext dbContext,
        string moduleCode,
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var rule = DocumentRuleRegistry.FindByEntity(moduleCode, entityType);
        if (rule is null)
        {
            return null;
        }

        if (string.Equals(rule.RuleCode, MarketTenantCertificateRuleCode, StringComparison.Ordinal))
        {
            var tenant = await ResolveMarketTenantCompletenessContextAsync(dbContext, entityId, cancellationToken);
            if (tenant is null)
            {
                return null;
            }

            var documentIds = await BuildMarketTenantCertificateDocumentQuery(dbContext, rule, entityId)
                .Select(document => document.Id)
                .ToListAsync(cancellationToken);

            return BuildCompletenessResponse(
                rule,
                entityId,
                BuildMarketTenantOriginContext(tenant),
                documentIds);
        }

        if (string.Equals(rule.RuleCode, DonationApplicationEvidenceRuleCode, StringComparison.Ordinal))
        {
            var application = await ResolveDonationApplicationCompletenessContextAsync(dbContext, entityId, cancellationToken);
            if (application is null)
            {
                return null;
            }

            var documentIds = await BuildDonationApplicationEvidenceDocumentQuery(dbContext, rule, entityId)
                .Select(document => document.Id)
                .ToListAsync(cancellationToken);

            return BuildCompletenessResponse(
                rule,
                entityId,
                BuildDonationApplicationOriginContext(application),
                documentIds);
        }

        if (string.Equals(rule.RuleCode, FederationDonationApplicationEvidenceRuleCode, StringComparison.Ordinal))
        {
            var application = await ResolveFederationApplicationCompletenessContextAsync(dbContext, entityId, cancellationToken);
            if (application is null)
            {
                return null;
            }

            var documentIds = await BuildFederationApplicationEvidenceDocumentQuery(dbContext, rule, entityId)
                .Select(document => document.Id)
                .ToListAsync(cancellationToken);

            return BuildCompletenessResponse(
                rule,
                entityId,
                BuildFederationApplicationOriginContext(application),
                documentIds);
        }

        return null;
    }

    private static Task<IReadOnlyList<DocumentCompletenessResponse>> BuildPendingCompletenessAsync(
        PlatformDbContext dbContext,
        DocumentRuleDescriptor rule,
        CancellationToken cancellationToken)
    {
        return rule.RuleCode switch
        {
            MarketTenantCertificateRuleCode => BuildMarketTenantPendingCompletenessAsync(dbContext, rule, cancellationToken),
            DonationApplicationEvidenceRuleCode => BuildDonationApplicationPendingCompletenessAsync(dbContext, rule, cancellationToken),
            FederationDonationApplicationEvidenceRuleCode => BuildFederationApplicationPendingCompletenessAsync(dbContext, rule, cancellationToken),
            _ => Task.FromResult<IReadOnlyList<DocumentCompletenessResponse>>([])
        };
    }

    private static async Task<IReadOnlyList<DocumentCompletenessResponse>> BuildMarketTenantPendingCompletenessAsync(
        PlatformDbContext dbContext,
        DocumentRuleDescriptor rule,
        CancellationToken cancellationToken)
    {
        var coveredTenantIds = await BuildMarketTenantCertificateDocumentQuery(dbContext, rule, tenantId: null)
            .Select(document => document.EntityId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var tenants = await dbContext.MarketTenants
            .AsNoTracking()
            .Where(tenant => !coveredTenantIds.Contains(tenant.Id))
            .Select(tenant => new
            {
                tenant.Id,
                tenant.MarketId,
                tenant.TenantName,
                tenant.CertificateNumber
            })
            .ToListAsync(cancellationToken);
        var marketIds = tenants.Select(tenant => tenant.MarketId).Distinct().ToArray();
        var marketNamesById = await dbContext.Markets
            .AsNoTracking()
            .Where(market => marketIds.Contains(market.Id))
            .Select(market => new
            {
                market.Id,
                market.Name
            })
            .ToDictionaryAsync(market => market.Id, market => market.Name, cancellationToken);

        return tenants
            .Select(tenant =>
            {
                marketNamesById.TryGetValue(tenant.MarketId, out var marketName);
                return BuildCompletenessResponse(
                    rule,
                    tenant.Id,
                    BuildMarketTenantOriginContext(new MarketTenantCompletenessContext(
                        tenant.Id,
                        tenant.MarketId,
                        tenant.TenantName,
                        tenant.CertificateNumber,
                        marketName)),
                    []);
            })
            .ToArray();
    }

    private static async Task<IReadOnlyList<DocumentCompletenessResponse>> BuildDonationApplicationPendingCompletenessAsync(
        PlatformDbContext dbContext,
        DocumentRuleDescriptor rule,
        CancellationToken cancellationToken)
    {
        var coveredDocuments = BuildActiveRuleDocumentQuery(dbContext, rule);
        var coveredApplicationIds = await (
                from evidence in dbContext.DonationApplicationEvidences.AsNoTracking()
                join document in coveredDocuments
                    on evidence.Id equals document.EntityId
                select evidence.DonationApplicationId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var applications = await dbContext.DonationApplications
            .AsNoTracking()
            .Where(application => !coveredApplicationIds.Contains(application.Id))
            .Select(application => new
            {
                application.Id,
                application.DonationId,
                application.BeneficiaryName
            })
            .ToListAsync(cancellationToken);
        var donationIds = applications.Select(application => application.DonationId).Distinct().ToArray();
        var donationsById = await dbContext.Donations
            .AsNoTracking()
            .Where(donation => donationIds.Contains(donation.Id))
            .Select(donation => new
            {
                donation.Id,
                donation.DonorEntityName,
                donation.Reference
            })
            .ToDictionaryAsync(donation => donation.Id, cancellationToken);

        return applications
            .Select(application =>
            {
                donationsById.TryGetValue(application.DonationId, out var donation);
                return BuildCompletenessResponse(
                    rule,
                    application.Id,
                    BuildDonationApplicationOriginContext(new DonationApplicationCompletenessContext(
                        application.Id,
                        application.DonationId,
                        application.BeneficiaryName,
                        donation?.DonorEntityName,
                        donation?.Reference)),
                    []);
            })
            .ToArray();
    }

    private static async Task<IReadOnlyList<DocumentCompletenessResponse>> BuildFederationApplicationPendingCompletenessAsync(
        PlatformDbContext dbContext,
        DocumentRuleDescriptor rule,
        CancellationToken cancellationToken)
    {
        var coveredDocuments = BuildActiveRuleDocumentQuery(dbContext, rule);
        var coveredApplicationIds = await (
                from evidence in dbContext.FederationDonationApplicationEvidences.AsNoTracking()
                join document in coveredDocuments
                    on evidence.Id equals document.EntityId
                select evidence.FederationDonationApplicationId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var applications = await dbContext.FederationDonationApplications
            .AsNoTracking()
            .Where(application => !coveredApplicationIds.Contains(application.Id))
            .Select(application => new
            {
                application.Id,
                application.FederationDonationId,
                application.BeneficiaryOrDestinationName
            })
            .ToListAsync(cancellationToken);
        var donationIds = applications.Select(application => application.FederationDonationId).Distinct().ToArray();
        var donationsById = await dbContext.FederationDonations
            .AsNoTracking()
            .Where(donation => donationIds.Contains(donation.Id))
            .Select(donation => new
            {
                donation.Id,
                donation.DonorName,
                donation.Reference
            })
            .ToDictionaryAsync(donation => donation.Id, cancellationToken);

        return applications
            .Select(application =>
            {
                donationsById.TryGetValue(application.FederationDonationId, out var donation);
                return BuildCompletenessResponse(
                    rule,
                    application.Id,
                    BuildFederationApplicationOriginContext(new FederationApplicationCompletenessContext(
                        application.Id,
                        application.FederationDonationId,
                        application.BeneficiaryOrDestinationName,
                        donation?.DonorName,
                        donation?.Reference)),
                    []);
            })
            .ToArray();
    }

    private static IQueryable<StoredDocument> BuildMarketTenantCertificateDocumentQuery(
        PlatformDbContext dbContext,
        DocumentRuleDescriptor rule,
        Guid? tenantId)
    {
        var query = BuildActiveRuleDocumentQuery(dbContext, rule);

        return tenantId is null
            ? query
            : query.Where(document => document.EntityId == tenantId.Value);
    }

    private static IQueryable<StoredDocument> BuildDonationApplicationEvidenceDocumentQuery(
        PlatformDbContext dbContext,
        DocumentRuleDescriptor rule,
        Guid applicationId)
    {
        var evidenceIds = dbContext.DonationApplicationEvidences
            .AsNoTracking()
            .Where(evidence => evidence.DonationApplicationId == applicationId)
            .Select(evidence => evidence.Id);

        return BuildActiveRuleDocumentQuery(dbContext, rule)
            .Where(document => evidenceIds.Contains(document.EntityId));
    }

    private static IQueryable<StoredDocument> BuildFederationApplicationEvidenceDocumentQuery(
        PlatformDbContext dbContext,
        DocumentRuleDescriptor rule,
        Guid applicationId)
    {
        var evidenceIds = dbContext.FederationDonationApplicationEvidences
            .AsNoTracking()
            .Where(evidence => evidence.FederationDonationApplicationId == applicationId)
            .Select(evidence => evidence.Id);

        return BuildActiveRuleDocumentQuery(dbContext, rule)
            .Where(document => evidenceIds.Contains(document.EntityId));
    }

    private static IQueryable<StoredDocument> BuildActiveRuleDocumentQuery(
        PlatformDbContext dbContext,
        DocumentRuleDescriptor rule)
    {
        var requiredClassCodes = rule.RequiredDocumentClassCodes.ToArray();

        return dbContext.StoredDocuments
            .AsNoTracking()
            .Where(document =>
                document.ModuleCode == rule.ModuleCode
                && document.EntityType == rule.CoveredDocumentEntityType
                && document.DocumentAreaCode == rule.DocumentAreaCode
                && requiredClassCodes.Contains(document.DocumentClassCode)
                && document.StatusCode == StoredDocument.ActiveStatusCode
                && document.SupersededByDocumentId == null
            );
    }

    private static DocumentCompletenessResponse BuildCompletenessResponse(
        DocumentRuleDescriptor rule,
        Guid entityId,
        DocumentOriginContextResponse originContext,
        IReadOnlyList<Guid> documentIds)
    {
        var isComplete = documentIds.Count >= rule.MinimumRequiredCount;
        return new DocumentCompletenessResponse(
            rule.ModuleCode,
            ResolveModuleName(rule.ModuleCode),
            rule.EntityType,
            entityId,
            originContext,
            rule.RuleCode,
            isComplete ? CompleteStatusCode : IncompleteStatusCode,
            isComplete,
            rule.RequiredDocumentCode,
            rule.RequiredDocumentDescription,
            rule.RequiredDocumentClassCodes,
            rule.MinimumRequiredCount,
            isComplete ? null : rule.MissingReasonCode,
            isComplete ? null : rule.MissingReasonDescription,
            rule.RemediationHint,
            documentIds.Count,
            documentIds);
    }

    private static async Task<MarketTenantCompletenessContext?> ResolveMarketTenantCompletenessContextAsync(
        PlatformDbContext dbContext,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var tenant = await dbContext.MarketTenants
            .AsNoTracking()
            .Where(item => item.Id == tenantId)
            .Select(item => new
            {
                item.Id,
                item.MarketId,
                item.TenantName,
                item.CertificateNumber
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (tenant is null)
        {
            return null;
        }

        var marketName = await dbContext.Markets
            .AsNoTracking()
            .Where(market => market.Id == tenant.MarketId)
            .Select(market => market.Name)
            .SingleOrDefaultAsync(cancellationToken);

        return new MarketTenantCompletenessContext(
            tenant.Id,
            tenant.MarketId,
            tenant.TenantName,
            tenant.CertificateNumber,
            marketName);
    }

    private static async Task<DonationApplicationCompletenessContext?> ResolveDonationApplicationCompletenessContextAsync(
        PlatformDbContext dbContext,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await dbContext.DonationApplications
            .AsNoTracking()
            .Where(item => item.Id == applicationId)
            .Select(item => new
            {
                item.Id,
                item.DonationId,
                item.BeneficiaryName
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (application is null)
        {
            return null;
        }

        var donation = await dbContext.Donations
            .AsNoTracking()
            .Where(item => item.Id == application.DonationId)
            .Select(item => new
            {
                item.DonorEntityName,
                item.Reference
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new DonationApplicationCompletenessContext(
            application.Id,
            application.DonationId,
            application.BeneficiaryName,
            donation?.DonorEntityName,
            donation?.Reference);
    }

    private static async Task<FederationApplicationCompletenessContext?> ResolveFederationApplicationCompletenessContextAsync(
        PlatformDbContext dbContext,
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var application = await dbContext.FederationDonationApplications
            .AsNoTracking()
            .Where(item => item.Id == applicationId)
            .Select(item => new
            {
                item.Id,
                item.FederationDonationId,
                item.BeneficiaryOrDestinationName
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (application is null)
        {
            return null;
        }

        var donation = await dbContext.FederationDonations
            .AsNoTracking()
            .Where(item => item.Id == application.FederationDonationId)
            .Select(item => new
            {
                item.DonorName,
                item.Reference
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new FederationApplicationCompletenessContext(
            application.Id,
            application.FederationDonationId,
            application.BeneficiaryOrDestinationName,
            donation?.DonorName,
            donation?.Reference);
    }

    private static DocumentOriginContextResponse BuildMarketTenantOriginContext(MarketTenantCompletenessContext tenant)
    {
        return new DocumentOriginContextResponse(
            "MARKETS",
            ResolveModuleName("MARKETS"),
            MarketTenantEntityType,
            tenant.TenantId,
            tenant.TenantName,
            string.IsNullOrWhiteSpace(tenant.MarketName)
                ? $"Cedula {tenant.CertificateNumber}"
                : $"{tenant.MarketName} · Cedula {tenant.CertificateNumber}",
            $"/markets?marketId={tenant.MarketId}&tenantId={tenant.TenantId}");
    }

    private static DocumentOriginContextResponse BuildDonationApplicationOriginContext(DonationApplicationCompletenessContext application)
    {
        return new DocumentOriginContextResponse(
            "DONATARIAS",
            ResolveModuleName("DONATARIAS"),
            DonationApplicationEntityType,
            application.ApplicationId,
            application.BeneficiaryName,
            string.IsNullOrWhiteSpace(application.DonorEntityName)
                ? "Aplicacion de donacion"
                : $"{application.DonorEntityName} · {application.Reference}",
            $"/donatarias?donationId={application.DonationId}&applicationId={application.ApplicationId}");
    }

    private static DocumentOriginContextResponse BuildFederationApplicationOriginContext(FederationApplicationCompletenessContext application)
    {
        return new DocumentOriginContextResponse(
            "FEDERATION",
            ResolveModuleName("FEDERATION"),
            FederationDonationApplicationEntityType,
            application.ApplicationId,
            application.BeneficiaryOrDestinationName,
            string.IsNullOrWhiteSpace(application.DonorName)
                ? "Aplicacion federacion"
                : $"{application.DonorName} · {application.Reference}",
            $"/federation?donationId={application.FederationDonationId}&applicationId={application.ApplicationId}");
    }

    private static async Task<IReadOnlyDictionary<Guid, DocumentOriginContextResponse>> ResolveOriginContextsAsync(
        IReadOnlyList<StoredDocument> documents,
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var contexts = new Dictionary<Guid, DocumentOriginContextResponse>();
        if (documents.Count == 0)
        {
            return contexts;
        }

        await AddMarketTenantOriginContextsAsync(documents, dbContext, contexts, cancellationToken);
        await AddDonationEvidenceOriginContextsAsync(documents, dbContext, contexts, cancellationToken);
        await AddFederationEvidenceOriginContextsAsync(documents, dbContext, contexts, cancellationToken);

        return contexts;
    }

    private static async Task AddMarketTenantOriginContextsAsync(
        IReadOnlyList<StoredDocument> documents,
        PlatformDbContext dbContext,
        IDictionary<Guid, DocumentOriginContextResponse> contexts,
        CancellationToken cancellationToken)
    {
        var tenantDocuments = documents
            .Where(document => string.Equals(document.EntityType, MarketTenantEntityType, StringComparison.Ordinal))
            .ToArray();
        if (tenantDocuments.Length == 0)
        {
            return;
        }

        var tenantIds = tenantDocuments.Select(document => document.EntityId).Distinct().ToArray();
        var tenants = await dbContext.MarketTenants
            .AsNoTracking()
            .Where(tenant => tenantIds.Contains(tenant.Id))
            .Select(tenant => new
            {
                tenant.Id,
                tenant.MarketId,
                tenant.TenantName,
                tenant.CertificateNumber
            })
            .ToListAsync(cancellationToken);
        var marketIds = tenants.Select(tenant => tenant.MarketId).Distinct().ToArray();
        var markets = await dbContext.Markets
            .AsNoTracking()
            .Where(market => marketIds.Contains(market.Id))
            .Select(market => new
            {
                market.Id,
                market.Name
            })
            .ToListAsync(cancellationToken);
        var tenantsById = tenants.ToDictionary(tenant => tenant.Id);
        var marketNamesById = markets.ToDictionary(market => market.Id, market => market.Name);

        foreach (var document in tenantDocuments)
        {
            if (!tenantsById.TryGetValue(document.EntityId, out var tenant))
            {
                continue;
            }

            marketNamesById.TryGetValue(tenant.MarketId, out var marketName);
            contexts[document.Id] = new DocumentOriginContextResponse(
                document.ModuleCode,
                ResolveModuleName(document.ModuleCode),
                MarketTenantEntityType,
                tenant.Id,
                tenant.TenantName,
                string.IsNullOrWhiteSpace(marketName)
                    ? $"Cedula {tenant.CertificateNumber}"
                    : $"{marketName} · Cedula {tenant.CertificateNumber}",
                $"/markets?marketId={tenant.MarketId}&tenantId={tenant.Id}");
        }
    }

    private static async Task AddDonationEvidenceOriginContextsAsync(
        IReadOnlyList<StoredDocument> documents,
        PlatformDbContext dbContext,
        IDictionary<Guid, DocumentOriginContextResponse> contexts,
        CancellationToken cancellationToken)
    {
        var evidenceDocuments = documents
            .Where(document => string.Equals(document.EntityType, DonationEvidenceEntityType, StringComparison.Ordinal))
            .ToArray();
        if (evidenceDocuments.Length == 0)
        {
            return;
        }

        var evidenceIds = evidenceDocuments.Select(document => document.EntityId).Distinct().ToArray();
        var evidences = await dbContext.DonationApplicationEvidences
            .AsNoTracking()
            .Where(evidence => evidenceIds.Contains(evidence.Id))
            .Select(evidence => new
            {
                evidence.Id,
                evidence.DonationApplicationId,
                evidence.OriginalFileName
            })
            .ToListAsync(cancellationToken);
        var applicationIds = evidences.Select(evidence => evidence.DonationApplicationId).Distinct().ToArray();
        var applications = await dbContext.DonationApplications
            .AsNoTracking()
            .Where(application => applicationIds.Contains(application.Id))
            .Select(application => new
            {
                application.Id,
                application.DonationId,
                application.BeneficiaryName
            })
            .ToListAsync(cancellationToken);
        var donationIds = applications.Select(application => application.DonationId).Distinct().ToArray();
        var donations = await dbContext.Donations
            .AsNoTracking()
            .Where(donation => donationIds.Contains(donation.Id))
            .Select(donation => new
            {
                donation.Id,
                donation.DonorEntityName,
                donation.Reference
            })
            .ToListAsync(cancellationToken);
        var evidencesById = evidences.ToDictionary(evidence => evidence.Id);
        var applicationsById = applications.ToDictionary(application => application.Id);
        var donationsById = donations.ToDictionary(donation => donation.Id);

        foreach (var document in evidenceDocuments)
        {
            if (!evidencesById.TryGetValue(document.EntityId, out var evidence)
                || !applicationsById.TryGetValue(evidence.DonationApplicationId, out var application))
            {
                continue;
            }

            donationsById.TryGetValue(application.DonationId, out var donation);
            contexts[document.Id] = new DocumentOriginContextResponse(
                document.ModuleCode,
                ResolveModuleName(document.ModuleCode),
                DonationApplicationEntityType,
                application.Id,
                application.BeneficiaryName,
                donation is null
                    ? $"Evidencia {evidence.OriginalFileName}"
                    : $"{donation.DonorEntityName} · {donation.Reference} · Evidencia {evidence.OriginalFileName}",
                $"/donatarias?donationId={application.DonationId}&applicationId={application.Id}");
        }
    }

    private static async Task AddFederationEvidenceOriginContextsAsync(
        IReadOnlyList<StoredDocument> documents,
        PlatformDbContext dbContext,
        IDictionary<Guid, DocumentOriginContextResponse> contexts,
        CancellationToken cancellationToken)
    {
        var evidenceDocuments = documents
            .Where(document => string.Equals(document.EntityType, FederationDonationEvidenceEntityType, StringComparison.Ordinal))
            .ToArray();
        if (evidenceDocuments.Length == 0)
        {
            return;
        }

        var evidenceIds = evidenceDocuments.Select(document => document.EntityId).Distinct().ToArray();
        var evidences = await dbContext.FederationDonationApplicationEvidences
            .AsNoTracking()
            .Where(evidence => evidenceIds.Contains(evidence.Id))
            .Select(evidence => new
            {
                evidence.Id,
                evidence.FederationDonationApplicationId,
                evidence.OriginalFileName
            })
            .ToListAsync(cancellationToken);
        var applicationIds = evidences.Select(evidence => evidence.FederationDonationApplicationId).Distinct().ToArray();
        var applications = await dbContext.FederationDonationApplications
            .AsNoTracking()
            .Where(application => applicationIds.Contains(application.Id))
            .Select(application => new
            {
                application.Id,
                application.FederationDonationId,
                application.BeneficiaryOrDestinationName
            })
            .ToListAsync(cancellationToken);
        var donationIds = applications.Select(application => application.FederationDonationId).Distinct().ToArray();
        var donations = await dbContext.FederationDonations
            .AsNoTracking()
            .Where(donation => donationIds.Contains(donation.Id))
            .Select(donation => new
            {
                donation.Id,
                donation.DonorName,
                donation.Reference
            })
            .ToListAsync(cancellationToken);
        var evidencesById = evidences.ToDictionary(evidence => evidence.Id);
        var applicationsById = applications.ToDictionary(application => application.Id);
        var donationsById = donations.ToDictionary(donation => donation.Id);

        foreach (var document in evidenceDocuments)
        {
            if (!evidencesById.TryGetValue(document.EntityId, out var evidence)
                || !applicationsById.TryGetValue(evidence.FederationDonationApplicationId, out var application))
            {
                continue;
            }

            donationsById.TryGetValue(application.FederationDonationId, out var donation);
            contexts[document.Id] = new DocumentOriginContextResponse(
                document.ModuleCode,
                ResolveModuleName(document.ModuleCode),
                FederationDonationApplicationEntityType,
                application.Id,
                application.BeneficiaryOrDestinationName,
                donation is null
                    ? $"Evidencia {evidence.OriginalFileName}"
                    : $"{donation.DonorName} · {donation.Reference} · Evidencia {evidence.OriginalFileName}",
                $"/federation?donationId={application.FederationDonationId}&applicationId={application.Id}");
        }
    }

    private static DocumentOriginContextResponse BuildFallbackOriginContext(StoredDocument document)
    {
        return new DocumentOriginContextResponse(
            document.ModuleCode,
            ResolveModuleName(document.ModuleCode),
            document.EntityType,
            document.EntityId,
            $"{document.EntityType} {document.EntityId.ToString("N")[..8]}",
            document.OriginalFileName,
            ResolveModuleRouteHint(document.ModuleCode));
    }

    private static string ResolveModuleRouteHint(string moduleCode)
    {
        return moduleCode switch
        {
            "MARKETS" => "/markets",
            "DONATARIAS" => "/donatarias",
            "FEDERATION" => "/federation",
            _ => DocumentsNavigationPath
        };
    }

    private static IReadOnlyList<string> ResolveAllowedModuleCodes(ClaimsPrincipal user)
    {
        var permissions = user
            .FindAll(PlatformPermissionCodes.ClaimType)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);

        return ModuleAccess.Values
            .Where(access => permissions.Contains(access.RequiredPermission))
            .Select(access => access.ModuleCode)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool HasReadAccess(ClaimsPrincipal user, string moduleCode)
    {
        if (!ModuleAccess.TryGetValue(moduleCode, out var access))
        {
            return false;
        }

        return user.HasClaim(PlatformPermissionCodes.ClaimType, access.RequiredPermission);
    }

    private static string ResolveModuleName(string moduleCode)
    {
        return ModuleAccess.TryGetValue(moduleCode, out var access)
            ? access.ModuleName
            : moduleCode;
    }

    private static IQueryable<StoredDocument> ApplyRetentionPolicyFilter(
        IQueryable<StoredDocument> query,
        string retentionPolicyCode)
    {
        return query.Where(document =>
            (document.RetentionOverridePolicyCode ?? document.RetentionPolicyCode) == retentionPolicyCode);
    }

    private static IQueryable<StoredDocument> ApplyRetentionStatusFilter(
        IQueryable<StoredDocument> query,
        string retentionStatusCode,
        DateTimeOffset referenceUtc)
    {
        var reviewDueThresholdUtc = referenceUtc.Add(DocumentRetentionPolicyCodes.ReviewDueWindow);
        return retentionStatusCode switch
        {
            DocumentRetentionPolicyCodes.ExpiredRetention => query.Where(document =>
                (document.RetentionOverrideUntilUtc ?? document.RetentionUntilUtc) < referenceUtc),
            DocumentRetentionPolicyCodes.ReviewDue => query.Where(document =>
                (document.RetentionOverrideUntilUtc ?? document.RetentionUntilUtc) >= referenceUtc
                && (document.RetentionOverrideUntilUtc ?? document.RetentionUntilUtc) <= reviewDueThresholdUtc),
            DocumentRetentionPolicyCodes.ActiveRetention => query.Where(document =>
                (document.RetentionOverrideUntilUtc ?? document.RetentionUntilUtc) > reviewDueThresholdUtc),
            _ => query
        };
    }

    private static IQueryable<StoredDocument> ApplyRetentionReviewQueueFilter(
        IQueryable<StoredDocument> query,
        string? retentionReviewStatusCode,
        DateTimeOffset referenceUtc)
    {
        var reviewDueThresholdUtc = referenceUtc.Add(DocumentRetentionPolicyCodes.ReviewDueWindow);
        query = query.Where(document => !document.IsAdministrativeHold);

        if (retentionReviewStatusCode is null)
        {
            return query.Where(document =>
                document.RetentionReviewStatusCode != DocumentRetentionReviewStatusCodes.Completed
                && (
                    (document.RetentionReviewStatusCode == DocumentRetentionReviewStatusCodes.Deferred
                     && document.NextRetentionReviewUtc != null
                     && document.NextRetentionReviewUtc <= referenceUtc)
                    || (document.RetentionReviewStatusCode != DocumentRetentionReviewStatusCodes.Deferred
                        && (document.RetentionOverrideUntilUtc ?? document.RetentionUntilUtc) <= reviewDueThresholdUtc)));
        }

        var statusFilteredQuery = query.Where(document => document.RetentionReviewStatusCode == retentionReviewStatusCode);
        if (string.Equals(retentionReviewStatusCode, DocumentRetentionReviewStatusCodes.Deferred, StringComparison.Ordinal))
        {
            return statusFilteredQuery;
        }

        return statusFilteredQuery.Where(document =>
            (document.RetentionOverrideUntilUtc ?? document.RetentionUntilUtc) <= reviewDueThresholdUtc);
    }

    private static bool IsSupportedStatusCode(string statusCode)
    {
        return string.Equals(statusCode, StoredDocument.ActiveStatusCode, StringComparison.Ordinal)
               || string.Equals(statusCode, StoredDocument.ArchivedStatusCode, StringComparison.Ordinal);
    }

    private static Dictionary<string, string[]> ValidateMetadataRequest(
        UpdateStoredDocumentMetadataRequest? request,
        StoredDocument document)
    {
        var errors = new Dictionary<string, string[]>();
        if (request is null)
        {
            errors["body"] = ["Metadata payload is required."];
            return errors;
        }

        var normalizedDocumentClassCode = NormalizeOptionalCode(request.DocumentClassCode);
        if (normalizedDocumentClassCode is null || !DocumentClassCodes.Supported.Contains(normalizedDocumentClassCode))
        {
            errors["documentClassCode"] = ["DocumentClassCode must be CERTIFICATE, SIGNED_DOCUMENT, SUPPORTING_DOCUMENT, PHOTO_EVIDENCE, VIDEO_EVIDENCE or OTHER."];
        }

        if (request.BusinessPurpose is { Length: > 500 })
        {
            errors["businessPurpose"] = ["BusinessPurpose must be 500 characters or fewer."];
        }

        if (request.ClassificationNotes is { Length: > 500 })
        {
            errors["classificationNotes"] = ["ClassificationNotes must be 500 characters or fewer."];
        }

        if (document.IsArchived && request.IsPrimaryDocument)
        {
            errors["isPrimaryDocument"] = ["Archived documents cannot be marked as primary."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateRetentionReviewRequest(
        UpdateStoredDocumentRetentionReviewRequest? request,
        StoredDocument document,
        DateTimeOffset referenceUtc)
    {
        var errors = new Dictionary<string, string[]>();
        if (request is null)
        {
            errors["body"] = ["Retention review payload is required."];
            return errors;
        }

        var normalizedReviewStatusCode = NormalizeOptionalCode(request.RetentionReviewStatusCode);
        if (!string.Equals(normalizedReviewStatusCode, DocumentRetentionReviewStatusCodes.Completed, StringComparison.Ordinal)
            && !string.Equals(normalizedReviewStatusCode, DocumentRetentionReviewStatusCodes.Deferred, StringComparison.Ordinal))
        {
            errors["retentionReviewStatusCode"] = ["RetentionReviewStatusCode must be REVIEW_COMPLETED or REVIEW_DEFERRED."];
        }

        if (request.RetentionReviewNotes is { Length: > 500 })
        {
            errors["retentionReviewNotes"] = ["RetentionReviewNotes must be 500 characters or fewer."];
        }

        if (document.IsAdministrativeHold)
        {
            errors["administrativeHold"] = ["Administrative hold is active; clear the hold before operational retention review."];
        }

        var retentionStatusCode = document.ResolveRetentionStatusCode(referenceUtc);
        if (string.Equals(retentionStatusCode, DocumentRetentionPolicyCodes.ActiveRetention, StringComparison.Ordinal))
        {
            errors["retentionStatusCode"] = ["Only REVIEW_DUE or EXPIRED_RETENTION documents can be reviewed operationally."];
        }

        if (normalizedReviewStatusCode == DocumentRetentionReviewStatusCodes.Deferred)
        {
            if (request.NextRetentionReviewUtc is null)
            {
                errors["nextRetentionReviewUtc"] = ["NextRetentionReviewUtc is required when deferring retention review."];
            }
            else if (request.NextRetentionReviewUtc.Value <= referenceUtc)
            {
                errors["nextRetentionReviewUtc"] = ["NextRetentionReviewUtc must be in the future."];
            }
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateHoldRequest(SetStoredDocumentHoldRequest? request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request is null)
        {
            errors["body"] = ["Hold payload is required."];
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            errors["reason"] = ["Reason is required."];
        }
        else if (request.Reason.Length > 500)
        {
            errors["reason"] = ["Reason must be 500 characters or fewer."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateRetentionOverrideRequest(
        UpdateStoredDocumentRetentionOverrideRequest? request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request is null)
        {
            errors["body"] = ["Retention override payload is required."];
            return errors;
        }

        var normalizedPolicyCode = NormalizeOptionalCode(request.RetentionOverridePolicyCode);
        if (normalizedPolicyCode is not null
            && !DocumentRetentionPolicyCodes.SupportedPolicies.Contains(normalizedPolicyCode))
        {
            errors["retentionOverridePolicyCode"] = ["RetentionOverridePolicyCode must be CERTIFICATE_REVIEW, SIGNED_LONG_TERM, EVIDENCE_MEDIUM_TERM or GENERIC_REVIEW."];
        }

        if (normalizedPolicyCode is null && request.RetentionOverrideUntilUtc is null)
        {
            errors["retentionOverride"] = ["At least RetentionOverridePolicyCode or RetentionOverrideUntilUtc is required."];
        }

        if (string.IsNullOrWhiteSpace(request.RetentionOverrideReason))
        {
            errors["retentionOverrideReason"] = ["RetentionOverrideReason is required."];
        }
        else if (request.RetentionOverrideReason.Length > 500)
        {
            errors["retentionOverrideReason"] = ["RetentionOverrideReason must be 500 characters or fewer."];
        }

        return errors;
    }

    private static AuditEvent CreateLifecycleAuditEvent(
        ClaimsPrincipal principal,
        StoredDocument document,
        string actionType,
        string title,
        string detail,
        string? reason)
    {
        var actorUserIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var actorUserName = principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? "unknown";

        return AuditEventSupport.CreateAuditEvent(
            document.ModuleCode,
            ResolveModuleName(document.ModuleCode),
            StoredDocumentEntityType,
            document.Id,
            actionType,
            title,
            $"{detail} Actor: {actorUserName}.",
            document.StatusCode,
            document.OriginalFileName,
            DocumentsNavigationPath,
            metadata: new
            {
                actorUserId = actorUserIdClaim,
                actorUserName,
                documentId = document.Id,
                document.ModuleCode,
                document.DocumentAreaCode,
                document.EntityType,
                document.EntityId,
                document.OriginalFileName,
                document.DocumentClassCode,
                document.BusinessPurpose,
                document.IsPrimaryDocument,
                document.ClassificationNotes,
                document.RetentionPolicyCode,
                document.RetentionUntilUtc,
                retentionBaselinePolicyCode = document.RetentionPolicyCode,
                retentionBaselineUntilUtc = document.RetentionUntilUtc,
                retentionEffectivePolicyCode = document.ResolveEffectiveRetentionPolicyCode(),
                retentionEffectiveUntilUtc = document.ResolveEffectiveRetentionUntilUtc(),
                document.HasRetentionOverride,
                document.RetentionOverridePolicyCode,
                document.RetentionOverrideUntilUtc,
                document.RetentionOverrideReason,
                retentionStatusCode = document.ResolveRetentionStatusCode(DateTimeOffset.UtcNow),
                document.RetentionReviewStatusCode,
                document.LastRetentionReviewUtc,
                document.NextRetentionReviewUtc,
                document.RetentionReviewNotes,
                document.StatusCode,
                document.ArchivedUtc,
                reason
            });
    }

    private static AuditEvent CreateMetadataAuditEvent(
        ClaimsPrincipal principal,
        StoredDocument document,
        string actionType,
        string title,
        string detail,
        IReadOnlyList<Guid> demotedPrimaryIds)
    {
        var actorUserIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var actorUserName = principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? "unknown";

        return AuditEventSupport.CreateAuditEvent(
            document.ModuleCode,
            ResolveModuleName(document.ModuleCode),
            StoredDocumentEntityType,
            document.Id,
            actionType,
            title,
            $"{detail} Actor: {actorUserName}.",
            document.StatusCode,
            document.OriginalFileName,
            DocumentsNavigationPath,
            metadata: new
            {
                actorUserId = actorUserIdClaim,
                actorUserName,
                documentId = document.Id,
                document.ModuleCode,
                document.DocumentAreaCode,
                document.EntityType,
                document.EntityId,
                document.OriginalFileName,
                document.DocumentClassCode,
                document.BusinessPurpose,
                document.IsPrimaryDocument,
                document.ClassificationNotes,
                document.RetentionPolicyCode,
                document.RetentionUntilUtc,
                retentionBaselinePolicyCode = document.RetentionPolicyCode,
                retentionBaselineUntilUtc = document.RetentionUntilUtc,
                retentionEffectivePolicyCode = document.ResolveEffectiveRetentionPolicyCode(),
                retentionEffectiveUntilUtc = document.ResolveEffectiveRetentionUntilUtc(),
                document.HasRetentionOverride,
                document.RetentionOverridePolicyCode,
                document.RetentionOverrideUntilUtc,
                document.RetentionOverrideReason,
                retentionStatusCode = document.ResolveRetentionStatusCode(DateTimeOffset.UtcNow),
                document.RetentionReviewStatusCode,
                document.LastRetentionReviewUtc,
                document.NextRetentionReviewUtc,
                document.RetentionReviewNotes,
                demotedPrimaryIds
            });
    }

    private static AuditEvent CreateRetentionReviewAuditEvent(
        ClaimsPrincipal principal,
        StoredDocument document,
        string actionType,
        string title,
        string detail)
    {
        var actorUserIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var actorUserName = principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? "unknown";

        return AuditEventSupport.CreateAuditEvent(
            document.ModuleCode,
            ResolveModuleName(document.ModuleCode),
            StoredDocumentEntityType,
            document.Id,
            actionType,
            title,
            $"{detail} Actor: {actorUserName}.",
            document.StatusCode,
            document.OriginalFileName,
            DocumentsNavigationPath,
            metadata: new
            {
                actorUserId = actorUserIdClaim,
                actorUserName,
                documentId = document.Id,
                document.ModuleCode,
                document.DocumentAreaCode,
                document.EntityType,
                document.EntityId,
                document.OriginalFileName,
                document.RetentionPolicyCode,
                document.RetentionUntilUtc,
                retentionBaselinePolicyCode = document.RetentionPolicyCode,
                retentionBaselineUntilUtc = document.RetentionUntilUtc,
                retentionEffectivePolicyCode = document.ResolveEffectiveRetentionPolicyCode(),
                retentionEffectiveUntilUtc = document.ResolveEffectiveRetentionUntilUtc(),
                document.HasRetentionOverride,
                document.RetentionOverridePolicyCode,
                document.RetentionOverrideUntilUtc,
                document.RetentionOverrideReason,
                retentionStatusCode = document.ResolveRetentionStatusCode(DateTimeOffset.UtcNow),
                document.RetentionReviewStatusCode,
                document.LastRetentionReviewUtc,
                document.NextRetentionReviewUtc,
                document.RetentionReviewNotes
            });
    }

    private static AuditEvent CreateRetentionOverrideAuditEvent(
        ClaimsPrincipal principal,
        StoredDocument document,
        string actionType,
        string title,
        string detail)
    {
        var actorUserIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var actorUserName = principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? "unknown";

        return AuditEventSupport.CreateAuditEvent(
            document.ModuleCode,
            ResolveModuleName(document.ModuleCode),
            StoredDocumentEntityType,
            document.Id,
            actionType,
            title,
            $"{detail} Actor: {actorUserName}.",
            document.StatusCode,
            document.OriginalFileName,
            DocumentsNavigationPath,
            metadata: new
            {
                actorUserId = actorUserIdClaim,
                actorUserName,
                documentId = document.Id,
                document.ModuleCode,
                document.DocumentAreaCode,
                document.EntityType,
                document.EntityId,
                document.OriginalFileName,
                retentionBaselinePolicyCode = document.RetentionPolicyCode,
                retentionBaselineUntilUtc = document.RetentionUntilUtc,
                retentionEffectivePolicyCode = document.ResolveEffectiveRetentionPolicyCode(),
                retentionEffectiveUntilUtc = document.ResolveEffectiveRetentionUntilUtc(),
                document.HasRetentionOverride,
                document.RetentionOverridePolicyCode,
                document.RetentionOverrideUntilUtc,
                document.RetentionOverrideReason,
                retentionStatusCode = document.ResolveRetentionStatusCode(DateTimeOffset.UtcNow),
                document.RetentionReviewStatusCode,
                document.LastRetentionReviewUtc,
                document.NextRetentionReviewUtc,
                document.RetentionReviewNotes
            });
    }

    private static AuditEvent CreateHoldAuditEvent(
        ClaimsPrincipal principal,
        StoredDocument document,
        string actionType,
        string title,
        string detail,
        string? reason)
    {
        var actorUserIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var actorUserName = principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? "unknown";

        return AuditEventSupport.CreateAuditEvent(
            document.ModuleCode,
            ResolveModuleName(document.ModuleCode),
            StoredDocumentEntityType,
            document.Id,
            actionType,
            title,
            $"{detail} Actor: {actorUserName}.",
            document.StatusCode,
            document.OriginalFileName,
            DocumentsNavigationPath,
            metadata: new
            {
                actorUserId = actorUserIdClaim,
                actorUserName,
                documentId = document.Id,
                document.ModuleCode,
                document.DocumentAreaCode,
                document.EntityType,
                document.EntityId,
                document.OriginalFileName,
                document.IsAdministrativeHold,
                document.HoldReason,
                document.HoldPlacedUtc,
                document.HoldReleasedUtc,
                document.HoldPlacedBy,
                reason,
                retentionBaselinePolicyCode = document.RetentionPolicyCode,
                retentionBaselineUntilUtc = document.RetentionUntilUtc,
                retentionEffectivePolicyCode = document.ResolveEffectiveRetentionPolicyCode(),
                retentionEffectiveUntilUtc = document.ResolveEffectiveRetentionUntilUtc(),
                retentionStatusCode = document.ResolveRetentionStatusCode(DateTimeOffset.UtcNow),
                document.RetentionReviewStatusCode,
                document.StatusCode
            });
    }

    private static ProblemHttpResult DocumentAccessDenied()
    {
        return TypedResults.Problem(
            statusCode: StatusCodes.Status403Forbidden,
            title: "Acceso documental no autorizado",
            detail: "El usuario no tiene permiso de lectura para el modulo documental solicitado.");
    }

    private static string? NormalizeOptionalCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }

    private static int NormalizeSkip(int? skip)
    {
        return Math.Max(0, skip ?? 0);
    }

    private static int NormalizeTake(int? take)
    {
        return Math.Clamp(take ?? DefaultTake, 1, MaxTake);
    }

    private sealed record ModuleDocumentAccess(
        string ModuleCode,
        string ModuleName,
        string RequiredPermission);

    private sealed record MarketTenantCompletenessContext(
        Guid TenantId,
        Guid MarketId,
        string TenantName,
        string CertificateNumber,
        string? MarketName);

    private sealed record DonationApplicationCompletenessContext(
        Guid ApplicationId,
        Guid DonationId,
        string BeneficiaryName,
        string? DonorEntityName,
        string? Reference);

    private sealed record FederationApplicationCompletenessContext(
        Guid ApplicationId,
        Guid FederationDonationId,
        string BeneficiaryOrDestinationName,
        string? DonorName,
        string? Reference);
}
