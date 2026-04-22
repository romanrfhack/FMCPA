using System.Net.Mail;
using FMCPA.Api.Contracts.Closeout;
using FMCPA.Api.Contracts.Markets;
using FMCPA.Api.Extensions;
using FMCPA.Domain.Entities.Markets;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class MarketsEndpoints
{
    private const string MarketsModuleCode = "MARKETS";
    private const string MarketsModuleName = "Mercados";
    private const string MarketContextCode = "MARKET";
    private const string MarketIssueContextCode = "MARKET_ISSUE";
    private const string MarketEntityType = "MARKET";
    private const string MarketTenantEntityType = "MARKET_TENANT";
    private const string MarketIssueEntityType = "MARKET_ISSUE";
    private const string MarketTenantCertificateAreaCode = DocumentAreaCodes.MarketsTenantCertificates;
    private const string ClosedStatusCode = "CLOSED";
    private const string DueSoonAlertState = "DUE_SOON";
    private const string ExpiredAlertState = "EXPIRED";
    private const string ValidAlertState = "VALID";
    private const string AlertsDisabledState = "ALERTS_DISABLED";

    public static IEndpointRouteBuilder MapMarketsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/markets")
            .WithTags("Markets");

        group.MapGet(
            "/alerts/tenants",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var alerts = await BuildTenantAlertsAsync(dbContext, cancellationToken);
                return Results.Ok(alerts);
            });

        group.MapGet(
            "/tenants/{tenantId:guid}/cedula",
            async (Guid tenantId, PlatformDbContext dbContext, IMarketTenantCertificateStorage certificateStorage, IDocumentBinaryStore documentBinaryStore, CancellationToken cancellationToken) =>
            {
                var tenant = await dbContext.MarketTenants
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);

                if (tenant is null)
                {
                    return Results.NotFound();
                }

                var storedDocument = await StoredDocumentSupport.FindStoredDocumentAsync(
                    dbContext,
                    MarketTenantCertificateAreaCode,
                    MarketTenantEntityType,
                    tenant.Id,
                    cancellationToken);

                var relativePath = storedDocument?.StoredRelativePath ?? tenant.CertificateStoredRelativePath;
                var originalFileName = storedDocument?.OriginalFileName ?? tenant.CertificateOriginalFileName;
                var contentType = storedDocument?.ContentType ?? tenant.CertificateContentType;
                var expectedSizeBytes = storedDocument?.SizeBytes ?? tenant.CertificateFileSizeBytes;
                var inspection = await documentBinaryStore.InspectAsync(
                    MarketTenantCertificateAreaCode,
                    relativePath,
                    expectedSizeBytes,
                    cancellationToken);

                if (!string.Equals(inspection.IntegrityState, "VALID", StringComparison.Ordinal))
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Inconsistencia documental detectada",
                        detail: $"La cédula digitalizada del locatario no puede servirse. Estado de integridad: {inspection.IntegrityState}.");
                }

                var download = await certificateStorage.OpenReadAsync(
                    relativePath,
                    originalFileName,
                    contentType,
                    cancellationToken);

                if (download is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Inconsistencia documental detectada",
                        detail: "La cédula digitalizada no se encuentra disponible en el storage local.");
                }

                return Results.File(download.Content, download.ContentType, download.OriginalFileName);
            });

        group.MapGet(
            string.Empty,
            async (string? statusCode, bool? alertsOnly, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var markets = await dbContext.Markets
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .OrderBy(item => item.Name)
                    .ToListAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(statusCode))
                {
                    var normalizedStatusCode = statusCode.Trim().ToUpperInvariant();
                    markets = markets
                        .Where(item => item.StatusCatalogEntry?.StatusCode == normalizedStatusCode)
                        .ToList();
                }

                var marketIds = markets.Select(item => item.Id).ToArray();

                var tenantCounts = await dbContext.MarketTenants
                    .AsNoTracking()
                    .Where(item => marketIds.Contains(item.MarketId))
                    .GroupBy(item => item.MarketId)
                    .Select(grouping => new { MarketId = grouping.Key, Count = grouping.Count() })
                    .ToDictionaryAsync(item => item.MarketId, item => item.Count, cancellationToken);

                var issueCounts = await dbContext.MarketIssues
                    .AsNoTracking()
                    .Where(item => marketIds.Contains(item.MarketId))
                    .GroupBy(item => item.MarketId)
                    .Select(grouping => new { MarketId = grouping.Key, Count = grouping.Count() })
                    .ToDictionaryAsync(item => item.MarketId, item => item.Count, cancellationToken);

                var tenants = await dbContext.MarketTenants
                    .AsNoTracking()
                    .Where(item => marketIds.Contains(item.MarketId))
                    .ToListAsync(cancellationToken);

                var activeAlertCounts = markets.ToDictionary(
                    item => item.Id,
                    item => CountActiveTenantAlerts(item, tenants.Where(tenant => tenant.MarketId == item.Id)));

                if (alertsOnly == true)
                {
                    markets = markets
                        .Where(item => activeAlertCounts.GetValueOrDefault(item.Id) > 0)
                        .ToList();
                }

                var response = markets
                    .Select(item => new MarketSummaryResponse(
                        item.Id,
                        item.Name,
                        item.Borough,
                        item.StatusCatalogEntryId,
                        item.StatusCatalogEntry!.StatusCode,
                        item.StatusCatalogEntry.StatusName,
                        item.StatusCatalogEntry.IsClosed,
                        item.StatusCatalogEntry.AlertsEnabledByDefault,
                        item.SecretaryGeneralContactId,
                        item.SecretaryGeneralName,
                        item.Notes,
                        tenantCounts.GetValueOrDefault(item.Id),
                        issueCounts.GetValueOrDefault(item.Id),
                        activeAlertCounts.GetValueOrDefault(item.Id),
                        item.CreatedUtc,
                        item.UpdatedUtc))
                    .ToList();

                return Results.Ok(response);
            });

        group.MapGet(
            "/{marketId:guid}",
            async (Guid marketId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var market = await dbContext.Markets
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == marketId, cancellationToken);

                if (market is null)
                {
                    return Results.NotFound();
                }

                var detail = await BuildMarketDetailAsync(dbContext, market, cancellationToken);
                return Results.Ok(detail);
            });

        group.MapPost(
            "/{marketId:guid}/close",
            async (Guid marketId, CloseRecordRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var market = await dbContext.Markets
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == marketId, cancellationToken);

                if (market is null)
                {
                    return Results.NotFound();
                }

                if (await AuditEventSupport.HasCloseEventAsync(dbContext, MarketsModuleCode, MarketEntityType, market.Id, cancellationToken))
                {
                    return Results.Conflict(new { message = StateTransitionSupport.BuildDuplicateCloseEventMessage("el mercado") });
                }

                var currentStatus = market.StatusCatalogEntry!;
                if (StateTransitionSupport.IsTerminal(currentStatus))
                {
                    return Results.Conflict(new { message = StateTransitionSupport.BuildTerminalCloseMessage("el mercado", currentStatus) });
                }

                if (!currentStatus.IsClosed)
                {
                    var closedStatus = await AuditEventSupport.ResolveContextStatusByCodeAsync(
                        dbContext,
                        MarketsModuleCode,
                        MarketContextCode,
                        ClosedStatusCode,
                        cancellationToken);

                    if (closedStatus is null)
                    {
                        return Results.ValidationProblem(
                            new Dictionary<string, string[]>
                            {
                                ["statusCatalogEntryId"] = ["The closed status for markets is not configured."]
                            });
                    }

                    market.SyncStatus(closedStatus.Id);
                    currentStatus = closedStatus;
                }

                var reason = NormalizeOptionalText(request.Reason);
                var auditEvent = AuditEventSupport.CreateAuditEvent(
                    MarketsModuleCode,
                    MarketsModuleName,
                    MarketEntityType,
                    market.Id,
                    AuditEventSupport.FormalCloseActionType,
                    market.Name,
                    reason is null
                        ? $"Cierre formal registrado para el mercado {market.Name}."
                        : $"Cierre formal registrado para el mercado {market.Name}. Motivo: {reason}.",
                    currentStatus.StatusCode,
                    market.Borough,
                    "/markets",
                    isCloseEvent: true,
                    metadata: new
                    {
                        reason,
                        secretaryGeneralName = market.SecretaryGeneralName
                    });

                dbContext.AuditEvents.Add(auditEvent);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Ok(
                    new CloseRecordResponse(
                        auditEvent.Id,
                        MarketsModuleCode,
                        MarketsModuleName,
                        MarketEntityType,
                        market.Id.ToString(),
                        currentStatus.StatusCode,
                        currentStatus.StatusName,
                        auditEvent.OccurredUtc,
                        reason));
            });

        group.MapPost(
            string.Empty,
            async (CreateMarketRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var errors = ValidateCreateMarketRequest(request);
                var marketStatus = await ResolveModuleStatusAsync(
                    request.StatusCatalogEntryId,
                    MarketContextCode,
                    dbContext,
                    cancellationToken);

                if (marketStatus is null)
                {
                    errors["statusCatalogEntryId"] = ["The selected market status does not exist."];
                }

                Contact? secretaryGeneralContact = null;
                if (request.SecretaryGeneralContactId is Guid secretaryGeneralContactId)
                {
                    secretaryGeneralContact = await dbContext.Contacts
                        .SingleOrDefaultAsync(item => item.Id == secretaryGeneralContactId, cancellationToken);

                    if (secretaryGeneralContact is null)
                    {
                        errors["secretaryGeneralContactId"] = ["The selected secretary general contact does not exist."];
                    }
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var market = new Market(
                    request.Name,
                    request.Borough,
                    request.StatusCatalogEntryId,
                    request.SecretaryGeneralContactId,
                    request.SecretaryGeneralName,
                    request.Notes);

                dbContext.Markets.Add(market);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        MarketsModuleCode,
                        MarketsModuleName,
                        MarketEntityType,
                        market.Id,
                        "CREATED",
                        market.Name,
                        $"Mercado registrado en {market.Borough} con secretario general {market.SecretaryGeneralName}.",
                        marketStatus!.StatusCode,
                        market.Borough,
                        "/markets",
                        metadata: new
                        {
                            market.StatusCatalogEntryId,
                            market.SecretaryGeneralContactId
                        }));

                if (request.SecretaryGeneralContactId is Guid validContactId)
                {
                    dbContext.ContactParticipations.Add(
                        new ContactParticipation(
                            validContactId,
                            MarketsModuleCode,
                            "MARKET_SECRETARY_GENERAL",
                            market.Id.ToString(),
                            "Secretario general",
                            $"Mercado vinculado: {market.Name}"));
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var response = new MarketSummaryResponse(
                    market.Id,
                    market.Name,
                    market.Borough,
                    market.StatusCatalogEntryId,
                    marketStatus!.StatusCode,
                    marketStatus.StatusName,
                    marketStatus.IsClosed,
                    marketStatus.AlertsEnabledByDefault,
                    market.SecretaryGeneralContactId,
                    market.SecretaryGeneralName,
                    market.Notes,
                    0,
                    0,
                    0,
                    market.CreatedUtc,
                    market.UpdatedUtc);

                return Results.Created($"/api/markets/{market.Id}", response);
            });

        group.MapGet(
            "/{marketId:guid}/tenants",
            async (Guid marketId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var market = await dbContext.Markets
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == marketId, cancellationToken);

                if (market is null)
                {
                    return Results.NotFound();
                }

                var tenants = await dbContext.MarketTenants
                    .AsNoTracking()
                    .Where(item => item.MarketId == marketId)
                    .OrderBy(item => item.CertificateValidityTo)
                    .ThenBy(item => item.TenantName)
                    .ToListAsync(cancellationToken);

                var response = tenants
                    .Select(item => MapMarketTenantResponse(item, market))
                    .ToList();

                return Results.Ok(response);
            });

        group.MapPost(
            "/{marketId:guid}/tenants",
            async ([FromRoute] Guid marketId, [FromForm] CreateMarketTenantRequest request, PlatformDbContext dbContext, IMarketTenantCertificateStorage certificateStorage, CancellationToken cancellationToken) =>
            {
                var market = await dbContext.Markets
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == marketId, cancellationToken);

                if (market is null)
                {
                    return Results.NotFound();
                }

                if (StateTransitionSupport.IsTerminal(market.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        new
                        {
                            message = StateTransitionSupport.BuildTerminalMutationMessage(
                                $"el mercado {market.Name}",
                                "registrar un locatario",
                                market.StatusCatalogEntry!)
                        });
                }

                var errors = ValidateCreateMarketTenantRequest(request);

                if (request.ContactId is Guid contactId)
                {
                    var contactExists = await dbContext.Contacts
                        .AnyAsync(item => item.Id == contactId, cancellationToken);

                    if (!contactExists)
                    {
                        errors["contactId"] = ["The selected tenant contact does not exist."];
                    }
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var tenantId = Guid.NewGuid();
                await using var certificateContent = request.CertificateFile!.OpenReadStream();
                var storedCertificate = await certificateStorage.SaveAsync(
                    tenantId,
                    request.CertificateFile.FileName,
                    request.CertificateFile.ContentType,
                    certificateContent,
                    cancellationToken);

                try
                {
                    var tenant = new MarketTenant(
                        marketId,
                        request.ContactId,
                        request.TenantName,
                        request.CertificateNumber,
                        request.CertificateValidityTo,
                        request.BusinessLine,
                        request.MobilePhone,
                        request.WhatsAppPhone,
                        request.Email,
                        request.Notes,
                        storedCertificate.OriginalFileName,
                        storedCertificate.RelativePath,
                        storedCertificate.ContentType,
                        storedCertificate.SizeBytes,
                        storedCertificate.UploadedUtc,
                        tenantId);

                    dbContext.MarketTenants.Add(tenant);
                    dbContext.StoredDocuments.Add(
                        StoredDocumentSupport.CreateStoredDocument(
                            MarketsModuleCode,
                            MarketTenantCertificateAreaCode,
                            MarketTenantEntityType,
                            tenant.Id,
                            storedCertificate.OriginalFileName,
                            storedCertificate.RelativePath,
                            storedCertificate.ContentType,
                            storedCertificate.SizeBytes,
                            storedCertificate.UploadedUtc,
                            storedCertificate.Sha256Hex));
                    dbContext.AuditEvents.Add(
                        AuditEventSupport.CreateAuditEvent(
                            MarketsModuleCode,
                            MarketsModuleName,
                            MarketTenantEntityType,
                            tenant.Id,
                            "REGISTERED",
                            tenant.TenantName,
                            $"Locatario registrado en {market.Name} con cédula {tenant.CertificateNumber} y giro {tenant.BusinessLine}.",
                            null,
                            tenant.CertificateNumber,
                            "/markets",
                            metadata: new
                            {
                                tenant.MarketId,
                                tenant.ContactId,
                                tenant.CertificateValidityTo
                            }));

                    if (request.ContactId is Guid validContactId)
                    {
                        dbContext.ContactParticipations.Add(
                            new ContactParticipation(
                                validContactId,
                                MarketsModuleCode,
                                "MARKET_TENANT",
                                tenant.Id.ToString(),
                                "Locatario",
                                $"Mercado vinculado: {market.Name}"));
                    }

                    await dbContext.SaveChangesAsync(cancellationToken);

                    var response = MapMarketTenantResponse(tenant, market);
                    return Results.Created($"/api/markets/{marketId}/tenants/{tenant.Id}", response);
                }
                catch
                {
                    try
                    {
                        await certificateStorage.DeleteIfExistsAsync(storedCertificate.RelativePath, cancellationToken);
                    }
                    catch
                    {
                    }

                    throw;
                }
            })
            .DisableAntiforgery();

        group.MapGet(
            "/{marketId:guid}/issues",
            async (Guid marketId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var marketExists = await dbContext.Markets
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == marketId, cancellationToken);

                if (!marketExists)
                {
                    return Results.NotFound();
                }

                var issues = await dbContext.MarketIssues
                    .AsNoTracking()
                    .Where(item => item.MarketId == marketId)
                    .Include(item => item.StatusCatalogEntry)
                    .OrderByDescending(item => item.IssueDate)
                    .ThenByDescending(item => item.CreatedUtc)
                    .ToListAsync(cancellationToken);

                var response = issues
                    .Select(MapMarketIssueResponse)
                    .ToList();

                return Results.Ok(response);
            });

        group.MapPost(
            "/{marketId:guid}/issues",
            async (Guid marketId, CreateMarketIssueRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var market = await dbContext.Markets
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == marketId, cancellationToken);

                if (market is null)
                {
                    return Results.NotFound();
                }

                if (StateTransitionSupport.IsTerminal(market.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        new
                        {
                            message = StateTransitionSupport.BuildTerminalMutationMessage(
                                $"el mercado {market.Name}",
                                "registrar una incidencia",
                                market.StatusCatalogEntry!)
                        });
                }

                var errors = ValidateCreateMarketIssueRequest(request);
                var issueStatus = await ResolveModuleStatusAsync(
                    request.StatusCatalogEntryId,
                    MarketIssueContextCode,
                    dbContext,
                    cancellationToken);

                if (issueStatus is null)
                {
                    errors["statusCatalogEntryId"] = ["The selected issue status does not exist."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var issue = new MarketIssue(
                    marketId,
                    request.IssueType,
                    request.Description,
                    request.IssueDate,
                    request.AdvanceSummary,
                    request.StatusCatalogEntryId,
                    request.FollowUpOrResolution,
                    request.FinalSatisfaction);

                dbContext.MarketIssues.Add(issue);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        MarketsModuleCode,
                        MarketsModuleName,
                        MarketIssueEntityType,
                        issue.Id,
                        "REGISTERED",
                        issue.IssueType,
                        $"Incidencia registrada para {market.Name}: {issue.Description}.",
                        issueStatus!.StatusCode,
                        market.Name,
                        "/markets",
                        metadata: new
                        {
                            issue.MarketId,
                            issue.IssueDate
                        }));
                await dbContext.SaveChangesAsync(cancellationToken);

                var response = new MarketIssueResponse(
                    issue.Id,
                    issue.MarketId,
                    issue.IssueType,
                    issue.Description,
                    issue.IssueDate,
                    issue.AdvanceSummary,
                    issue.StatusCatalogEntryId,
                    issueStatus!.StatusCode,
                    issueStatus.StatusName,
                    issueStatus.IsClosed,
                    issue.FollowUpOrResolution,
                    issue.FinalSatisfaction,
                    issue.CreatedUtc);

                return Results.Created($"/api/markets/{marketId}/issues/{issue.Id}", response);
            });

        return app;
    }

    private static async Task<MarketDetailResponse> BuildMarketDetailAsync(
        PlatformDbContext dbContext,
        Market market,
        CancellationToken cancellationToken)
    {
        var tenants = await dbContext.MarketTenants
            .AsNoTracking()
            .Where(item => item.MarketId == market.Id)
            .OrderBy(item => item.CertificateValidityTo)
            .ThenBy(item => item.TenantName)
            .ToListAsync(cancellationToken);

        var issues = await dbContext.MarketIssues
            .AsNoTracking()
            .Where(item => item.MarketId == market.Id)
            .Include(item => item.StatusCatalogEntry)
            .OrderByDescending(item => item.IssueDate)
            .ThenByDescending(item => item.CreatedUtc)
            .ToListAsync(cancellationToken);

        return new MarketDetailResponse(
            market.Id,
            market.Name,
            market.Borough,
            market.StatusCatalogEntryId,
            market.StatusCatalogEntry!.StatusCode,
            market.StatusCatalogEntry.StatusName,
            market.StatusCatalogEntry.IsClosed,
            market.StatusCatalogEntry.AlertsEnabledByDefault,
            market.SecretaryGeneralContactId,
            market.SecretaryGeneralName,
            market.Notes,
            market.CreatedUtc,
            market.UpdatedUtc,
            tenants.Select(item => MapMarketTenantResponse(item, market)).ToList(),
            issues.Select(MapMarketIssueResponse).ToList());
    }

    private static async Task<List<MarketTenantAlertResponse>> BuildTenantAlertsAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var markets = await dbContext.Markets
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .ToListAsync(cancellationToken);

        var activeMarkets = markets
            .Where(item => !AreAlertsSuppressed(item.StatusCatalogEntry!))
            .ToDictionary(item => item.Id, item => item);

        if (activeMarkets.Count == 0)
        {
            return [];
        }

        var tenants = await dbContext.MarketTenants
            .AsNoTracking()
            .Where(item => activeMarkets.Keys.Contains(item.MarketId))
            .ToListAsync(cancellationToken);

        return tenants
            .Select(item =>
            {
                var market = activeMarkets[item.MarketId];
                var daysUntilExpiration = GetDaysUntilExpiration(item.CertificateValidityTo);
                var alertState = GetCertificateAlertState(daysUntilExpiration, alertsSuppressed: false);

                return new
                {
                    Tenant = item,
                    Market = market,
                    DaysUntilExpiration = daysUntilExpiration,
                    AlertState = alertState
                };
            })
            .Where(item => item.AlertState is DueSoonAlertState or ExpiredAlertState)
            .OrderBy(item => item.DaysUntilExpiration)
            .ThenBy(item => item.Market.Name)
            .ThenBy(item => item.Tenant.TenantName)
            .Select(item => new MarketTenantAlertResponse(
                item.Market.Id,
                item.Market.Name,
                item.Market.StatusCatalogEntry!.StatusCode,
                item.Tenant.Id,
                item.Tenant.TenantName,
                item.Tenant.CertificateNumber,
                item.Tenant.CertificateValidityTo,
                item.DaysUntilExpiration,
                item.AlertState))
            .ToList();
    }

    private static MarketTenantResponse MapMarketTenantResponse(MarketTenant tenant, Market market)
    {
        var alertsSuppressed = AreAlertsSuppressed(market.StatusCatalogEntry!);
        var daysUntilExpiration = GetDaysUntilExpiration(tenant.CertificateValidityTo);
        var alertState = GetCertificateAlertState(daysUntilExpiration, alertsSuppressed);

        return new MarketTenantResponse(
            tenant.Id,
            tenant.MarketId,
            tenant.ContactId,
            tenant.TenantName,
            tenant.CertificateNumber,
            tenant.CertificateValidityTo,
            tenant.BusinessLine,
            tenant.MobilePhone,
            tenant.WhatsAppPhone,
            tenant.Email,
            tenant.Notes,
            true,
            tenant.CertificateOriginalFileName,
            tenant.CertificateContentType,
            tenant.CertificateFileSizeBytes,
            tenant.CertificateUploadedUtc,
            alertState,
            daysUntilExpiration,
            alertsSuppressed,
            tenant.CreatedUtc);
    }

    private static MarketIssueResponse MapMarketIssueResponse(MarketIssue issue)
    {
        return new MarketIssueResponse(
            issue.Id,
            issue.MarketId,
            issue.IssueType,
            issue.Description,
            issue.IssueDate,
            issue.AdvanceSummary,
            issue.StatusCatalogEntryId,
            issue.StatusCatalogEntry!.StatusCode,
            issue.StatusCatalogEntry.StatusName,
            issue.StatusCatalogEntry.IsClosed,
            issue.FollowUpOrResolution,
            issue.FinalSatisfaction,
            issue.CreatedUtc);
    }

    private static int CountActiveTenantAlerts(Market market, IEnumerable<MarketTenant> tenants)
    {
        if (AreAlertsSuppressed(market.StatusCatalogEntry!))
        {
            return 0;
        }

        return tenants.Count(item =>
        {
            var daysUntilExpiration = GetDaysUntilExpiration(item.CertificateValidityTo);
            return daysUntilExpiration <= 30;
        });
    }

    private static bool AreAlertsSuppressed(ModuleStatusCatalogEntry marketStatus)
    {
        return marketStatus.IsClosed || !marketStatus.AlertsEnabledByDefault;
    }

    private static int GetDaysUntilExpiration(DateOnly certificateValidityTo)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return certificateValidityTo.DayNumber - today.DayNumber;
    }

    private static string GetCertificateAlertState(int daysUntilExpiration, bool alertsSuppressed)
    {
        if (alertsSuppressed)
        {
            return AlertsDisabledState;
        }

        if (daysUntilExpiration < 0)
        {
            return ExpiredAlertState;
        }

        if (daysUntilExpiration <= 30)
        {
            return DueSoonAlertState;
        }

        return ValidAlertState;
    }

    private static async Task<ModuleStatusCatalogEntry?> ResolveModuleStatusAsync(
        int statusCatalogEntryId,
        string contextCode,
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.ModuleStatusCatalogEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == statusCatalogEntryId
                    && item.ModuleCode == MarketsModuleCode
                    && item.ContextCode == contextCode,
                cancellationToken);
    }

    private static Dictionary<string, string[]> ValidateCreateMarketRequest(CreateMarketRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["name"] = ["Name is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Borough))
        {
            errors["borough"] = ["Borough is required."];
        }

        if (request.StatusCatalogEntryId <= 0)
        {
            errors["statusCatalogEntryId"] = ["StatusCatalogEntryId is required."];
        }

        if (string.IsNullOrWhiteSpace(request.SecretaryGeneralName))
        {
            errors["secretaryGeneralName"] = ["SecretaryGeneralName is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateCreateMarketTenantRequest(CreateMarketTenantRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.TenantName))
        {
            errors["tenantName"] = ["TenantName is required."];
        }

        if (string.IsNullOrWhiteSpace(request.CertificateNumber))
        {
            errors["certificateNumber"] = ["CertificateNumber is required."];
        }

        if (request.CertificateValidityTo == default)
        {
            errors["certificateValidityTo"] = ["CertificateValidityTo is required."];
        }

        if (string.IsNullOrWhiteSpace(request.BusinessLine))
        {
            errors["businessLine"] = ["BusinessLine is required."];
        }

        if (request.CertificateFile is null || request.CertificateFile.Length <= 0)
        {
            errors["certificateFile"] = ["A digital certificate file is required."];
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && !IsValidEmail(request.Email))
        {
            errors["email"] = ["Email must be a valid email address."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateCreateMarketIssueRequest(CreateMarketIssueRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.IssueType))
        {
            errors["issueType"] = ["IssueType is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors["description"] = ["Description is required."];
        }

        if (request.IssueDate == default)
        {
            errors["issueDate"] = ["IssueDate is required."];
        }

        if (string.IsNullOrWhiteSpace(request.AdvanceSummary))
        {
            errors["advanceSummary"] = ["AdvanceSummary is required."];
        }

        if (request.StatusCatalogEntryId <= 0)
        {
            errors["statusCatalogEntryId"] = ["StatusCatalogEntryId is required."];
        }

        return errors;
    }

    private static bool IsValidEmail(string value)
    {
        try
        {
            _ = new MailAddress(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
