using FMCPA.Api.Contracts.Closeout;
using FMCPA.Api.Extensions;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Domain.Entities.Audit;
using FMCPA.Domain.Entities.Donations;
using FMCPA.Domain.Entities.Documents;
using FMCPA.Domain.Entities.Federation;
using FMCPA.Domain.Entities.Markets;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class CloseoutEndpoints
{
    private const string MarketsModuleCode = "MARKETS";
    private const string DonationsModuleCode = "DONATARIAS";
    private const string FinancialsModuleCode = "FINANCIALS";
    private const string FederationModuleCode = "FEDERATION";
    private const string ContactsModuleCode = "CONTACTS";
    private const string MarketTenantEntityType = "MARKET_TENANT";
    private const string DonationEvidenceEntityType = "DONATION_APPLICATION_EVIDENCE";
    private const string FederationDonationEvidenceEntityType = "FEDERATION_DONATION_APPLICATION_EVIDENCE";
    private const string ValidDocumentIntegrityState = "VALID";
    private const string MissingFileDocumentIntegrityState = "MISSING_FILE";
    private const string SizeMismatchDocumentIntegrityState = "SIZE_MISMATCH";
    private const string InvalidPathDocumentIntegrityState = "INVALID_PATH";
    private const string MissingDocumentRecordIntegrityState = "MISSING_DOCUMENT_RECORD";
    private const string OrphanedDocumentRecordIntegrityState = "ORPHANED_DOCUMENT_RECORD";
    private const string MetadataMismatchDocumentIntegrityState = "METADATA_MISMATCH";
    private const string DueSoonAlertState = "DUE_SOON";
    private const string ExpiredAlertState = "EXPIRED";
    private const string ValidAlertState = "VALID";
    private const string RenewalAlertState = "RENEWAL";
    private const string AlertsDisabledState = "ALERTS_DISABLED";
    private const string NoAlertState = "NONE";
    private const string NotAppliedStatusCode = "NOT_APPLIED";
    private const string PartiallyAppliedStatusCode = "PARTIALLY_APPLIED";
    private const string InProcessStatusCode = "IN_PROCESS";
    private const string FollowUpPendingStatusCode = "FOLLOW_UP_PENDING";
    private const string FormalCloseEventSource = "FORMAL_CLOSE_EVENT";
    private const string LegacyCloseNormalizedSource = "LEGACY_CLOSE_NORMALIZED";
    private const string LegacyTimestampFallbackSource = "LEGACY_TIMESTAMP_FALLBACK";

    public static IEndpointRouteBuilder MapCloseoutEndpoints(this IEndpointRouteBuilder app)
    {
        var dashboardGroup = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard");

        dashboardGroup.MapGet(
            "/summary",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var summary = await BuildDashboardSummaryAsync(dbContext, cancellationToken);
                return Results.Ok(summary);
            });

        dashboardGroup.MapGet(
            "/alerts",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var alerts = await BuildDashboardAlertsAsync(dbContext, cancellationToken);
                return Results.Ok(alerts);
            });

        app.MapGet(
                "/api/commissions/consolidated",
                async (string? sourceModuleCode, int? commissionTypeId, string? recipientCategory, DateOnly? fromDate, DateOnly? toDate, string? q, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
                {
                    var response = await BuildConsolidatedCommissionsAsync(
                        dbContext,
                        sourceModuleCode,
                        commissionTypeId,
                        recipientCategory,
                        fromDate,
                        toDate,
                        q,
                        cancellationToken);

                    return Results.Ok(response);
                })
            .WithTags("Commissions");

        app.MapGet(
                "/api/bitacora",
                async (string? moduleCode, string? entityType, string? entityId, DateOnly? fromDate, DateOnly? toDate, string? q, int? take, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
                {
                    var response = await BuildBitacoraAsync(
                        dbContext,
                        moduleCode,
                        entityType,
                        entityId,
                        fromDate,
                        toDate,
                        q,
                        take,
                        cancellationToken);
                    return Results.Ok(response);
                })
            .WithTags("Bitacora");

        app.MapGet(
                "/api/history/closed-items",
                async (string? moduleCode, string? q, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
                {
                    var response = await BuildClosedItemsAsync(dbContext, moduleCode, q, cancellationToken);
                    return Results.Ok(response);
                })
            .WithTags("History");

        app.MapPost(
                "/api/history/normalize-legacy-closures",
                async (bool? dryRun, IHostEnvironment environment, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
                {
                    if (!environment.IsDevelopment())
                    {
                        return Results.Problem(
                            statusCode: StatusCodes.Status403Forbidden,
                            title: "Regularización no disponible",
                            detail: "La regularización retrospectiva de cierres heredados solo está habilitada en Development.");
                    }

                    var response = await NormalizeLegacyClosuresAsync(dbContext, dryRun == true, cancellationToken);
                    return Results.Ok(response);
                })
            .WithTags("History");

        app.MapGet(
                "/api/documents/integrity",
                async (string? moduleCode, string? entityType, string? entityId, int? take, PlatformDbContext dbContext, IDocumentBinaryStore documentBinaryStore, CancellationToken cancellationToken) =>
                {
                    var response = await BuildDocumentIntegrityAsync(
                        dbContext,
                        documentBinaryStore,
                        moduleCode,
                        entityType,
                        entityId,
                        take,
                        cancellationToken);
                    return Results.Ok(response);
                })
            .WithTags("Documents");

        return app;
    }

    private static async Task<DashboardSummaryResponse> BuildDashboardSummaryAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var markets = await dbContext.Markets
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .ToListAsync(cancellationToken);

        var marketIds = markets.Select(item => item.Id).ToArray();
        var tenants = marketIds.Length == 0
            ? []
            : await dbContext.MarketTenants
                .AsNoTracking()
                .Where(item => marketIds.Contains(item.MarketId))
                .ToListAsync(cancellationToken);

        var issues = marketIds.Length == 0
            ? []
            : await dbContext.MarketIssues
                .AsNoTracking()
                .Where(item => marketIds.Contains(item.MarketId))
                .Include(item => item.StatusCatalogEntry)
                .ToListAsync(cancellationToken);

        var marketLookup = markets.ToDictionary(item => item.Id);
        var marketCertificateAlertCount = tenants.Count(item =>
            GetMarketCertificateAlertState(marketLookup[item.MarketId], item) is DueSoonAlertState or ExpiredAlertState);

        var marketsSummary = new DashboardMarketsSummaryResponse(
            markets.Count,
            markets.Count(item => string.Equals(item.StatusCatalogEntry?.StatusCode, "ACTIVE", StringComparison.OrdinalIgnoreCase)),
            markets.Count(item => item.StatusCatalogEntry?.IsClosed == true),
            tenants.Count,
            issues.Count(item => item.StatusCatalogEntry?.AlertsEnabledByDefault == true),
            marketCertificateAlertCount);

        var donations = await dbContext.Donations
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .ToListAsync(cancellationToken);

        var donationIds = donations.Select(item => item.Id).ToArray();
        var donationApplications = donationIds.Length == 0
            ? []
            : await dbContext.DonationApplications
                .AsNoTracking()
                .Where(item => donationIds.Contains(item.DonationId))
                .ToListAsync(cancellationToken);

        var donationAppliedLookup = donationApplications
            .GroupBy(item => item.DonationId)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.Sum(item => item.AppliedAmount));

        var donationsSummary = new DashboardDonationsSummaryResponse(
            donations.Count,
            donations.Count(item => string.Equals(item.StatusCatalogEntry?.StatusCode, NotAppliedStatusCode, StringComparison.OrdinalIgnoreCase)),
            donations.Count(item => string.Equals(item.StatusCatalogEntry?.StatusCode, PartiallyAppliedStatusCode, StringComparison.OrdinalIgnoreCase)),
            donations.Count(item => item.StatusCatalogEntry?.IsClosed == true),
            donations.Sum(item => item.BaseAmount),
            donationAppliedLookup.Values.Sum(),
            donations.Count(item => item.StatusCatalogEntry?.StatusCode is NotAppliedStatusCode or PartiallyAppliedStatusCode));

        var permits = await dbContext.FinancialPermits
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .ToListAsync(cancellationToken);

        var permitIds = permits.Select(item => item.Id).ToArray();
        var credits = permitIds.Length == 0
            ? []
            : await dbContext.FinancialCredits
                .AsNoTracking()
                .Where(item => permitIds.Contains(item.FinancialPermitId))
                .ToListAsync(cancellationToken);

        var creditIds = credits.Select(item => item.Id).ToArray();
        var financialCommissions = creditIds.Length == 0
            ? []
            : await dbContext.FinancialCreditCommissions
                .AsNoTracking()
                .Where(item => creditIds.Contains(item.FinancialCreditId))
                .ToListAsync(cancellationToken);

        var financialAlertStates = permits
            .Select(item => GetFinancialPermitAlertState(item.StatusCatalogEntry, item.ValidTo))
            .ToList();

        var financialsSummary = new DashboardFinancialsSummaryResponse(
            permits.Count,
            permits.Count(item => item.StatusCatalogEntry?.IsClosed == true),
            credits.Count,
            financialCommissions.Count,
            financialAlertStates.Count(item => item == DueSoonAlertState),
            financialAlertStates.Count(item => item == ExpiredAlertState),
            financialAlertStates.Count(item => item == RenewalAlertState),
            financialAlertStates.Count(item => item is DueSoonAlertState or ExpiredAlertState or RenewalAlertState));

        var federationActions = await dbContext.FederationActions
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .ToListAsync(cancellationToken);

        var federationDonations = await dbContext.FederationDonations
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .ToListAsync(cancellationToken);

        var federationDonationIds = federationDonations.Select(item => item.Id).ToArray();
        var federationApplications = federationDonationIds.Length == 0
            ? []
            : await dbContext.FederationDonationApplications
                .AsNoTracking()
                .Where(item => federationDonationIds.Contains(item.FederationDonationId))
                .ToListAsync(cancellationToken);

        var federationApplicationIds = federationApplications.Select(item => item.Id).ToArray();
        var federationEvidenceCount = federationApplicationIds.Length == 0
            ? 0
            : await dbContext.FederationDonationApplicationEvidences
                .AsNoTracking()
                .CountAsync(item => federationApplicationIds.Contains(item.FederationDonationApplicationId), cancellationToken);

        var federationCommissionCount = federationApplicationIds.Length == 0
            ? 0
            : await dbContext.FederationDonationApplicationCommissions
                .AsNoTracking()
                .CountAsync(item => federationApplicationIds.Contains(item.FederationDonationApplicationId), cancellationToken);

        var federationSummary = new DashboardFederationSummaryResponse(
            federationActions.Count,
            federationActions.Count(item => item.StatusCatalogEntry?.IsClosed == true),
            federationActions.Count(item => item.StatusCatalogEntry?.StatusCode is InProcessStatusCode or FollowUpPendingStatusCode),
            federationDonations.Count,
            federationDonations.Count(item => item.StatusCatalogEntry?.IsClosed == true),
            federationDonations.Count(item => item.StatusCatalogEntry?.StatusCode is NotAppliedStatusCode or PartiallyAppliedStatusCode),
            federationCommissionCount,
            federationEvidenceCount);

        var totals = new DashboardTotalsSummaryResponse(
            marketsSummary.CertificateAlertCount
            + donationsSummary.ActiveAlertCount
            + financialsSummary.ActiveAlertCount
            + federationSummary.ActionAlertCount
            + federationSummary.DonationAlertCount,
            marketsSummary.ClosedOrArchivedMarkets
            + donationsSummary.ClosedDonationsCount
            + financialsSummary.ClosedPermitsCount
            + federationSummary.ClosedActionsCount
            + federationSummary.ClosedDonationsCount,
            financialsSummary.CommissionCount + federationSummary.CommissionCount,
            federationSummary.EvidenceCount
            + await dbContext.DonationApplicationEvidences.AsNoTracking().CountAsync(cancellationToken)
            + tenants.Count);

        return new DashboardSummaryResponse(marketsSummary, donationsSummary, financialsSummary, federationSummary, totals);
    }

    private static async Task<DashboardAlertsResponse> BuildDashboardAlertsAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var marketAlerts = await BuildMarketCertificateAlertsAsync(dbContext, cancellationToken);
        var donationAlerts = await BuildDonationAlertsAsync(dbContext, cancellationToken);
        var financialAlerts = await BuildFinancialAlertsAsync(dbContext, cancellationToken);
        var federationActionAlerts = await BuildFederationActionAlertsAsync(dbContext, cancellationToken);
        var federationDonationAlerts = await BuildFederationDonationAlertsAsync(dbContext, cancellationToken);

        return new DashboardAlertsResponse(
            marketAlerts,
            donationAlerts,
            financialAlerts,
            federationActionAlerts,
            federationDonationAlerts);
    }

    private static async Task<ConsolidatedCommissionListResponse> BuildConsolidatedCommissionsAsync(
        PlatformDbContext dbContext,
        string? sourceModuleCode,
        int? commissionTypeId,
        string? recipientCategory,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? query,
        CancellationToken cancellationToken)
    {
        var normalizedSourceModuleCode = NormalizeOptionalUpper(sourceModuleCode);
        var normalizedRecipientCategory = NormalizeOptionalUpper(recipientCategory);
        var searchPattern = BuildSearchPattern(query);

        var items = new List<ConsolidatedCommissionItemResponse>();

        if (normalizedSourceModuleCode is null or FinancialsModuleCode)
        {
            var financialQuery = dbContext.FinancialCreditCommissions
                .AsNoTracking()
                .Where(item => commissionTypeId == null || item.CommissionTypeId == commissionTypeId.Value)
                .Where(item => normalizedRecipientCategory == null || item.RecipientCategory == normalizedRecipientCategory)
                .Where(item => fromDate == null || item.FinancialCredit!.AuthorizationDate >= fromDate.Value)
                .Where(item => toDate == null || item.FinancialCredit!.AuthorizationDate <= toDate.Value);

            if (searchPattern is not null)
            {
                financialQuery = financialQuery.Where(item =>
                    EF.Functions.Like(item.RecipientName, searchPattern)
                    || EF.Functions.Like(item.CommissionType!.Name, searchPattern)
                    || EF.Functions.Like(item.FinancialCredit!.BeneficiaryName, searchPattern)
                    || EF.Functions.Like(item.FinancialCredit.PromoterName, searchPattern)
                    || EF.Functions.Like(item.FinancialCredit.FinancialPermit!.FinancialName, searchPattern)
                    || EF.Functions.Like(item.FinancialCredit.FinancialPermit.PlaceOrStand, searchPattern));
            }

            items.AddRange(
                await financialQuery
                    .OrderByDescending(item => item.FinancialCredit!.AuthorizationDate)
                    .ThenByDescending(item => item.CreatedUtc)
                    .Select(item => new ConsolidatedCommissionItemResponse(
                        item.Id,
                        FinancialsModuleCode,
                        "Financieras",
                        "FINANCIAL_CREDIT",
                        item.FinancialCreditId,
                        item.FinancialCredit!.AuthorizationDate,
                        item.CommissionTypeId,
                        item.CommissionType!.Code,
                        item.CommissionType.Name,
                        item.RecipientCategory,
                        item.RecipientName,
                        item.BaseAmount,
                        item.CommissionAmount,
                        item.FinancialCredit.FinancialPermit!.PlaceOrStand,
                        item.FinancialCredit.BeneficiaryName,
                        item.FinancialCredit.FinancialPermit.FinancialName,
                        item.Notes,
                        item.CreatedUtc,
                        "/financials"))
                    .ToListAsync(cancellationToken));
        }

        if (normalizedSourceModuleCode is null or FederationModuleCode)
        {
            var federationQuery = dbContext.FederationDonationApplicationCommissions
                .AsNoTracking()
                .Where(item => commissionTypeId == null || item.CommissionTypeId == commissionTypeId.Value)
                .Where(item => normalizedRecipientCategory == null || item.RecipientCategory == normalizedRecipientCategory)
                .Where(item => fromDate == null || item.FederationDonationApplication!.ApplicationDate >= fromDate.Value)
                .Where(item => toDate == null || item.FederationDonationApplication!.ApplicationDate <= toDate.Value);

            if (searchPattern is not null)
            {
                federationQuery = federationQuery.Where(item =>
                    EF.Functions.Like(item.RecipientName, searchPattern)
                    || EF.Functions.Like(item.CommissionType!.Name, searchPattern)
                    || EF.Functions.Like(item.FederationDonationApplication!.BeneficiaryOrDestinationName, searchPattern)
                    || EF.Functions.Like(item.FederationDonationApplication.FederationDonation!.DonorName, searchPattern)
                    || EF.Functions.Like(item.FederationDonationApplication.FederationDonation.Reference, searchPattern));
            }

            items.AddRange(
                await federationQuery
                    .OrderByDescending(item => item.FederationDonationApplication!.ApplicationDate)
                    .ThenByDescending(item => item.CreatedUtc)
                    .Select(item => new ConsolidatedCommissionItemResponse(
                        item.Id,
                        FederationModuleCode,
                        "Federacion",
                        "FEDERATION_DONATION_APPLICATION",
                        item.FederationDonationApplicationId,
                        item.FederationDonationApplication!.ApplicationDate,
                        item.CommissionTypeId,
                        item.CommissionType!.Code,
                        item.CommissionType.Name,
                        item.RecipientCategory,
                        item.RecipientName,
                        item.BaseAmount,
                        item.CommissionAmount,
                        item.FederationDonationApplication.FederationDonation!.Reference,
                        item.FederationDonationApplication.BeneficiaryOrDestinationName,
                        item.FederationDonationApplication.FederationDonation.DonorName,
                        item.Notes,
                        item.CreatedUtc,
                        "/federation"))
                    .ToListAsync(cancellationToken));
        }

        var orderedItems = items
            .OrderByDescending(item => item.OperationDate)
            .ThenByDescending(item => item.CreatedUtc)
            .ToList();

        return new ConsolidatedCommissionListResponse(
            orderedItems,
            orderedItems.Count,
            orderedItems.Sum(item => item.BaseAmount),
            orderedItems.Sum(item => item.CommissionAmount));
    }

    private static async Task<IReadOnlyList<BitacoraEntryResponse>> BuildBitacoraAsync(
        PlatformDbContext dbContext,
        string? moduleCode,
        string? entityType,
        string? entityId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? query,
        int? take,
        CancellationToken cancellationToken)
    {
        var normalizedModuleCode = NormalizeOptionalUpper(moduleCode);
        var normalizedEntityType = NormalizeOptionalUpper(entityType);
        var normalizedEntityId = NormalizeOptional(entityId);
        var queryable = dbContext.AuditEvents
            .AsNoTracking()
            .AsQueryable();

        if (normalizedModuleCode is not null)
        {
            queryable = queryable.Where(item => item.ModuleCode == normalizedModuleCode);
        }

        if (normalizedEntityType is not null)
        {
            queryable = queryable.Where(item => item.EntityType == normalizedEntityType);
        }

        if (normalizedEntityId is not null)
        {
            queryable = queryable.Where(item => item.EntityId == normalizedEntityId);
        }

        if (fromDate is DateOnly from)
        {
            var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            queryable = queryable.Where(item => item.OccurredUtc >= fromUtc);
        }

        if (toDate is DateOnly to)
        {
            var toExclusiveUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
            queryable = queryable.Where(item => item.OccurredUtc < toExclusiveUtc);
        }

        var searchPattern = BuildSearchPattern(query);
        if (searchPattern is not null)
        {
            queryable = queryable.Where(item =>
                EF.Functions.Like(item.Title, searchPattern)
                || EF.Functions.Like(item.Detail, searchPattern)
                || (item.Reference != null && EF.Functions.Like(item.Reference, searchPattern))
                || EF.Functions.Like(item.EntityType, searchPattern)
                || EF.Functions.Like(item.ActionType, searchPattern));
        }

        return await queryable
            .OrderByDescending(item => item.OccurredUtc)
            .Take(NormalizeTake(take))
            .Select(item => new BitacoraEntryResponse(
                item.Id.ToString(),
                item.OccurredUtc,
                item.ModuleCode,
                item.ModuleName,
                item.EntityType,
                item.EntityId,
                item.ActionType,
                item.Title,
                item.Detail,
                item.RelatedStatusCode,
                item.IsCloseEvent,
                item.IsCloseEvent ? ResolveCloseEventSource(item.ActionType) : null,
                item.Reference,
                item.NavigationPath,
                item.MetadataJson))
            .ToListAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<ClosedItemResponse>> BuildClosedItemsAsync(
        PlatformDbContext dbContext,
        string? moduleCode,
        string? query,
        CancellationToken cancellationToken)
    {
        var normalizedModuleCode = NormalizeOptionalUpper(moduleCode);
        var closeEventLookup = await BuildCloseEventLookupAsync(dbContext, normalizedModuleCode, cancellationToken);
        var items = new List<ClosedItemResponse>();

        if (normalizedModuleCode is null or MarketsModuleCode)
        {
            var markets = await dbContext.Markets
                .AsNoTracking()
                .Include(item => item.StatusCatalogEntry)
                .Where(item => item.StatusCatalogEntry!.IsClosed)
                .ToListAsync(cancellationToken);

            items.AddRange(
                markets.Select(item =>
                {
                    var key = BuildEntityKey(MarketsModuleCode, "MARKET", item.Id);
                    closeEventLookup.TryGetValue(key, out var closeEventSnapshot);

                    return new ClosedItemResponse(
                        $"MARKET:{item.Id}",
                        MarketsModuleCode,
                        "Mercados",
                        "MARKET",
                        item.Id.ToString(),
                        item.Name,
                        item.Borough,
                        item.SecretaryGeneralName,
                        item.StatusCatalogEntry!.StatusCode,
                        item.StatusCatalogEntry.StatusName,
                        closeEventSnapshot?.OccurredUtc ?? item.UpdatedUtc ?? item.CreatedUtc,
                        closeEventSnapshot?.Source ?? LegacyTimestampFallbackSource,
                        string.Equals(closeEventSnapshot?.Source, FormalCloseEventSource, StringComparison.Ordinal),
                        "/markets");
                }));
        }

        if (normalizedModuleCode is null or DonationsModuleCode)
        {
            var donations = await dbContext.Donations
                .AsNoTracking()
                .Include(item => item.StatusCatalogEntry)
                .Where(item => item.StatusCatalogEntry!.IsClosed)
                .ToListAsync(cancellationToken);

            items.AddRange(
                donations.Select(item =>
                {
                    var key = BuildEntityKey(DonationsModuleCode, "DONATION", item.Id);
                    closeEventLookup.TryGetValue(key, out var closeEventSnapshot);

                    return new ClosedItemResponse(
                        $"DONATION:{item.Id}",
                        DonationsModuleCode,
                        "Donatarias",
                        "DONATION",
                        item.Id.ToString(),
                        item.DonorEntityName,
                        item.DonationType,
                        item.Reference,
                        item.StatusCatalogEntry!.StatusCode,
                        item.StatusCatalogEntry.StatusName,
                        closeEventSnapshot?.OccurredUtc ?? item.UpdatedUtc ?? item.CreatedUtc,
                        closeEventSnapshot?.Source ?? LegacyTimestampFallbackSource,
                        string.Equals(closeEventSnapshot?.Source, FormalCloseEventSource, StringComparison.Ordinal),
                        "/donatarias");
                }));
        }

        if (normalizedModuleCode is null or FinancialsModuleCode)
        {
            var permits = await dbContext.FinancialPermits
                .AsNoTracking()
                .Include(item => item.StatusCatalogEntry)
                .Where(item => item.StatusCatalogEntry!.IsClosed)
                .ToListAsync(cancellationToken);

            items.AddRange(
                permits.Select(item =>
                {
                    var key = BuildEntityKey(FinancialsModuleCode, "FINANCIAL_PERMIT", item.Id);
                    closeEventLookup.TryGetValue(key, out var closeEventSnapshot);

                    return new ClosedItemResponse(
                        $"FINANCIAL_PERMIT:{item.Id}",
                        FinancialsModuleCode,
                        "Financieras",
                        "FINANCIAL_PERMIT",
                        item.Id.ToString(),
                        item.FinancialName,
                        item.PlaceOrStand,
                        item.InstitutionOrDependency,
                        item.StatusCatalogEntry!.StatusCode,
                        item.StatusCatalogEntry.StatusName,
                        closeEventSnapshot?.OccurredUtc ?? item.UpdatedUtc ?? item.CreatedUtc,
                        closeEventSnapshot?.Source ?? LegacyTimestampFallbackSource,
                        string.Equals(closeEventSnapshot?.Source, FormalCloseEventSource, StringComparison.Ordinal),
                        "/financials");
                }));
        }

        if (normalizedModuleCode is null or FederationModuleCode)
        {
            var actions = await dbContext.FederationActions
                .AsNoTracking()
                .Include(item => item.StatusCatalogEntry)
                .Where(item => item.StatusCatalogEntry!.IsClosed)
                .ToListAsync(cancellationToken);

            items.AddRange(
                actions.Select(item =>
                {
                    var key = BuildEntityKey(FederationModuleCode, "FEDERATION_ACTION", item.Id);
                    closeEventLookup.TryGetValue(key, out var closeEventSnapshot);

                    return new ClosedItemResponse(
                        $"FEDERATION_ACTION:{item.Id}",
                        FederationModuleCode,
                        "Federacion",
                        "FEDERATION_ACTION",
                        item.Id.ToString(),
                        MapFederationActionTypeName(item.ActionTypeCode),
                        item.CounterpartyOrInstitution,
                        item.Objective,
                        item.StatusCatalogEntry!.StatusCode,
                        item.StatusCatalogEntry.StatusName,
                        closeEventSnapshot?.OccurredUtc ?? item.UpdatedUtc ?? item.CreatedUtc,
                        closeEventSnapshot?.Source ?? LegacyTimestampFallbackSource,
                        string.Equals(closeEventSnapshot?.Source, FormalCloseEventSource, StringComparison.Ordinal),
                        "/federation");
                }));

            var donations = await dbContext.FederationDonations
                .AsNoTracking()
                .Include(item => item.StatusCatalogEntry)
                .Where(item => item.StatusCatalogEntry!.IsClosed)
                .ToListAsync(cancellationToken);

            items.AddRange(
                donations.Select(item =>
                {
                    var key = BuildEntityKey(FederationModuleCode, "FEDERATION_DONATION", item.Id);
                    closeEventLookup.TryGetValue(key, out var closeEventSnapshot);

                    return new ClosedItemResponse(
                        $"FEDERATION_DONATION:{item.Id}",
                        FederationModuleCode,
                        "Federacion",
                        "FEDERATION_DONATION",
                        item.Id.ToString(),
                        item.DonorName,
                        item.DonationType,
                        item.Reference,
                        item.StatusCatalogEntry!.StatusCode,
                        item.StatusCatalogEntry.StatusName,
                        closeEventSnapshot?.OccurredUtc ?? item.UpdatedUtc ?? item.CreatedUtc,
                        closeEventSnapshot?.Source ?? LegacyTimestampFallbackSource,
                        string.Equals(closeEventSnapshot?.Source, FormalCloseEventSource, StringComparison.Ordinal),
                        "/federation");
                }));
        }

        return items
            .Where(item => MatchesQuery(item.Title, item.Subtitle, item.Reference, query))
            .OrderByDescending(item => item.HistoricalTimestampUtc)
            .ToList();
    }

    private static async Task<DocumentIntegrityResponse> BuildDocumentIntegrityAsync(
        PlatformDbContext dbContext,
        IDocumentBinaryStore documentBinaryStore,
        string? moduleCode,
        string? entityType,
        string? entityId,
        int? take,
        CancellationToken cancellationToken)
    {
        var normalizedModuleCode = NormalizeOptionalUpper(moduleCode);
        var normalizedEntityType = NormalizeOptionalUpper(entityType);
        var normalizedEntityId = NormalizeOptional(entityId);
        var candidates = await BuildDocumentMetadataCandidatesAsync(dbContext, normalizedModuleCode, cancellationToken);

        if (normalizedEntityType is not null)
        {
            candidates = candidates
                .Where(item => item.EntityType == normalizedEntityType)
                .ToList();
        }

        if (normalizedEntityId is not null)
        {
            candidates = candidates
                .Where(item => string.Equals(item.EntityId.ToString(), normalizedEntityId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var storedDocumentsQuery = dbContext.StoredDocuments
            .AsNoTracking()
            .AsQueryable();

        if (normalizedModuleCode is not null)
        {
            storedDocumentsQuery = storedDocumentsQuery.Where(item => item.ModuleCode == normalizedModuleCode);
        }

        if (normalizedEntityType is not null)
        {
            storedDocumentsQuery = storedDocumentsQuery.Where(item => item.EntityType == normalizedEntityType);
        }

        if (normalizedEntityId is not null && Guid.TryParse(normalizedEntityId, out var parsedEntityId))
        {
            storedDocumentsQuery = storedDocumentsQuery.Where(item => item.EntityId == parsedEntityId);
        }

        var storedDocuments = await storedDocumentsQuery.ToListAsync(cancellationToken);
        var candidateLookup = candidates.ToDictionary(item => BuildEntityKey(item.DocumentAreaCode, item.EntityType, item.EntityId));
        var storedDocumentLookup = storedDocuments.ToDictionary(item => BuildEntityKey(item.DocumentAreaCode, item.EntityType, item.EntityId));
        var issues = new List<DocumentIntegrityIssueResponse>();
        var records = new List<DocumentRecordResponse>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates.OrderByDescending(item => item.CreatedUtc))
        {
            var key = BuildEntityKey(candidate.DocumentAreaCode, candidate.EntityType, candidate.EntityId);
            seenKeys.Add(key);
            storedDocumentLookup.TryGetValue(key, out var storedDocument);

            var documentReference = storedDocument is null
                ? candidate
                : new DocumentMetadataCandidate(
                    storedDocument.ModuleCode,
                    MapModuleName(storedDocument.ModuleCode),
                    storedDocument.DocumentAreaCode,
                    storedDocument.EntityType,
                    storedDocument.EntityId,
                    storedDocument.OriginalFileName,
                    storedDocument.StoredRelativePath,
                    storedDocument.ContentType,
                    storedDocument.SizeBytes,
                    storedDocument.CreatedUtc,
                    BuildNavigationPath(storedDocument.ModuleCode));

            var inspection = await documentBinaryStore.InspectAsync(
                documentReference.DocumentAreaCode,
                documentReference.StoredRelativePath,
                documentReference.SizeBytes,
                cancellationToken);

            var issueStates = new List<string>();

            if (storedDocument is null)
            {
                issueStates.Add(MissingDocumentRecordIntegrityState);
                issues.Add(
                    CreateDocumentIntegrityIssue(
                        candidate,
                        MissingDocumentRecordIntegrityState,
                        "Documento sin registro transversal",
                        "La entidad de negocio conserva metadatos documentales, pero no existe fila equivalente en StoredDocuments."));
            }
            else if (HasDocumentMetadataMismatch(candidate, storedDocument))
            {
                issueStates.Add(MetadataMismatchDocumentIntegrityState);
                issues.Add(
                    CreateDocumentIntegrityIssue(
                        candidate,
                        MetadataMismatchDocumentIntegrityState,
                        "Metadatos documentales inconsistentes",
                        "El registro transversal y los metadatos de la entidad de negocio no coinciden en nombre, ruta, content type o tamaño."));
            }

            if (!string.Equals(inspection.IntegrityState, ValidDocumentIntegrityState, StringComparison.Ordinal))
            {
                issueStates.Add(inspection.IntegrityState);
                issues.Add(
                    CreateDocumentIntegrityIssue(
                        documentReference,
                        inspection.IntegrityState,
                        "Archivo físico inconsistente",
                        BuildIntegrityDetail(inspection.IntegrityState)));
            }

            records.Add(
                new DocumentRecordResponse(
                    key,
                    candidate.ModuleCode,
                    candidate.ModuleName,
                    candidate.DocumentAreaCode,
                    candidate.EntityType,
                    candidate.EntityId.ToString(),
                    documentReference.OriginalFileName,
                    documentReference.StoredRelativePath,
                    documentReference.ContentType,
                    documentReference.SizeBytes,
                    inspection.ActualSizeBytes,
                    ResolveDocumentIntegrityState(issueStates),
                    storedDocument is not null,
                    storedDocument?.IsLegacyBackfill ?? false,
                    documentReference.CreatedUtc,
                    storedDocument?.Sha256Hex,
                    candidate.NavigationPath));
        }

        foreach (var storedDocument in storedDocuments.OrderByDescending(item => item.CreatedUtc))
        {
            var key = BuildEntityKey(storedDocument.DocumentAreaCode, storedDocument.EntityType, storedDocument.EntityId);
            if (seenKeys.Contains(key))
            {
                continue;
            }

            var documentReference = new DocumentMetadataCandidate(
                storedDocument.ModuleCode,
                MapModuleName(storedDocument.ModuleCode),
                storedDocument.DocumentAreaCode,
                storedDocument.EntityType,
                storedDocument.EntityId,
                storedDocument.OriginalFileName,
                storedDocument.StoredRelativePath,
                storedDocument.ContentType,
                storedDocument.SizeBytes,
                storedDocument.CreatedUtc,
                BuildNavigationPath(storedDocument.ModuleCode));

            var inspection = await documentBinaryStore.InspectAsync(
                storedDocument.DocumentAreaCode,
                storedDocument.StoredRelativePath,
                storedDocument.SizeBytes,
                cancellationToken);

            var issueStates = new List<string> { OrphanedDocumentRecordIntegrityState };
            issues.Add(
                CreateDocumentIntegrityIssue(
                    documentReference,
                    OrphanedDocumentRecordIntegrityState,
                    "Registro transversal sin entidad vigente",
                    "Existe una fila en StoredDocuments, pero no se encontró la entidad de negocio que debería respaldarla."));

            if (!string.Equals(inspection.IntegrityState, ValidDocumentIntegrityState, StringComparison.Ordinal))
            {
                issueStates.Add(inspection.IntegrityState);
                issues.Add(
                    CreateDocumentIntegrityIssue(
                        documentReference,
                        inspection.IntegrityState,
                        "Archivo físico inconsistente",
                        BuildIntegrityDetail(inspection.IntegrityState)));
            }

            records.Add(
                new DocumentRecordResponse(
                    key,
                    documentReference.ModuleCode,
                    documentReference.ModuleName,
                    documentReference.DocumentAreaCode,
                    documentReference.EntityType,
                    documentReference.EntityId.ToString(),
                    documentReference.OriginalFileName,
                    documentReference.StoredRelativePath,
                    documentReference.ContentType,
                    documentReference.SizeBytes,
                    inspection.ActualSizeBytes,
                    ResolveDocumentIntegrityState(issueStates),
                    true,
                    storedDocument.IsLegacyBackfill,
                    documentReference.CreatedUtc,
                    storedDocument.Sha256Hex,
                    documentReference.NavigationPath));
        }

        var orderedRecords = records
            .OrderByDescending(item => item.CreatedUtc)
            .Take(NormalizeTake(take))
            .ToList();

        var orderedRecordKeys = orderedRecords.Select(item => item.DocumentKey).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var orderedIssues = issues
            .Where(item => orderedRecordKeys.Contains(BuildEntityKey(item.DocumentAreaCode, item.EntityType, Guid.Parse(item.EntityId))))
            .Take(200)
            .ToList();

        var summary = new DocumentIntegritySummaryResponse(
            orderedRecords.Count,
            orderedRecords.Count(item => item.IntegrityState == ValidDocumentIntegrityState),
            orderedIssues.Count(item => item.IntegrityState == MissingFileDocumentIntegrityState),
            orderedIssues.Count(item => item.IntegrityState == SizeMismatchDocumentIntegrityState),
            orderedIssues.Count(item => item.IntegrityState == InvalidPathDocumentIntegrityState),
            orderedIssues.Count(item => item.IntegrityState == MissingDocumentRecordIntegrityState),
            orderedIssues.Count(item => item.IntegrityState == OrphanedDocumentRecordIntegrityState),
            orderedIssues.Count(item => item.IntegrityState == MetadataMismatchDocumentIntegrityState));

        return new DocumentIntegrityResponse(summary, orderedIssues, orderedRecords);
    }

    private static async Task<LegacyCloseNormalizationResponse> NormalizeLegacyClosuresAsync(
        PlatformDbContext dbContext,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var closeEventLookup = await BuildCloseEventLookupAsync(dbContext, null, cancellationToken);
        var scannedItems = await BuildLegacyCloseNormalizationCandidatesAsync(dbContext, cancellationToken);
        var normalizedItems = new List<LegacyCloseNormalizationItemResponse>();
        var pendingEvents = new List<(LegacyCloseNormalizationCandidate Candidate, AuditEvent AuditEvent)>();
        var normalizedAtUtc = DateTimeOffset.UtcNow;

        foreach (var candidate in scannedItems
                     .OrderByDescending(item => item.HistoricalTimestampUtc)
                     .ThenBy(item => item.ModuleCode)
                     .ThenBy(item => item.Title))
        {
            var recordKey = BuildEntityKey(candidate.ModuleCode, candidate.ItemType, candidate.EntityId);
            if (closeEventLookup.TryGetValue(recordKey, out var existingCloseEvent))
            {
                normalizedItems.Add(
                    new LegacyCloseNormalizationItemResponse(
                        recordKey,
                        candidate.ModuleCode,
                        candidate.ModuleName,
                        candidate.ItemType,
                        candidate.EntityId.ToString(),
                        candidate.Title,
                        candidate.Reference,
                        candidate.StatusCode,
                        candidate.StatusName,
                        candidate.HistoricalTimestampUtc,
                        candidate.LegacyTimestampSource,
                        existingCloseEvent.Source == FormalCloseEventSource
                            ? "SKIPPED_FORMAL_CLOSE_EXISTS"
                            : "SKIPPED_ALREADY_NORMALIZED",
                        candidate.NavigationPath,
                        null));
                continue;
            }

            if (dryRun)
            {
                normalizedItems.Add(
                    new LegacyCloseNormalizationItemResponse(
                        recordKey,
                        candidate.ModuleCode,
                        candidate.ModuleName,
                        candidate.ItemType,
                        candidate.EntityId.ToString(),
                        candidate.Title,
                        candidate.Reference,
                        candidate.StatusCode,
                        candidate.StatusName,
                        candidate.HistoricalTimestampUtc,
                        candidate.LegacyTimestampSource,
                        "DRY_RUN_ELIGIBLE",
                        candidate.NavigationPath,
                        null));
                continue;
            }

            var auditEvent = AuditEventSupport.CreateAuditEvent(
                candidate.ModuleCode,
                candidate.ModuleName,
                candidate.ItemType,
                candidate.EntityId,
                AuditEventSupport.LegacyCloseNormalizedActionType,
                candidate.Title,
                candidate.NormalizedDetail,
                candidate.StatusCode,
                candidate.Reference,
                candidate.NavigationPath,
                isCloseEvent: true,
                metadata: new
                {
                    normalizationMode = "RETROSPECTIVE_LEGACY_CLOSE_NORMALIZATION",
                    sourceTimestampKind = candidate.LegacyTimestampSource,
                    sourceTimestampUtc = candidate.HistoricalTimestampUtc,
                    normalizedAtUtc,
                    note = "Evento retrospectivo generado a partir de estado y timestamp legado. No equivale a un cierre formal capturado operativamente."
                },
                occurredUtc: candidate.HistoricalTimestampUtc);

            pendingEvents.Add((candidate, auditEvent));
        }

        if (!dryRun && pendingEvents.Count > 0)
        {
            dbContext.AuditEvents.AddRange(pendingEvents.Select(item => item.AuditEvent));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        normalizedItems.AddRange(
            pendingEvents.Select(item =>
                new LegacyCloseNormalizationItemResponse(
                    BuildEntityKey(item.Candidate.ModuleCode, item.Candidate.ItemType, item.Candidate.EntityId),
                    item.Candidate.ModuleCode,
                    item.Candidate.ModuleName,
                    item.Candidate.ItemType,
                    item.Candidate.EntityId.ToString(),
                    item.Candidate.Title,
                    item.Candidate.Reference,
                    item.Candidate.StatusCode,
                    item.Candidate.StatusName,
                    item.Candidate.HistoricalTimestampUtc,
                    item.Candidate.LegacyTimestampSource,
                    "NORMALIZED",
                    item.Candidate.NavigationPath,
                    item.AuditEvent.Id)));

        normalizedItems = normalizedItems
            .OrderByDescending(item => item.HistoricalTimestampUtc)
            .ThenBy(item => item.ModuleCode)
            .ThenBy(item => item.Title)
            .ToList();

        return new LegacyCloseNormalizationResponse(
            dryRun,
            scannedItems.Count,
            normalizedItems.Count(item => item.Outcome is "DRY_RUN_ELIGIBLE" or "NORMALIZED"),
            normalizedItems.Count(item => item.Outcome == "NORMALIZED"),
            normalizedItems.Count(item => item.Outcome.StartsWith("SKIPPED_", StringComparison.Ordinal)),
            normalizedItems);
    }

    private static async Task<Dictionary<string, CloseEventSnapshot>> BuildCloseEventLookupAsync(
        PlatformDbContext dbContext,
        string? normalizedModuleCode,
        CancellationToken cancellationToken)
    {
        var queryable = dbContext.AuditEvents
            .AsNoTracking()
            .Where(item => item.IsCloseEvent);

        if (normalizedModuleCode is not null)
        {
            queryable = queryable.Where(item => item.ModuleCode == normalizedModuleCode);
        }

        var closeEvents = await queryable
            .Select(item => new
            {
                item.ModuleCode,
                item.EntityType,
                item.EntityId,
                item.ActionType,
                item.OccurredUtc
            })
            .ToListAsync(cancellationToken);

        return closeEvents
            .GroupBy(item => $"{item.ModuleCode}:{item.EntityType}:{item.EntityId}")
            .ToDictionary(
                grouping => grouping.Key,
                grouping =>
                {
                    var formalClose = grouping
                        .Where(item => !AuditEventSupport.IsLegacyNormalizedCloseAction(item.ActionType))
                        .OrderByDescending(item => item.OccurredUtc)
                        .FirstOrDefault();

                    if (formalClose is not null)
                    {
                        return new CloseEventSnapshot(formalClose.OccurredUtc, FormalCloseEventSource);
                    }

                    var normalizedClose = grouping
                        .Where(item => AuditEventSupport.IsLegacyNormalizedCloseAction(item.ActionType))
                        .OrderByDescending(item => item.OccurredUtc)
                        .First();

                    return new CloseEventSnapshot(normalizedClose.OccurredUtc, LegacyCloseNormalizedSource);
                });
    }

    private static async Task<List<LegacyCloseNormalizationCandidate>> BuildLegacyCloseNormalizationCandidatesAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var items = new List<LegacyCloseNormalizationCandidate>();

        var markets = await dbContext.Markets
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .Where(item => item.StatusCatalogEntry!.IsClosed)
            .ToListAsync(cancellationToken);

        items.AddRange(
            markets.Select(item => CreateLegacyCloseNormalizationCandidate(
                MarketsModuleCode,
                "Mercados",
                "MARKET",
                item.Id,
                item.Name,
                item.SecretaryGeneralName,
                item.StatusCatalogEntry!.StatusCode,
                item.StatusCatalogEntry.StatusName,
                item.UpdatedUtc,
                item.CreatedUtc,
                "/markets",
                $"Cierre retrospectivo normalizado para el mercado {item.Name} a partir del estado legado previo al hardening.")));

        var donations = await dbContext.Donations
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .Where(item => item.StatusCatalogEntry!.IsClosed)
            .ToListAsync(cancellationToken);

        items.AddRange(
            donations.Select(item => CreateLegacyCloseNormalizationCandidate(
                DonationsModuleCode,
                "Donatarias",
                "DONATION",
                item.Id,
                item.DonorEntityName,
                item.Reference,
                item.StatusCatalogEntry!.StatusCode,
                item.StatusCatalogEntry.StatusName,
                item.UpdatedUtc,
                item.CreatedUtc,
                "/donatarias",
                $"Cierre retrospectivo normalizado para la donación {item.Reference} a partir del estado legado previo al hardening.")));

        var permits = await dbContext.FinancialPermits
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .Where(item => item.StatusCatalogEntry!.IsClosed)
            .ToListAsync(cancellationToken);

        items.AddRange(
            permits.Select(item => CreateLegacyCloseNormalizationCandidate(
                FinancialsModuleCode,
                "Financieras",
                "FINANCIAL_PERMIT",
                item.Id,
                item.FinancialName,
                item.PlaceOrStand,
                item.StatusCatalogEntry!.StatusCode,
                item.StatusCatalogEntry.StatusName,
                item.UpdatedUtc,
                item.CreatedUtc,
                "/financials",
                $"Cierre retrospectivo normalizado para el oficio de {item.FinancialName} a partir del estado legado previo al hardening.")));

        var federationActions = await dbContext.FederationActions
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .Where(item => item.StatusCatalogEntry!.IsClosed)
            .ToListAsync(cancellationToken);

        items.AddRange(
            federationActions.Select(item => CreateLegacyCloseNormalizationCandidate(
                FederationModuleCode,
                "Federacion",
                "FEDERATION_ACTION",
                item.Id,
                MapFederationActionTypeName(item.ActionTypeCode),
                item.CounterpartyOrInstitution,
                item.StatusCatalogEntry!.StatusCode,
                item.StatusCatalogEntry.StatusName,
                item.UpdatedUtc,
                item.CreatedUtc,
                "/federation",
                $"Cierre retrospectivo normalizado para la gestión con {item.CounterpartyOrInstitution} a partir del estado legado previo al hardening.")));

        var federationDonations = await dbContext.FederationDonations
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .Where(item => item.StatusCatalogEntry!.IsClosed)
            .ToListAsync(cancellationToken);

        items.AddRange(
            federationDonations.Select(item => CreateLegacyCloseNormalizationCandidate(
                FederationModuleCode,
                "Federacion",
                "FEDERATION_DONATION",
                item.Id,
                item.DonorName,
                item.Reference,
                item.StatusCatalogEntry!.StatusCode,
                item.StatusCatalogEntry.StatusName,
                item.UpdatedUtc,
                item.CreatedUtc,
                "/federation",
                $"Cierre retrospectivo normalizado para la donación de federación {item.Reference} a partir del estado legado previo al hardening.")));

        return items;
    }

    private static LegacyCloseNormalizationCandidate CreateLegacyCloseNormalizationCandidate(
        string moduleCode,
        string moduleName,
        string itemType,
        Guid entityId,
        string title,
        string? reference,
        string statusCode,
        string statusName,
        DateTimeOffset? updatedUtc,
        DateTimeOffset createdUtc,
        string navigationPath,
        string normalizedDetail)
    {
        var historicalTimestampUtc = updatedUtc ?? createdUtc;
        var legacyTimestampSource = updatedUtc is not null ? "UPDATED_UTC" : "CREATED_UTC";

        return new LegacyCloseNormalizationCandidate(
            moduleCode,
            moduleName,
            itemType,
            entityId,
            title,
            NormalizeOptional(reference),
            statusCode,
            statusName,
            historicalTimestampUtc,
            legacyTimestampSource,
            navigationPath,
            normalizedDetail);
    }

    private static async Task<IReadOnlyList<DashboardAlertItemResponse>> BuildMarketCertificateAlertsAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tenants = await dbContext.MarketTenants
            .AsNoTracking()
            .Include(item => item.Market)
            .ThenInclude(market => market!.StatusCatalogEntry)
            .OrderBy(item => item.CertificateValidityTo)
            .ThenBy(item => item.TenantName)
            .ToListAsync(cancellationToken);

        return tenants
            .Where(item => item.Market is not null)
            .Select(item => new
            {
                Tenant = item,
                AlertState = GetMarketCertificateAlertState(item.Market!, item),
                DaysUntilExpiration = GetDaysUntil(item.CertificateValidityTo)
            })
            .Where(item => item.AlertState is DueSoonAlertState or ExpiredAlertState)
            .Select(item => new DashboardAlertItemResponse(
                $"MARKET_TENANT:{item.Tenant.Id}",
                MarketsModuleCode,
                "Mercados",
                "MARKET_TENANT_CERTIFICATE",
                item.Tenant.TenantName,
                item.Tenant.Market!.Name,
                item.AlertState == ExpiredAlertState
                    ? $"Cédula vencida desde hace {Math.Abs(item.DaysUntilExpiration)} días."
                    : $"Cédula por vencer en {item.DaysUntilExpiration} días.",
                item.AlertState,
                item.Tenant.CertificateNumber,
                item.Tenant.CertificateValidityTo,
                item.DaysUntilExpiration,
                "/markets"))
            .ToList();
    }

    private static async Task<IReadOnlyList<DashboardAlertItemResponse>> BuildDonationAlertsAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var donations = await dbContext.Donations
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .OrderByDescending(item => item.DonationDate)
            .ToListAsync(cancellationToken);

        var donationIds = donations.Select(item => item.Id).ToArray();
        var applications = donationIds.Length == 0
            ? []
            : await dbContext.DonationApplications
                .AsNoTracking()
                .Where(item => donationIds.Contains(item.DonationId))
                .ToListAsync(cancellationToken);

        var appliedLookup = applications
            .GroupBy(item => item.DonationId)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.Sum(item => item.AppliedAmount));

        return donations
            .Where(item => item.StatusCatalogEntry?.StatusCode is NotAppliedStatusCode or PartiallyAppliedStatusCode)
            .Select(item =>
            {
                var appliedAmount = appliedLookup.GetValueOrDefault(item.Id);
                var appliedPercentage = item.BaseAmount <= 0
                    ? 0
                    : decimal.Round(appliedAmount / item.BaseAmount * 100m, 2, MidpointRounding.AwayFromZero);

                return new DashboardAlertItemResponse(
                    $"DONATION:{item.Id}",
                    DonationsModuleCode,
                    "Donatarias",
                    "DONATION",
                    item.DonorEntityName,
                    item.Reference,
                    $"Monto aplicado {appliedAmount:0.##} de {item.BaseAmount:0.##} ({appliedPercentage:0.##}%).",
                    item.StatusCatalogEntry!.StatusCode,
                    item.Reference,
                    item.DonationDate,
                    null,
                    "/donatarias");
            })
            .ToList();
    }

    private static async Task<IReadOnlyList<DashboardAlertItemResponse>> BuildFinancialAlertsAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var permits = await dbContext.FinancialPermits
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .OrderBy(item => item.ValidTo)
            .ThenBy(item => item.FinancialName)
            .ToListAsync(cancellationToken);

        return permits
            .Select(item => new
            {
                Permit = item,
                AlertState = GetFinancialPermitAlertState(item.StatusCatalogEntry, item.ValidTo),
                DaysUntilExpiration = GetDaysUntil(item.ValidTo)
            })
            .Where(item => item.AlertState is DueSoonAlertState or ExpiredAlertState or RenewalAlertState)
            .Select(item => new DashboardAlertItemResponse(
                $"FINANCIAL_PERMIT:{item.Permit.Id}",
                FinancialsModuleCode,
                "Financieras",
                "FINANCIAL_PERMIT",
                item.Permit.FinancialName,
                item.Permit.PlaceOrStand,
                item.AlertState == RenewalAlertState
                    ? "Oficio en renovación, requiere seguimiento operativo."
                    : item.AlertState == ExpiredAlertState
                        ? $"Oficio vencido desde hace {Math.Abs(item.DaysUntilExpiration)} días."
                        : $"Oficio por vencer en {item.DaysUntilExpiration} días.",
                item.AlertState,
                item.Permit.InstitutionOrDependency,
                item.Permit.ValidTo,
                item.AlertState == RenewalAlertState ? null : item.DaysUntilExpiration,
                "/financials"))
            .ToList();
    }

    private static async Task<IReadOnlyList<DashboardAlertItemResponse>> BuildFederationActionAlertsAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actions = await dbContext.FederationActions
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .OrderByDescending(item => item.ActionDate)
            .ToListAsync(cancellationToken);

        return actions
            .Where(item => item.StatusCatalogEntry?.StatusCode is InProcessStatusCode or FollowUpPendingStatusCode)
            .Select(item => new DashboardAlertItemResponse(
                $"FEDERATION_ACTION:{item.Id}",
                FederationModuleCode,
                "Federacion",
                "FEDERATION_ACTION",
                MapFederationActionTypeName(item.ActionTypeCode),
                item.CounterpartyOrInstitution,
                item.Objective,
                item.StatusCatalogEntry!.StatusCode,
                item.CounterpartyOrInstitution,
                item.ActionDate,
                null,
                "/federation"))
            .ToList();
    }

    private static async Task<IReadOnlyList<DashboardAlertItemResponse>> BuildFederationDonationAlertsAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var donations = await dbContext.FederationDonations
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .OrderByDescending(item => item.DonationDate)
            .ToListAsync(cancellationToken);

        var donationIds = donations.Select(item => item.Id).ToArray();
        var applications = donationIds.Length == 0
            ? []
            : await dbContext.FederationDonationApplications
                .AsNoTracking()
                .Where(item => donationIds.Contains(item.FederationDonationId))
                .ToListAsync(cancellationToken);

        var appliedLookup = applications
            .GroupBy(item => item.FederationDonationId)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.Sum(item => item.AppliedAmount));

        return donations
            .Where(item => item.StatusCatalogEntry?.StatusCode is NotAppliedStatusCode or PartiallyAppliedStatusCode)
            .Select(item =>
            {
                var appliedAmount = appliedLookup.GetValueOrDefault(item.Id);
                var appliedPercentage = item.BaseAmount <= 0
                    ? 0
                    : decimal.Round(appliedAmount / item.BaseAmount * 100m, 2, MidpointRounding.AwayFromZero);

                return new DashboardAlertItemResponse(
                    $"FEDERATION_DONATION:{item.Id}",
                    FederationModuleCode,
                    "Federacion",
                    "FEDERATION_DONATION",
                    item.DonorName,
                    item.Reference,
                    $"Monto aplicado {appliedAmount:0.##} de {item.BaseAmount:0.##} ({appliedPercentage:0.##}%).",
                    item.StatusCatalogEntry!.StatusCode,
                    item.Reference,
                    item.DonationDate,
                    null,
                    "/federation");
            })
            .ToList();
    }

    private static string GetMarketCertificateAlertState(Market market, MarketTenant tenant)
    {
        if (market.StatusCatalogEntry?.AlertsEnabledByDefault != true)
        {
            return AlertsDisabledState;
        }

        var daysUntilExpiration = GetDaysUntil(tenant.CertificateValidityTo);
        if (daysUntilExpiration < 0)
        {
            return ExpiredAlertState;
        }

        return daysUntilExpiration <= 30
            ? DueSoonAlertState
            : ValidAlertState;
    }

    private static string GetFinancialPermitAlertState(ModuleStatusCatalogEntry? statusCatalogEntry, DateOnly validTo)
    {
        if (statusCatalogEntry is null)
        {
            return NoAlertState;
        }

        if (!statusCatalogEntry.AlertsEnabledByDefault)
        {
            return AlertsDisabledState;
        }

        if (string.Equals(statusCatalogEntry.StatusCode, "RENEW", StringComparison.OrdinalIgnoreCase))
        {
            return RenewalAlertState;
        }

        var daysUntilExpiration = GetDaysUntil(validTo);
        if (daysUntilExpiration < 0)
        {
            return ExpiredAlertState;
        }

        return daysUntilExpiration <= 30
            ? DueSoonAlertState
            : ValidAlertState;
    }

    private static int GetDaysUntil(DateOnly targetDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        return targetDate.DayNumber - today.DayNumber;
    }

    private static int NormalizeTake(int? take)
    {
        if (take is null or <= 0)
        {
            return 120;
        }

        return Math.Min(take.Value, 300);
    }

    private static async Task<List<DocumentMetadataCandidate>> BuildDocumentMetadataCandidatesAsync(
        PlatformDbContext dbContext,
        string? normalizedModuleCode,
        CancellationToken cancellationToken)
    {
        var items = new List<DocumentMetadataCandidate>();

        if (normalizedModuleCode is null or MarketsModuleCode)
        {
            var tenants = await dbContext.MarketTenants
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            items.AddRange(
                tenants.Select(item =>
                    new DocumentMetadataCandidate(
                        MarketsModuleCode,
                        "Mercados",
                        DocumentAreaCodes.MarketsTenantCertificates,
                        MarketTenantEntityType,
                        item.Id,
                        item.CertificateOriginalFileName,
                        item.CertificateStoredRelativePath,
                        string.IsNullOrWhiteSpace(item.CertificateContentType) ? "application/octet-stream" : item.CertificateContentType,
                        item.CertificateFileSizeBytes,
                        item.CertificateUploadedUtc,
                        "/markets")));
        }

        if (normalizedModuleCode is null or DonationsModuleCode)
        {
            var evidences = await dbContext.DonationApplicationEvidences
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            items.AddRange(
                evidences.Select(item =>
                    new DocumentMetadataCandidate(
                        DonationsModuleCode,
                        "Donatarias",
                        DocumentAreaCodes.DonationsApplicationEvidences,
                        DonationEvidenceEntityType,
                        item.Id,
                        item.OriginalFileName,
                        item.StoredRelativePath,
                        string.IsNullOrWhiteSpace(item.ContentType) ? "application/octet-stream" : item.ContentType,
                        item.FileSizeBytes,
                        item.UploadedUtc,
                        "/donatarias")));
        }

        if (normalizedModuleCode is null or FederationModuleCode)
        {
            var evidences = await dbContext.FederationDonationApplicationEvidences
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            items.AddRange(
                evidences.Select(item =>
                    new DocumentMetadataCandidate(
                        FederationModuleCode,
                        "Federacion",
                        DocumentAreaCodes.FederationApplicationEvidences,
                        FederationDonationEvidenceEntityType,
                        item.Id,
                        item.OriginalFileName,
                        item.StoredRelativePath,
                        string.IsNullOrWhiteSpace(item.ContentType) ? "application/octet-stream" : item.ContentType,
                        item.FileSizeBytes,
                        item.UploadedUtc,
                        "/federation")));
        }

        return items;
    }

    private static string BuildEntityKey(string moduleCode, string entityType, Guid entityId)
    {
        return $"{moduleCode}:{entityType}:{entityId}";
    }

    private static string ResolveCloseEventSource(string actionType)
    {
        return AuditEventSupport.IsLegacyNormalizedCloseAction(actionType)
            ? LegacyCloseNormalizedSource
            : FormalCloseEventSource;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static string? NormalizeOptionalUpper(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }

    private static string? BuildSearchPattern(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : $"%{value.Trim()}%";
    }

    private static bool MatchesQuery(string title, string detail, string? reference, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var normalizedQuery = query.Trim();
        return title.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
            || detail.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
            || (reference?.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static string MapFederationActionTypeName(string actionTypeCode)
    {
        return actionTypeCode switch
        {
            "AGREEMENT" => "Convenio",
            "MEETING" => "Reunion",
            "INTERVIEW" => "Entrevista",
            "GOVERNMENT_MANAGEMENT" => "Gestion con gobierno",
            _ => actionTypeCode
        };
    }

    private static string MapParticipantSideName(string participantSide)
    {
        return participantSide.ToUpperInvariant() switch
        {
            "INTERNAL" => "interno",
            "EXTERNAL" => "externo",
            _ => participantSide.ToLowerInvariant()
        };
    }

    private static bool HasDocumentMetadataMismatch(DocumentMetadataCandidate candidate, StoredDocument storedDocument)
    {
        return !string.Equals(candidate.OriginalFileName, storedDocument.OriginalFileName, StringComparison.Ordinal)
               || !string.Equals(candidate.StoredRelativePath, storedDocument.StoredRelativePath, StringComparison.Ordinal)
               || !string.Equals(candidate.ContentType, storedDocument.ContentType, StringComparison.OrdinalIgnoreCase)
               || candidate.SizeBytes != storedDocument.SizeBytes;
    }

    private static string ResolveDocumentIntegrityState(IReadOnlyList<string> issueStates)
    {
        if (issueStates.Contains(InvalidPathDocumentIntegrityState, StringComparer.Ordinal))
        {
            return InvalidPathDocumentIntegrityState;
        }

        if (issueStates.Contains(MissingFileDocumentIntegrityState, StringComparer.Ordinal))
        {
            return MissingFileDocumentIntegrityState;
        }

        if (issueStates.Contains(SizeMismatchDocumentIntegrityState, StringComparer.Ordinal))
        {
            return SizeMismatchDocumentIntegrityState;
        }

        if (issueStates.Contains(OrphanedDocumentRecordIntegrityState, StringComparer.Ordinal))
        {
            return OrphanedDocumentRecordIntegrityState;
        }

        if (issueStates.Contains(MissingDocumentRecordIntegrityState, StringComparer.Ordinal))
        {
            return MissingDocumentRecordIntegrityState;
        }

        if (issueStates.Contains(MetadataMismatchDocumentIntegrityState, StringComparer.Ordinal))
        {
            return MetadataMismatchDocumentIntegrityState;
        }

        return ValidDocumentIntegrityState;
    }

    private static DocumentIntegrityIssueResponse CreateDocumentIntegrityIssue(
        DocumentMetadataCandidate reference,
        string integrityState,
        string title,
        string detail)
    {
        return new DocumentIntegrityIssueResponse(
            $"{reference.DocumentAreaCode}:{reference.EntityType}:{reference.EntityId}:{integrityState}",
            reference.ModuleCode,
            reference.ModuleName,
            reference.DocumentAreaCode,
            reference.EntityType,
            reference.EntityId.ToString(),
            integrityState,
            title,
            detail,
            reference.OriginalFileName,
            reference.StoredRelativePath,
            reference.NavigationPath);
    }

    private static string BuildIntegrityDetail(string integrityState)
    {
        return integrityState switch
        {
            MissingFileDocumentIntegrityState => "El archivo físico no existe en disco para la ruta registrada.",
            SizeMismatchDocumentIntegrityState => "El archivo físico existe, pero su tamaño ya no coincide con los metadatos persistidos.",
            InvalidPathDocumentIntegrityState => "La ruta relativa registrada no puede resolverse de forma segura dentro del storage configurado.",
            _ => "Se detectó una inconsistencia documental operativa."
        };
    }

    private static string MapModuleName(string moduleCode)
    {
        return moduleCode switch
        {
            MarketsModuleCode => "Mercados",
            DonationsModuleCode => "Donatarias",
            FinancialsModuleCode => "Financieras",
            FederationModuleCode => "Federacion",
            ContactsModuleCode => "Contactos",
            _ => moduleCode
        };
    }

    private static string BuildNavigationPath(string moduleCode)
    {
        return moduleCode switch
        {
            MarketsModuleCode => "/markets",
            DonationsModuleCode => "/donatarias",
            FinancialsModuleCode => "/financials",
            FederationModuleCode => "/federation",
            ContactsModuleCode => "/contacts",
            _ => "/dashboard"
        };
    }

    private sealed record CloseEventSnapshot(
        DateTimeOffset OccurredUtc,
        string Source);

    private sealed record LegacyCloseNormalizationCandidate(
        string ModuleCode,
        string ModuleName,
        string ItemType,
        Guid EntityId,
        string Title,
        string? Reference,
        string StatusCode,
        string StatusName,
        DateTimeOffset HistoricalTimestampUtc,
        string LegacyTimestampSource,
        string NavigationPath,
        string NormalizedDetail);

    private sealed record DocumentMetadataCandidate(
        string ModuleCode,
        string ModuleName,
        string DocumentAreaCode,
        string EntityType,
        Guid EntityId,
        string OriginalFileName,
        string StoredRelativePath,
        string ContentType,
        long SizeBytes,
        DateTimeOffset CreatedUtc,
        string NavigationPath);
}
