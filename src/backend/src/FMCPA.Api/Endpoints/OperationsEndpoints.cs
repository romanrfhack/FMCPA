using System.Security.Claims;
using FMCPA.Api.Auth;
using FMCPA.Api.Contracts.Closeout;
using FMCPA.Api.Contracts.Documents;
using FMCPA.Api.Contracts.Operations;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Infrastructure.Persistence;

namespace FMCPA.Api.Endpoints;

public static class OperationsEndpoints
{
    private const string CategoryBusiness = "BUSINESS";
    private const string CategoryDocuments = "DOCUMENTS";
    private const string CategorySecurity = "SECURITY";
    private const string SeverityHigh = "HIGH";
    private const string SeverityMedium = "MEDIUM";
    private const string SeverityLow = "LOW";
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
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var user = httpContext.User;
                var dashboardSummary = await CloseoutEndpoints.BuildDashboardSummaryAsync(dbContext, cancellationToken);
                var allowedDocumentModuleCodes = ResolveAllowedDocumentModuleCodes(user);
                var documentSummary = allowedDocumentModuleCodes.Count == 0
                    ? null
                    : await DocumentCatalogEndpoints.BuildDocumentSummaryAsync(
                        dbContext,
                        documentBinaryStore,
                        allowedDocumentModuleCodes,
                        cancellationToken);
                var securitySummary = HasPermission(user, PlatformPermissionCodes.UsersAdmin)
                    ? await SecurityOperationsEndpoints.BuildSecurityActivitySummaryAsync(
                        dbContext,
                        hours: null,
                        cancellationToken: cancellationToken)
                    : null;

                return Results.Ok(new OperationsSummaryResponse(
                    DateTimeOffset.UtcNow,
                    BuildSeverityConvention(),
                    BuildBusinessKpis(dashboardSummary, user),
                    documentSummary,
                    securitySummary));
            });

        group.MapGet(
            "/work-queue",
            async (
                string? categoryCode,
                string? severityCode,
                int? skip,
                int? take,
                PlatformDbContext dbContext,
                IDocumentBinaryStore documentBinaryStore,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var normalizedCategoryCode = NormalizeOptionalCode(categoryCode);
                var normalizedSeverityCode = NormalizeOptionalCode(severityCode);
                var normalizedSkip = NormalizeSkip(skip);
                var normalizedTake = NormalizeTake(take);
                var errors = new Dictionary<string, string[]>();

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
                    return Results.ValidationProblem(errors);
                }

                var user = httpContext.User;
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
                        items.AddRange(documentWorkItems.Select(BuildDocumentWorkQueueItem));
                    }
                }

                if ((normalizedCategoryCode is null or CategorySecurity)
                    && HasPermission(user, PlatformPermissionCodes.UsersAdmin))
                {
                    var securitySummary = await SecurityOperationsEndpoints.BuildSecurityActivitySummaryAsync(
                        dbContext,
                        hours: null,
                        cancellationToken: cancellationToken);
                    var lockedUsers = await SecurityOperationsEndpoints.BuildLockedUsersAsync(dbContext, cancellationToken);
                    items.AddRange(BuildSecurityWorkQueueItems(securitySummary, lockedUsers));
                }

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

                return Results.Ok(new OperationsWorkQueueResponse(
                    orderedItems.Length,
                    pageItems.Length,
                    normalizedSkip,
                    normalizedTake,
                    BuildSeverityConvention(),
                    pageItems));
            });

        return app;
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
                alert.NavigationPath,
                alert.AlertKey,
                null,
                null,
                null,
                alert.RelevantDate is null
                    ? null
                    : new DateTimeOffset(alert.RelevantDate.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)));
        }
    }

    private static OperationsWorkQueueItemResponse BuildDocumentWorkQueueItem(DocumentWorkQueueItemResponse item)
    {
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
            item.RouteHint,
            item.WorkItemKey,
            item.EntityType,
            item.EntityId.ToString(),
            item.DocumentId,
            RelevantUtc: null);
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
                "/admin/security",
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
                "/admin/security",
                SourceItemKey: null,
                EntityType: null,
                EntityId: null,
                DocumentId: null,
                summary.GeneratedAtUtc);
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
                "/admin/security",
                SourceItemKey: null,
                EntityType: null,
                EntityId: null,
                DocumentId: null,
                summary.GeneratedAtUtc);
        }
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
}
