using System.Globalization;
using System.Security.Claims;
using System.Text;
using FMCPA.Api.Auth;
using FMCPA.Api.Contracts.Closeout;
using FMCPA.Api.Contracts.Documents;
using FMCPA.Api.Contracts.Operations;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class OperationsEndpoints
{
    private const string CategoryBusiness = "BUSINESS";
    private const string CategoryDocuments = "DOCUMENTS";
    private const string CategorySecurity = "SECURITY";
    private const string SeverityHigh = "HIGH";
    private const string SeverityMedium = "MEDIUM";
    private const string SeverityLow = "LOW";
    private const string ActionKindView = "VIEW";
    private const string ActionKindRemediate = "REMEDIATE";
    private const string ActionKindReview = "REVIEW";
    private const string ActionKindSecurityAdmin = "SECURITY_ADMIN";
    private const string ActionKindDocumentsQueue = "DOCUMENTS_QUEUE";
    private const string QuickActionUnlockUser = "UNLOCK_USER";
    private const string DocumentWorkItemCompletenessPending = "COMPLETENESS_PENDING";
    private const string DocumentWorkItemIntegrityIssue = "DOCUMENT_INTEGRITY_ISSUE";
    private const string DocumentWorkItemRetentionReview = "RETENTION_REVIEW";
    private const string DocumentsNavigationPath = "/documents";
    private const string DocumentsWorkQueueNavigationPath = "/documents/work-queue";
    private const string DocumentsReviewNavigationPath = "/documents/review";
    private const string SecurityNavigationPath = "/admin/security";
    private const string TimeWindowAll = "ALL";
    private const string TimeWindowCustom = "CUSTOM";
    private const string TimeWindowToday = "TODAY";
    private const string TimeWindowLast7Days = "LAST_7_DAYS";
    private const string TimeWindowNext30Days = "NEXT_30_DAYS";
    private const string CsvContentType = "text/csv; charset=utf-8";
    private const int DefaultTake = 50;
    private const int MaximumTake = 200;

    private static readonly IReadOnlyDictionary<string, SurfaceAccess> BusinessSurfaceAccess =
        new Dictionary<string, SurfaceAccess>(StringComparer.Ordinal)
        {
            ["MARKETS"] = new("MARKETS", "Mercados", PlatformPermissionCodes.MarketsRead, "/markets"),
            ["DONATARIAS"] = new("DONATARIAS", "Donatarias", PlatformPermissionCodes.DonationsRead, "/donatarias"),
            ["FINANCIALS"] = new("FINANCIALS", "Financieras", PlatformPermissionCodes.FinancialsRead, "/financials"),
            ["FEDERATION"] = new("FEDERATION", "Federacion", PlatformPermissionCodes.FederationRead, "/federation")
        };

    private static readonly IReadOnlyDictionary<string, SurfaceAccess> DocumentSurfaceAccess =
        new Dictionary<string, SurfaceAccess>(StringComparer.Ordinal)
        {
            ["MARKETS"] = BusinessSurfaceAccess["MARKETS"],
            ["DONATARIAS"] = BusinessSurfaceAccess["DONATARIAS"],
            ["FEDERATION"] = BusinessSurfaceAccess["FEDERATION"]
        };

    public static IEndpointRouteBuilder MapOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/operations")
            .WithTags("Operations")
            .RequireDashboardReadAccess();

        group.MapGet(
            "/summary",
            async (
                string? timeWindowCode,
                DateTimeOffset? fromUtc,
                DateTimeOffset? toUtc,
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await BuildOperationsSummaryAsync(
                    timeWindowCode,
                    fromUtc,
                    toUtc,
                    dbContext,
                    documentBinaryStore,
                    httpContext.User,
                    cancellationToken);
                if (result.Errors.Count > 0)
                {
                    return Results.ValidationProblem(result.Errors);
                }

                return Results.Ok(result.Summary);
            });

        group.MapGet(
            "/summary/export",
            async (
                string? timeWindowCode,
                DateTimeOffset? fromUtc,
                DateTimeOffset? toUtc,
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await BuildOperationsSummaryAsync(
                    timeWindowCode,
                    fromUtc,
                    toUtc,
                    dbContext,
                    documentBinaryStore,
                    httpContext.User,
                    cancellationToken);
                if (result.Errors.Count > 0)
                {
                    return Results.ValidationProblem(result.Errors);
                }

                return BuildCsvResponse(
                    httpContext,
                    "operations-summary",
                    BuildOperationsSummaryExportCsv(result.Summary!));
            });

        group.MapGet(
            "/work-queue",
            async (
                string? categoryCode,
                string? severityCode,
                string? timeWindowCode,
                DateTimeOffset? fromUtc,
                DateTimeOffset? toUtc,
                int? skip,
                int? take,
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await BuildOperationsWorkQueueAsync(
                    categoryCode,
                    severityCode,
                    timeWindowCode,
                    fromUtc,
                    toUtc,
                    skip,
                    take,
                    dbContext,
                    documentBinaryStore,
                    httpContext.User,
                    cancellationToken);
                if (result.Errors.Count > 0)
                {
                    return Results.ValidationProblem(result.Errors);
                }

                return Results.Ok(result.WorkQueue);
            });

        group.MapGet(
            "/work-queue/export",
            async (
                string? categoryCode,
                string? severityCode,
                string? timeWindowCode,
                DateTimeOffset? fromUtc,
                DateTimeOffset? toUtc,
                int? skip,
                int? take,
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await BuildOperationsWorkQueueAsync(
                    categoryCode,
                    severityCode,
                    timeWindowCode,
                    fromUtc,
                    toUtc,
                    skip,
                    take,
                    dbContext,
                    documentBinaryStore,
                    httpContext.User,
                    cancellationToken);
                if (result.Errors.Count > 0)
                {
                    return Results.ValidationProblem(result.Errors);
                }

                return BuildCsvResponse(
                    httpContext,
                    "operations-work-queue",
                    BuildOperationsWorkQueueExportCsv(result.WorkQueue!));
            });

        return app;
    }

    private static async Task<OperationsSummaryBuildResult> BuildOperationsSummaryAsync(
        string? timeWindowCode,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        PlatformDbContext dbContext,
        IDocumentBinaryStore documentBinaryStore,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var timeWindow = ResolveTimeWindow(timeWindowCode, fromUtc, toUtc);
        if (timeWindow.Errors.Count > 0)
        {
            return new OperationsSummaryBuildResult(null, timeWindow.Errors);
        }

        var allowedDocumentModuleCodes = ResolveAllowedDocumentModuleCodes(user);
        var dashboardAlerts = await CloseoutEndpoints.BuildDashboardAlertsAsync(dbContext, cancellationToken);
        var businessItems = ApplyTimeWindow(
            BuildBusinessWorkQueueItems(dashboardAlerts, user),
            timeWindow.Filter).ToArray();
        var documentWorkItems = allowedDocumentModuleCodes.Count == 0
            ? []
            : await DocumentCatalogEndpoints.BuildDocumentWorkQueueAsync(
                dbContext,
                documentBinaryStore,
                allowedDocumentModuleCodes,
                cancellationToken);
        var filteredDocumentWorkItems = ApplyDocumentTimeWindow(documentWorkItems, timeWindow.Filter).ToArray();
        var dashboardSummary = timeWindow.Filter.IsApplied
            ? null
            : await CloseoutEndpoints.BuildDashboardSummaryAsync(dbContext, cancellationToken);
        var documentSummary = allowedDocumentModuleCodes.Count == 0
            ? null
            : timeWindow.Filter.IsApplied
                ? BuildDocumentSummaryFromWorkItems(filteredDocumentWorkItems, allowedDocumentModuleCodes)
                : await DocumentCatalogEndpoints.BuildDocumentSummaryAsync(
                    dbContext,
                    documentBinaryStore,
                    allowedDocumentModuleCodes,
                    cancellationToken);
        var securitySummary = HasPermission(user, PlatformPermissionCodes.UsersAdmin)
            ? await BuildSecuritySummaryAsync(
                dbContext,
                timeWindow.Filter,
                cancellationToken)
            : null;

        return new OperationsSummaryBuildResult(
            new OperationsSummaryResponse(
                DateTimeOffset.UtcNow,
                BuildTimeWindowResponse(timeWindow.Filter),
                BuildSeverityConvention(),
                dashboardSummary is null
                    ? BuildBusinessKpisFromWorkItems(businessItems, user)
                    : BuildBusinessKpis(dashboardSummary, user),
                documentSummary,
                securitySummary),
            new Dictionary<string, string[]>());
    }

    private static async Task<OperationsWorkQueueBuildResult> BuildOperationsWorkQueueAsync(
        string? categoryCode,
        string? severityCode,
        string? timeWindowCode,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int? skip,
        int? take,
        PlatformDbContext dbContext,
        IDocumentBinaryStore documentBinaryStore,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var normalizedCategoryCode = NormalizeOptionalCode(categoryCode);
        var normalizedSeverityCode = NormalizeOptionalCode(severityCode);
        var normalizedSkip = NormalizeSkip(skip);
        var normalizedTake = NormalizeTake(take);
        var timeWindow = ResolveTimeWindow(timeWindowCode, fromUtc, toUtc);
        var errors = new Dictionary<string, string[]>();

        foreach (var error in timeWindow.Errors)
        {
            errors[error.Key] = error.Value;
        }

        if (normalizedCategoryCode is not null
            && normalizedCategoryCode is not CategoryBusiness and not CategoryDocuments and not CategorySecurity)
        {
            errors["categoryCode"] = ["CategoryCode must be BUSINESS, DOCUMENTS or SECURITY."];
        }

        if (normalizedSeverityCode is not null
            && normalizedSeverityCode is not SeverityHigh and not SeverityMedium and not SeverityLow)
        {
            errors["severityCode"] = ["SeverityCode must be HIGH, MEDIUM or LOW."];
        }

        if (errors.Count > 0)
        {
            return new OperationsWorkQueueBuildResult(null, errors);
        }

        var items = new List<OperationsWorkQueueItemResponse>();

        if (normalizedCategoryCode is null or CategoryBusiness)
        {
            var alerts = await CloseoutEndpoints.BuildDashboardAlertsAsync(dbContext, cancellationToken);
            items.AddRange(BuildBusinessWorkQueueItems(alerts, user));
        }

        if (normalizedCategoryCode is null or CategoryDocuments)
        {
            var allowedDocumentModuleCodes = ResolveAllowedDocumentModuleCodes(user);
            if (allowedDocumentModuleCodes.Count > 0)
            {
                var documentWorkItems = await DocumentCatalogEndpoints.BuildDocumentWorkQueueAsync(
                    dbContext,
                    documentBinaryStore,
                    allowedDocumentModuleCodes,
                    cancellationToken);
                items.AddRange(documentWorkItems.Select(item => BuildDocumentWorkQueueItem(item, user)));
            }
        }

        if ((normalizedCategoryCode is null or CategorySecurity)
            && HasPermission(user, PlatformPermissionCodes.UsersAdmin))
        {
            var securitySummary = await BuildSecuritySummaryAsync(
                dbContext,
                timeWindow.Filter,
                cancellationToken);
            var lockedUsers = await SecurityOperationsEndpoints.BuildLockedUsersAsync(dbContext, cancellationToken);
            items.AddRange(BuildSecurityWorkQueueItems(securitySummary, lockedUsers));
        }

        items = ApplyTimeWindow(items, timeWindow.Filter).ToList();

        if (normalizedSeverityCode is not null)
        {
            items = items
                .Where(item => item.SeverityCode == normalizedSeverityCode)
                .ToList();
        }

        var orderedItems = items
            .OrderBy(item => ResolveSeveritySortOrder(item.SeverityCode))
            .ThenBy(item => item.CategoryCode, StringComparer.Ordinal)
            .ThenBy(item => item.ModuleCode, StringComparer.Ordinal)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var pageItems = orderedItems
            .Skip(normalizedSkip)
            .Take(normalizedTake)
            .ToArray();

        return new OperationsWorkQueueBuildResult(
            new OperationsWorkQueueResponse(
                orderedItems.Length,
                pageItems.Length,
                normalizedSkip,
                normalizedTake,
                BuildTimeWindowResponse(timeWindow.Filter),
                BuildSeverityConvention(),
                pageItems),
            new Dictionary<string, string[]>());
    }

    private static IReadOnlyList<OperationsKpiResponse> BuildBusinessKpis(
        DashboardSummaryResponse summary,
        ClaimsPrincipal user)
    {
        var kpis = new List<OperationsKpiResponse>();

        if (HasPermission(user, PlatformPermissionCodes.MarketsRead))
        {
            kpis.Add(new(
                "MARKETS_ACTIVE",
                CategoryBusiness,
                "Operacion",
                "MARKETS",
                "Mercados",
                "Mercados activos",
                summary.Markets.ActiveMarkets,
                null,
                SeverityLow,
                "/markets"));
            kpis.Add(new(
                "MARKET_CERTIFICATE_ALERTS",
                CategoryBusiness,
                "Operacion",
                "MARKETS",
                "Mercados",
                "Cedulas por vencer o vencidas",
                summary.Markets.CertificateAlertCount,
                null,
                summary.Markets.CertificateAlertCount > 0 ? SeverityMedium : SeverityLow,
                "/markets"));
        }

        if (HasPermission(user, PlatformPermissionCodes.DonationsRead))
        {
            kpis.Add(new(
                "DONATIONS_PENDING_APPLICATION",
                CategoryBusiness,
                "Operacion",
                "DONATARIAS",
                "Donatarias",
                "Donaciones no aplicadas o parciales",
                summary.Donations.ActiveAlertCount,
                summary.Donations.BaseAmountTotal - summary.Donations.AppliedAmountTotal,
                summary.Donations.ActiveAlertCount > 0 ? SeverityHigh : SeverityLow,
                "/donatarias"));
        }

        if (HasPermission(user, PlatformPermissionCodes.FinancialsRead))
        {
            kpis.Add(new(
                "FINANCIAL_PERMITS_EXPIRED",
                CategoryBusiness,
                "Operacion",
                "FINANCIALS",
                "Financieras",
                "Oficios vencidos",
                summary.Financials.ExpiredCount,
                null,
                summary.Financials.ExpiredCount > 0 ? SeverityHigh : SeverityLow,
                "/financials"));
            kpis.Add(new(
                "FINANCIAL_PERMITS_DUE_SOON",
                CategoryBusiness,
                "Operacion",
                "FINANCIALS",
                "Financieras",
                "Oficios por vencer o renovar",
                summary.Financials.DueSoonCount + summary.Financials.RenewalCount,
                null,
                summary.Financials.DueSoonCount + summary.Financials.RenewalCount > 0 ? SeverityMedium : SeverityLow,
                "/financials"));
        }

        if (HasPermission(user, PlatformPermissionCodes.FederationRead))
        {
            kpis.Add(new(
                "FEDERATION_FOLLOW_UP",
                CategoryBusiness,
                "Operacion",
                "FEDERATION",
                "Federacion",
                "Gestiones o donaciones con seguimiento",
                summary.Federation.ActionAlertCount + summary.Federation.DonationAlertCount,
                null,
                summary.Federation.ActionAlertCount + summary.Federation.DonationAlertCount > 0 ? SeverityMedium : SeverityLow,
                "/federation"));
        }

        return kpis;
    }

    private static IReadOnlyList<OperationsKpiResponse> BuildBusinessKpisFromWorkItems(
        IReadOnlyList<OperationsWorkQueueItemResponse> businessItems,
        ClaimsPrincipal user)
    {
        var kpis = new List<OperationsKpiResponse>();

        if (HasPermission(user, PlatformPermissionCodes.MarketsRead))
        {
            var count = businessItems.Count(item => item.ModuleCode == "MARKETS");
            kpis.Add(new(
                "MARKET_CERTIFICATE_ALERTS",
                CategoryBusiness,
                "Operacion",
                "MARKETS",
                "Mercados",
                "Cedulas en ventana",
                count,
                null,
                count > 0 ? SeverityMedium : SeverityLow,
                "/markets"));
        }

        if (HasPermission(user, PlatformPermissionCodes.DonationsRead))
        {
            var count = businessItems.Count(item => item.ModuleCode == "DONATARIAS");
            kpis.Add(new(
                "DONATIONS_PENDING_APPLICATION",
                CategoryBusiness,
                "Operacion",
                "DONATARIAS",
                "Donatarias",
                "Donaciones en ventana",
                count,
                null,
                count > 0 ? SeverityHigh : SeverityLow,
                "/donatarias"));
        }

        if (HasPermission(user, PlatformPermissionCodes.FinancialsRead))
        {
            var expiredCount = businessItems.Count(item =>
                item.ModuleCode == "FINANCIALS"
                && string.Equals(item.ReasonCode, "EXPIRED", StringComparison.Ordinal));
            var followUpCount = businessItems.Count(item =>
                item.ModuleCode == "FINANCIALS"
                && !string.Equals(item.ReasonCode, "EXPIRED", StringComparison.Ordinal));

            kpis.Add(new(
                "FINANCIAL_PERMITS_EXPIRED",
                CategoryBusiness,
                "Operacion",
                "FINANCIALS",
                "Financieras",
                "Oficios vencidos en ventana",
                expiredCount,
                null,
                expiredCount > 0 ? SeverityHigh : SeverityLow,
                "/financials"));
            kpis.Add(new(
                "FINANCIAL_PERMITS_DUE_SOON",
                CategoryBusiness,
                "Operacion",
                "FINANCIALS",
                "Financieras",
                "Oficios por vencer o renovar en ventana",
                followUpCount,
                null,
                followUpCount > 0 ? SeverityMedium : SeverityLow,
                "/financials"));
        }

        if (HasPermission(user, PlatformPermissionCodes.FederationRead))
        {
            var count = businessItems.Count(item => item.ModuleCode == "FEDERATION");
            kpis.Add(new(
                "FEDERATION_FOLLOW_UP",
                CategoryBusiness,
                "Operacion",
                "FEDERATION",
                "Federacion",
                "Seguimientos en ventana",
                count,
                null,
                count > 0 ? SeverityMedium : SeverityLow,
                "/federation"));
        }

        return kpis;
    }

    private static DocumentSummaryResponse BuildDocumentSummaryFromWorkItems(
        IReadOnlyList<DocumentWorkQueueItemResponse> workItems,
        IReadOnlyList<string> allowedModuleCodes)
    {
        var documentIds = workItems
            .Where(item => item.DocumentId is not null)
            .Select(item => item.DocumentId!.Value)
            .Distinct()
            .ToArray();
        var modules = allowedModuleCodes
            .Select(moduleCode =>
            {
                var moduleWorkItems = workItems
                    .Where(item => string.Equals(item.ModuleCode, moduleCode, StringComparison.Ordinal))
                    .ToArray();

                return new DocumentSummaryModuleResponse(
                    moduleCode,
                    ResolveDocumentModuleName(moduleCode),
                    moduleWorkItems.Where(item => item.DocumentId is not null).Select(item => item.DocumentId!.Value).Distinct().Count(),
                    0,
                    0,
                    moduleWorkItems.Count(item => string.Equals(item.WorkItemType, DocumentWorkItemIntegrityIssue, StringComparison.Ordinal)),
                    moduleWorkItems.Count(item => string.Equals(item.WorkItemType, DocumentWorkItemCompletenessPending, StringComparison.Ordinal)),
                    moduleWorkItems.Count(item =>
                        string.Equals(item.WorkItemType, DocumentWorkItemRetentionReview, StringComparison.Ordinal)
                        && string.Equals(item.ReasonCode, FMCPA.Domain.Entities.Documents.DocumentRetentionPolicyCodes.ReviewDue, StringComparison.Ordinal)),
                    moduleWorkItems.Count(item =>
                        string.Equals(item.WorkItemType, DocumentWorkItemRetentionReview, StringComparison.Ordinal)
                        && string.Equals(item.ReasonCode, FMCPA.Domain.Entities.Documents.DocumentRetentionPolicyCodes.ExpiredRetention, StringComparison.Ordinal)),
                    0);
            })
            .OrderBy(item => item.ModuleCode, StringComparer.Ordinal)
            .ToArray();
        var operationalStatuses = workItems
            .Where(item => !string.IsNullOrWhiteSpace(item.DocumentOperationalStatusCode))
            .GroupBy(item => item.DocumentOperationalStatusCode!, StringComparer.Ordinal)
            .Select(group => new DocumentSummaryOperationalStatusResponse(
                group.Key,
                group.First().DocumentOperationalSeverityCode ?? SeverityLow,
                group.Count()))
            .OrderBy(item => item.DocumentOperationalStatusCode, StringComparer.Ordinal)
            .ToArray();
        var workQueueCategories = workItems
            .GroupBy(item => new { item.WorkItemType, item.ReasonCode, item.SeverityCode })
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
            documentIds.Length,
            0,
            0,
            workItems.Count(item => string.Equals(item.WorkItemType, DocumentWorkItemIntegrityIssue, StringComparison.Ordinal)),
            workItems.Count(item => string.Equals(item.WorkItemType, DocumentWorkItemCompletenessPending, StringComparison.Ordinal)),
            workItems.Count(item =>
                string.Equals(item.WorkItemType, DocumentWorkItemRetentionReview, StringComparison.Ordinal)
                && string.Equals(item.ReasonCode, FMCPA.Domain.Entities.Documents.DocumentRetentionPolicyCodes.ReviewDue, StringComparison.Ordinal)),
            workItems.Count(item =>
                string.Equals(item.WorkItemType, DocumentWorkItemRetentionReview, StringComparison.Ordinal)
                && string.Equals(item.ReasonCode, FMCPA.Domain.Entities.Documents.DocumentRetentionPolicyCodes.ExpiredRetention, StringComparison.Ordinal)),
            0,
            modules,
            [],
            operationalStatuses,
            workQueueCategories);
    }

    private static async Task<FMCPA.Api.Contracts.AdminSecurity.SecurityActivitySummaryResponse> BuildSecuritySummaryAsync(
        PlatformDbContext dbContext,
        OperationsTimeWindowFilter timeWindow,
        CancellationToken cancellationToken)
    {
        if (!timeWindow.IsApplied)
        {
            return await SecurityOperationsEndpoints.BuildSecurityActivitySummaryAsync(
                dbContext,
                hours: null,
                cancellationToken: cancellationToken);
        }

        var fromUtc = timeWindow.FromUtc!.Value;
        var toUtc = timeWindow.ToUtc!.Value;
        var recentEvents = dbContext.AuditEvents
            .AsNoTracking()
            .Where(item => item.ModuleCode == "SECURITY")
            .Where(item => item.OccurredUtc >= fromUtc && item.OccurredUtc <= toUtc);
        var lockedUsersInWindow = dbContext.ApplicationUsers
            .AsNoTracking()
            .Where(item => item.LockoutEndUtc != null
                           && item.LockoutEndUtc >= fromUtc
                           && item.LockoutEndUtc <= toUtc);

        return new FMCPA.Api.Contracts.AdminSecurity.SecurityActivitySummaryResponse(
            DateTimeOffset.UtcNow,
            fromUtc,
            await recentEvents.CountAsync(cancellationToken),
            await CountSecurityActionAsync(recentEvents, "AUTH_LOGIN_SUCCEEDED", cancellationToken),
            await CountSecurityActionAsync(recentEvents, "AUTH_LOGIN_FAILED", cancellationToken),
            await CountSecurityActionAsync(recentEvents, "AUTH_LOGIN_LOCKOUT_DENIED", cancellationToken),
            await CountSecurityActionAsync(recentEvents, "USER_TEMPORARILY_LOCKED", cancellationToken),
            await CountSecurityActionAsync(recentEvents, "USER_LOCKOUT_RESET", cancellationToken),
            await CountSecurityActionAsync(recentEvents, "USER_PASSWORD_RESET", cancellationToken),
            await CountSecurityActionAsync(recentEvents, "USER_ROLE_CHANGED", cancellationToken),
            await CountSecurityActionAsync(recentEvents, "USER_DEACTIVATED", cancellationToken),
            await CountSecurityActionAsync(recentEvents, "USER_ACTIVATED", cancellationToken),
            await lockedUsersInWindow.CountAsync(cancellationToken),
            await lockedUsersInWindow.CountAsync(item => item.AccessFailedCount > 0, cancellationToken));
    }

    private static IEnumerable<OperationsWorkQueueItemResponse> BuildBusinessWorkQueueItems(
        DashboardAlertsResponse alerts,
        ClaimsPrincipal user)
    {
        foreach (var alert in alerts.MarketCertificates
                     .Concat(alerts.Donations)
                     .Concat(alerts.FinancialPermits)
                     .Concat(alerts.FederationActions)
                     .Concat(alerts.FederationDonations))
        {
            if (!BusinessSurfaceAccess.TryGetValue(alert.ModuleCode, out var access)
                || !HasPermission(user, access.RequiredPermission))
            {
                continue;
            }

            yield return new OperationsWorkQueueItemResponse(
                $"BUSINESS_ALERT:{alert.AlertKey}",
                CategoryBusiness,
                alert.Category,
                ResolveBusinessAlertSeverity(alert.AlertState),
                alert.ModuleCode,
                alert.ModuleName,
                alert.Title,
                string.IsNullOrWhiteSpace(alert.Detail) ? alert.Subtitle : alert.Detail,
                alert.AlertState,
                NormalizeRouteHint(alert.NavigationPath, access.RouteHint),
                ActionKindRemediate,
                "Ir a remediar",
                BuildBusinessContextLabel(alert),
                null,
                null,
                alert.AlertKey,
                ResolveEntityTypeFromSourceKey(alert.AlertKey),
                ResolveEntityIdFromSourceKey(alert.AlertKey),
                null,
                alert.RelevantDate is null
                    ? null
                    : new DateTimeOffset(alert.RelevantDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)));
        }
    }

    private static OperationsWorkQueueItemResponse BuildDocumentWorkQueueItem(
        DocumentWorkQueueItemResponse item,
        ClaimsPrincipal user)
    {
        var actionKind = ResolveDocumentActionKind(item, user);

        return new OperationsWorkQueueItemResponse(
            $"DOCUMENT:{item.WorkItemKey}",
            CategoryDocuments,
            item.WorkItemType,
            item.SeverityCode,
            item.ModuleCode,
            item.ModuleName,
            item.Title,
            item.Summary,
            item.ReasonCode,
            ResolveDocumentRouteHint(item, user),
            actionKind,
            ResolveDocumentActionLabel(item, actionKind),
            item.OriginContext.DisplayName,
            null,
            null,
            item.WorkItemKey,
            item.EntityType,
            item.EntityId.ToString(),
            item.DocumentId,
            item.RelevantUtc);
    }

    private static IEnumerable<OperationsWorkQueueItemResponse> BuildSecurityWorkQueueItems(
        FMCPA.Api.Contracts.AdminSecurity.SecurityActivitySummaryResponse summary,
        IReadOnlyList<FMCPA.Api.Contracts.AdminSecurity.LockedApplicationUserResponse> lockedUsers)
    {
        foreach (var user in lockedUsers)
        {
            yield return new OperationsWorkQueueItemResponse(
                $"SECURITY_LOCKED_USER:{user.Id:N}",
                CategorySecurity,
                "LOCKED_USER",
                SeverityHigh,
                "SECURITY",
                "Seguridad",
                $"Usuario bloqueado: {user.UserName}",
                $"{user.DisplayName} acumula {user.AccessFailedCount} intentos fallidos.",
                "ACTIVE_LOCKOUT",
                SecurityNavigationPath,
                ActionKindSecurityAdmin,
                "Abrir seguridad",
                user.UserName,
                QuickActionUnlockUser,
                "Desbloquear",
                user.Id.ToString(),
                "APPLICATION_USER",
                user.Id.ToString(),
                null,
                user.LockoutEndUtc);
        }

        if (summary.LoginFailedCount > 0)
        {
            yield return new OperationsWorkQueueItemResponse(
                "SECURITY_LOGIN_FAILED_RECENT",
                CategorySecurity,
                "SECURITY_ACTIVITY",
                SeverityMedium,
                "SECURITY",
                "Seguridad",
                "Intentos de login fallidos recientes",
                $"{summary.LoginFailedCount} intentos fallidos desde {summary.SinceUtc:yyyy-MM-dd HH:mm} UTC.",
                "AUTH_LOGIN_FAILED",
                SecurityNavigationPath,
                ActionKindSecurityAdmin,
                "Abrir seguridad",
                "Eventos SECURITY recientes",
                QuickActionCode: null,
                QuickActionLabel: null,
                SourceItemKey: null,
                EntityType: null,
                EntityId: null,
                DocumentId: null,
                summary.SinceUtc);
        }

        if (summary.LockoutDeniedCount > 0)
        {
            yield return new OperationsWorkQueueItemResponse(
                "SECURITY_LOCKOUT_DENIED_RECENT",
                CategorySecurity,
                "SECURITY_ACTIVITY",
                SeverityHigh,
                "SECURITY",
                "Seguridad",
                "Accesos denegados por lockout",
                $"{summary.LockoutDeniedCount} intentos fueron bloqueados por lockout activo.",
                "AUTH_LOGIN_LOCKOUT_DENIED",
                SecurityNavigationPath,
                ActionKindSecurityAdmin,
                "Abrir seguridad",
                "Eventos SECURITY recientes",
                QuickActionCode: null,
                QuickActionLabel: null,
                SourceItemKey: null,
                EntityType: null,
                EntityId: null,
                DocumentId: null,
                summary.SinceUtc);
        }
    }

    private static string BuildOperationsSummaryExportCsv(OperationsSummaryResponse summary)
    {
        var rows = new List<string?[]>();

        foreach (var kpi in summary.BusinessKpis)
        {
            AddSummaryExportRow(
                rows,
                summary.TimeWindow.TimeWindowCode,
                CategoryBusiness,
                kpi.KpiCode,
                kpi.CategoryCode,
                kpi.ModuleCode,
                kpi.ModuleName,
                kpi.Label,
                kpi.Count,
                kpi.Amount,
                kpi.SeverityCode,
                kpi.RouteHint);
        }

        if (summary.Documents is not null)
        {
            AddSummaryExportRow(rows, summary.TimeWindow.TimeWindowCode, CategoryDocuments, "DOCUMENTS_TOTAL", CategoryDocuments, null, null, "Documentos en resumen", summary.Documents.TotalDocuments, null, SeverityLow, DocumentsNavigationPath);
            AddSummaryExportRow(rows, summary.TimeWindow.TimeWindowCode, CategoryDocuments, "DOCUMENTS_INTEGRITY_ISSUES", CategoryDocuments, null, null, "Issues de integridad", summary.Documents.IntegrityIssuesCount, null, summary.Documents.IntegrityIssuesCount > 0 ? SeverityHigh : SeverityLow, DocumentsWorkQueueNavigationPath);
            AddSummaryExportRow(rows, summary.TimeWindow.TimeWindowCode, CategoryDocuments, "DOCUMENTS_INCOMPLETE_ENTITIES", CategoryDocuments, null, null, "Entidades incompletas", summary.Documents.IncompleteEntitiesCount, null, summary.Documents.IncompleteEntitiesCount > 0 ? SeverityHigh : SeverityLow, DocumentsWorkQueueNavigationPath);
            AddSummaryExportRow(rows, summary.TimeWindow.TimeWindowCode, CategoryDocuments, "DOCUMENTS_REVIEW_DUE", CategoryDocuments, null, null, "Revision documental pendiente", summary.Documents.ReviewDueCount, null, SeverityLow, DocumentsReviewNavigationPath);
            AddSummaryExportRow(rows, summary.TimeWindow.TimeWindowCode, CategoryDocuments, "DOCUMENTS_EXPIRED_RETENTION", CategoryDocuments, null, null, "Retencion vencida", summary.Documents.ExpiredRetentionCount, null, summary.Documents.ExpiredRetentionCount > 0 ? SeverityMedium : SeverityLow, DocumentsReviewNavigationPath);
            AddSummaryExportRow(rows, summary.TimeWindow.TimeWindowCode, CategoryDocuments, "DOCUMENTS_ADMINISTRATIVE_HOLD", CategoryDocuments, null, null, "Holds administrativos", summary.Documents.AdministrativeHoldCount, null, SeverityLow, DocumentsNavigationPath);

            foreach (var module in summary.Documents.Modules)
            {
                AddSummaryExportRow(
                    rows,
                    summary.TimeWindow.TimeWindowCode,
                    CategoryDocuments,
                    "DOCUMENTS_MODULE_TOTAL",
                    CategoryDocuments,
                    module.ModuleCode,
                    module.ModuleName,
                    $"{module.ModuleName} documentos",
                    module.TotalDocuments,
                    null,
                    module.IntegrityIssuesCount + module.IncompleteEntitiesCount > 0
                        ? SeverityHigh
                        : module.ExpiredRetentionCount > 0
                            ? SeverityMedium
                            : SeverityLow,
                    DocumentsNavigationPath);
            }

            foreach (var status in summary.Documents.OperationalStatuses)
            {
                AddSummaryExportRow(
                    rows,
                    summary.TimeWindow.TimeWindowCode,
                    CategoryDocuments,
                    $"DOCUMENTS_STATUS_{status.DocumentOperationalStatusCode}",
                    CategoryDocuments,
                    null,
                    null,
                    status.DocumentOperationalStatusCode,
                    status.TotalCount,
                    null,
                    status.DocumentOperationalSeverityCode,
                    DocumentsNavigationPath);
            }

            foreach (var category in summary.Documents.WorkQueueCategories)
            {
                AddSummaryExportRow(
                    rows,
                    summary.TimeWindow.TimeWindowCode,
                    CategoryDocuments,
                    $"DOCUMENTS_QUEUE_{category.WorkItemType}_{category.ReasonCode}",
                    CategoryDocuments,
                    null,
                    null,
                    $"{category.WorkItemType} - {category.ReasonCode}",
                    category.TotalCount,
                    null,
                    category.SeverityCode,
                    DocumentsWorkQueueNavigationPath);
            }
        }

        if (summary.Security is not null)
        {
            AddSummaryExportRow(rows, summary.TimeWindow.TimeWindowCode, CategorySecurity, "SECURITY_RECENT_EVENTS", CategorySecurity, "SECURITY", "Seguridad", "Eventos SECURITY recientes", summary.Security.RecentSecurityEventCount, null, summary.Security.RecentSecurityEventCount > 0 ? SeverityMedium : SeverityLow, SecurityNavigationPath);
            AddSummaryExportRow(rows, summary.TimeWindow.TimeWindowCode, CategorySecurity, "SECURITY_LOGIN_FAILED", CategorySecurity, "SECURITY", "Seguridad", "Login failed", summary.Security.LoginFailedCount, null, summary.Security.LoginFailedCount > 0 ? SeverityMedium : SeverityLow, SecurityNavigationPath);
            AddSummaryExportRow(rows, summary.TimeWindow.TimeWindowCode, CategorySecurity, "SECURITY_LOCKOUT_DENIED", CategorySecurity, "SECURITY", "Seguridad", "Accesos denegados por lockout", summary.Security.LockoutDeniedCount, null, summary.Security.LockoutDeniedCount > 0 ? SeverityHigh : SeverityLow, SecurityNavigationPath);
            AddSummaryExportRow(rows, summary.TimeWindow.TimeWindowCode, CategorySecurity, "SECURITY_ACTIVE_LOCKED_USERS", CategorySecurity, "SECURITY", "Seguridad", "Usuarios bloqueados", summary.Security.ActiveLockedUserCount, null, summary.Security.ActiveLockedUserCount > 0 ? SeverityHigh : SeverityLow, SecurityNavigationPath);
            AddSummaryExportRow(rows, summary.TimeWindow.TimeWindowCode, CategorySecurity, "SECURITY_USERS_WITH_FAILED_ATTEMPTS", CategorySecurity, "SECURITY", "Seguridad", "Usuarios con intentos fallidos", summary.Security.UsersWithFailedAttemptsCount, null, summary.Security.UsersWithFailedAttemptsCount > 0 ? SeverityMedium : SeverityLow, SecurityNavigationPath);
        }

        return BuildCsv(
            [
                "timeWindowCode",
                "sectionCode",
                "metricCode",
                "categoryCode",
                "moduleCode",
                "moduleName",
                "label",
                "value",
                "amount",
                "severityCode",
                "routeHint"
            ],
            rows);
    }

    private static void AddSummaryExportRow(
        List<string?[]> rows,
        string timeWindowCode,
        string sectionCode,
        string metricCode,
        string categoryCode,
        string? moduleCode,
        string? moduleName,
        string label,
        int value,
        decimal? amount,
        string severityCode,
        string routeHint)
    {
        rows.Add(
        [
            timeWindowCode,
            sectionCode,
            metricCode,
            categoryCode,
            moduleCode,
            moduleName,
            label,
            value.ToString(CultureInfo.InvariantCulture),
            amount?.ToString(CultureInfo.InvariantCulture),
            severityCode,
            routeHint
        ]);
    }

    private static string BuildOperationsWorkQueueExportCsv(OperationsWorkQueueResponse workQueue)
    {
        return BuildCsv(
            [
                "timeWindowCode",
                "workItemKey",
                "categoryCode",
                "workItemType",
                "severity",
                "moduleCode",
                "moduleName",
                "title",
                "summary",
                "reasonCode",
                "actionKind",
                "actionLabel",
                "routeHint",
                "contextLabel",
                "quickActionCode",
                "quickActionLabel",
                "entityType",
                "entityId",
                "documentId",
                "relevantUtc"
            ],
            workQueue.Items.Select(item => new string?[]
            {
                workQueue.TimeWindow.TimeWindowCode,
                item.WorkItemKey,
                item.CategoryCode,
                item.TypeCode,
                item.SeverityCode,
                item.ModuleCode,
                item.ModuleName,
                item.Title,
                item.Summary,
                item.ReasonCode,
                item.ActionKind,
                item.ActionLabel,
                item.RouteHint,
                item.ContextLabel,
                item.QuickActionCode,
                item.QuickActionLabel,
                item.EntityType,
                item.EntityId,
                item.DocumentId?.ToString(),
                FormatCsvDate(item.RelevantUtc)
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

    private static string? FormatCsvDate(DateTimeOffset? value)
    {
        return value is null
            ? null
            : value.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static IEnumerable<OperationsWorkQueueItemResponse> ApplyTimeWindow(
        IEnumerable<OperationsWorkQueueItemResponse> items,
        OperationsTimeWindowFilter timeWindow)
    {
        return !timeWindow.IsApplied
            ? items
            : items.Where(item => IsInTimeWindow(item.RelevantUtc, timeWindow));
    }

    private static IEnumerable<DocumentWorkQueueItemResponse> ApplyDocumentTimeWindow(
        IEnumerable<DocumentWorkQueueItemResponse> items,
        OperationsTimeWindowFilter timeWindow)
    {
        return !timeWindow.IsApplied
            ? items
            : items.Where(item => IsInTimeWindow(item.RelevantUtc, timeWindow));
    }

    private static bool IsInTimeWindow(DateTimeOffset? relevantUtc, OperationsTimeWindowFilter timeWindow)
    {
        return !timeWindow.IsApplied
               || (relevantUtc is not null
                   && relevantUtc.Value >= timeWindow.FromUtc!.Value
                   && relevantUtc.Value <= timeWindow.ToUtc!.Value);
    }

    private static async Task<int> CountSecurityActionAsync(
        IQueryable<FMCPA.Domain.Entities.Audit.AuditEvent> query,
        string actionType,
        CancellationToken cancellationToken)
    {
        return await query.CountAsync(item => item.ActionType == actionType, cancellationToken);
    }

    private static string ResolveDocumentModuleName(string moduleCode)
    {
        return DocumentSurfaceAccess.TryGetValue(moduleCode, out var access)
            ? access.ModuleName
            : moduleCode;
    }

    private static TimeWindowResolution ResolveTimeWindow(
        string? timeWindowCode,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc)
    {
        var errors = new Dictionary<string, string[]>();
        var normalizedCode = NormalizeOptionalCode(timeWindowCode);

        if (fromUtc is not null || toUtc is not null)
        {
            if (fromUtc is null || toUtc is null)
            {
                errors["fromUtc"] = ["FromUtc and ToUtc must be provided together for a custom operations time window."];
                errors["toUtc"] = ["FromUtc and ToUtc must be provided together for a custom operations time window."];
                return new TimeWindowResolution(BuildAllTimeWindow(), errors);
            }

            var normalizedFromUtc = fromUtc.Value.ToUniversalTime();
            var normalizedToUtc = toUtc.Value.ToUniversalTime();
            if (normalizedFromUtc > normalizedToUtc)
            {
                errors["toUtc"] = ["ToUtc must be greater than or equal to FromUtc."];
                return new TimeWindowResolution(BuildAllTimeWindow(), errors);
            }

            return new TimeWindowResolution(
                new OperationsTimeWindowFilter(
                    TimeWindowCustom,
                    normalizedFromUtc,
                    normalizedToUtc,
                    true,
                    "Rango personalizado explicito."),
                errors);
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var todayStartUtc = new DateTimeOffset(nowUtc.UtcDateTime.Date, TimeSpan.Zero);

        return normalizedCode switch
        {
            null or TimeWindowAll => new TimeWindowResolution(BuildAllTimeWindow(), errors),
            TimeWindowToday => new TimeWindowResolution(
                new OperationsTimeWindowFilter(
                    TimeWindowToday,
                    todayStartUtc,
                    todayStartUtc.AddDays(1).AddTicks(-1),
                    true,
                    "Senales con fecha operativa de hoy en UTC."),
                errors),
            TimeWindowLast7Days => new TimeWindowResolution(
                new OperationsTimeWindowFilter(
                    TimeWindowLast7Days,
                    nowUtc.AddDays(-7),
                    nowUtc,
                    true,
                    "Senales con fecha operativa en los ultimos 7 dias."),
                errors),
            TimeWindowNext30Days => new TimeWindowResolution(
                new OperationsTimeWindowFilter(
                    TimeWindowNext30Days,
                    nowUtc,
                    nowUtc.AddDays(30),
                    true,
                    "Senales con fecha operativa en los proximos 30 dias."),
                errors),
            _ => new TimeWindowResolution(
                BuildAllTimeWindow(),
                new Dictionary<string, string[]>
                {
                    ["timeWindowCode"] = ["TimeWindowCode must be TODAY, LAST_7_DAYS, NEXT_30_DAYS or ALL."]
                })
        };
    }

    private static OperationsTimeWindowFilter BuildAllTimeWindow()
    {
        return new OperationsTimeWindowFilter(
            TimeWindowAll,
            null,
            null,
            false,
            "Sin filtro temporal; conserva el comportamiento global.");
    }

    private static OperationsTimeWindowResponse BuildTimeWindowResponse(OperationsTimeWindowFilter timeWindow)
    {
        return new OperationsTimeWindowResponse(
            timeWindow.TimeWindowCode,
            timeWindow.FromUtc,
            timeWindow.ToUtc,
            timeWindow.IsApplied,
            timeWindow.Description);
    }

    private static IReadOnlyList<string> ResolveAllowedDocumentModuleCodes(ClaimsPrincipal user)
    {
        return DocumentSurfaceAccess.Values
            .Where(access => HasPermission(user, access.RequiredPermission))
            .Select(access => access.ModuleCode)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool HasPermission(ClaimsPrincipal user, string permissionCode)
    {
        return user.HasClaim(PlatformPermissionCodes.ClaimType, permissionCode);
    }

    private static string ResolveBusinessAlertSeverity(string alertState)
    {
        return alertState switch
        {
            "EXPIRED" or "NOT_APPLIED" => SeverityHigh,
            "DUE_SOON" or "PARTIALLY_APPLIED" or "RENEWAL" => SeverityMedium,
            _ => SeverityLow
        };
    }

    private static string ResolveDocumentRouteHint(DocumentWorkQueueItemResponse item, ClaimsPrincipal user)
    {
        return item.WorkItemType switch
        {
            DocumentWorkItemRetentionReview when HasPermission(user, PlatformPermissionCodes.UsersAdmin) => DocumentsReviewNavigationPath,
            DocumentWorkItemRetentionReview => DocumentsWorkQueueNavigationPath,
            DocumentWorkItemIntegrityIssue => DocumentsWorkQueueNavigationPath,
            _ => NormalizeRouteHint(item.RouteHint, DocumentsNavigationPath)
        };
    }

    private static string ResolveDocumentActionKind(DocumentWorkQueueItemResponse item, ClaimsPrincipal user)
    {
        return item.WorkItemType switch
        {
            DocumentWorkItemCompletenessPending => ActionKindRemediate,
            DocumentWorkItemRetentionReview when HasPermission(user, PlatformPermissionCodes.UsersAdmin) => ActionKindReview,
            DocumentWorkItemRetentionReview => ActionKindDocumentsQueue,
            DocumentWorkItemIntegrityIssue => ActionKindDocumentsQueue,
            _ => ActionKindView
        };
    }

    private static string ResolveDocumentActionLabel(DocumentWorkQueueItemResponse item, string actionKind)
    {
        return actionKind switch
        {
            ActionKindReview => "Abrir revision",
            ActionKindDocumentsQueue => "Abrir bandeja documental",
            _ when string.Equals(item.WorkItemType, DocumentWorkItemCompletenessPending, StringComparison.Ordinal) => "Ir a remediar",
            _ => "Abrir contexto"
        };
    }

    private static string NormalizeRouteHint(string? routeHint, string fallbackRouteHint)
    {
        if (string.IsNullOrWhiteSpace(routeHint))
        {
            return fallbackRouteHint;
        }

        var normalized = routeHint.Trim();
        return normalized.StartsWith("/", StringComparison.Ordinal)
            ? normalized
            : fallbackRouteHint;
    }

    private static string BuildBusinessContextLabel(DashboardAlertItemResponse alert)
    {
        return string.IsNullOrWhiteSpace(alert.Subtitle)
            ? alert.ModuleName
            : $"{alert.ModuleName} - {alert.Subtitle}";
    }

    private static string? ResolveEntityTypeFromSourceKey(string? sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
        {
            return null;
        }

        var separatorIndex = sourceKey.IndexOf(":", StringComparison.Ordinal);
        return separatorIndex <= 0 ? null : sourceKey[..separatorIndex];
    }

    private static string? ResolveEntityIdFromSourceKey(string? sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey))
        {
            return null;
        }

        var separatorIndex = sourceKey.IndexOf(":", StringComparison.Ordinal);
        return separatorIndex < 0 || separatorIndex == sourceKey.Length - 1
            ? null
            : sourceKey[(separatorIndex + 1)..];
    }

    private static IReadOnlyList<OperationsSeverityConventionResponse> BuildSeverityConvention()
    {
        return
        [
            new(SeverityHigh, 0, "Bloqueo operativo, integridad rota, vencimiento critico o usuario bloqueado."),
            new(SeverityMedium, 1, "Atencion prioritaria sin bloqueo inmediato."),
            new(SeverityLow, 2, "Seguimiento, revision o contexto informativo accionable.")
        ];
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

    private static string? NormalizeOptionalCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    private static int NormalizeSkip(int? skip)
    {
        return Math.Max(0, skip ?? 0);
    }

    private static int NormalizeTake(int? take)
    {
        return Math.Clamp(take ?? DefaultTake, 1, MaximumTake);
    }

    private sealed record SurfaceAccess(
        string ModuleCode,
        string ModuleName,
        string RequiredPermission,
        string RouteHint);

    private sealed record TimeWindowResolution(
        OperationsTimeWindowFilter Filter,
        IReadOnlyDictionary<string, string[]> Errors);

    private sealed record OperationsSummaryBuildResult(
        OperationsSummaryResponse? Summary,
        IReadOnlyDictionary<string, string[]> Errors);

    private sealed record OperationsWorkQueueBuildResult(
        OperationsWorkQueueResponse? WorkQueue,
        IReadOnlyDictionary<string, string[]> Errors);

    private sealed record OperationsTimeWindowFilter(
        string TimeWindowCode,
        DateTimeOffset? FromUtc,
        DateTimeOffset? ToUtc,
        bool IsApplied,
        string Description);
}
