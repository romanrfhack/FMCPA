using System.Globalization;
using System.Text;
using FMCPA.Api.Auth;
using FMCPA.Api.Contracts.Closeout;
using FMCPA.Api.Contracts.Financials;
using FMCPA.Api.Extensions;
using FMCPA.Domain.Entities.Financials;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class FinancialsEndpoints
{
    private const string FinancialsModuleCode = "FINANCIALS";
    private const string FinancialsModuleName = "Financieras";
    private const string FinancialPermitContextCode = "FINANCIAL_PERMIT";
    private const string FinancialPermitEntityType = "FINANCIAL_PERMIT";
    private const string FinancialCreditEntityType = "FINANCIAL_CREDIT";
    private const string FinancialCommissionEntityType = "FINANCIAL_CREDIT_COMMISSION";
    private const string AcceptedStatusCode = "ACCEPTED";
    private const string RejectedStatusCode = "REJECTED";
    private const string InProcessStatusCode = "IN_PROCESS";
    private const string RenewStatusCode = "RENEW";
    private const string ClosedStatusCode = "CLOSED";
    private const string AdministrationCommissionTypeCode = "ADMINISTRATION";
    private const string PromoterCommissionTypeCode = "PROMOTER";
    private const string NotCurrentPermitOperationReasonCode = "FINANCIAL_PERMIT_NOT_CURRENT";
    private const string TerminalPermitOperationReasonCode = "FINANCIAL_PERMIT_TERMINAL";
    private const string ActivePermitConflictReasonCode = "FINANCIAL_PERMIT_ACTIVE_CONFLICT";
    private const string CurrentPermitAmbiguousReasonCode = "FINANCIAL_PERMIT_CURRENT_AMBIGUOUS";
    private const string UseCurrentPermitSuggestionCode = "USE_CURRENT_PERMIT";
    private const string RenewLastPermitSuggestionCode = "RENEW_LAST_PERMIT";
    private const string CreateNewPermitSuggestionCode = "CREATE_NEW_PERMIT";
    private const string ReviewTerminalChainSuggestionCode = "REVIEW_TERMINAL_CHAIN";
    private const string CaptureCreditAction = "CAPTURE_CREDIT";
    private const string PrepareRenewalAction = "PREPARE_RENEWAL";
    private const string CreatePermitAction = "CREATE_PERMIT";
    private const string ViewChainAction = "VIEW_CHAIN";
    private const string ViewCurrentPermitAction = "VIEW_CURRENT_PERMIT";
    private const string DueSoonAlertState = "DUE_SOON";
    private const string ExpiredAlertState = "EXPIRED";
    private const string RenewalAlertState = "RENEWAL";
    private const string ValidAlertState = "VALID";
    private const string AlertsDisabledState = "ALERTS_DISABLED";
    private const string HistoricalAlertState = "HISTORICAL";
    private static readonly HashSet<string> AllowedRecipientCategories =
    [
        "COMPANY",
        "THIRD_PARTY",
        "OTHER_PARTICIPANT"
    ];

    public static IEndpointRouteBuilder MapFinancialsEndpoints(this IEndpointRouteBuilder app)
    {
        var readGroup = app.MapGroup("/api/financials")
            .WithTags("Financials")
            .RequireFinancialsReadAccess();

        var writeGroup = app.MapGroup("/api/financials")
            .WithTags("Financials")
            .RequireFinancialsWriteAccess();

        var adminGroup = app.MapGroup("/api/financials")
            .WithTags("Financials")
            .RequireFinancialsFormalCloseAccess();

        readGroup.MapGet(
            "/alerts/permits",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var alerts = await BuildPermitAlertsAsync(dbContext, cancellationToken);
                return Results.Ok(alerts);
            });

        readGroup.MapGet(
            string.Empty,
            async (string? statusCode, bool? alertsOnly, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var permits = await dbContext.FinancialPermits
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .OrderByDescending(item => item.IsCurrentVersion)
                    .ThenBy(item => item.ValidTo)
                    .ThenBy(item => item.FinancialName)
                    .ToListAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(statusCode))
                {
                    var normalizedStatusCode = statusCode.Trim().ToUpperInvariant();
                    permits = permits
                        .Where(item => item.StatusCatalogEntry?.StatusCode == normalizedStatusCode)
                        .ToList();
                }

                var permitIds = permits.Select(item => item.Id).ToArray();
                var credits = permitIds.Length == 0
                    ? []
                    : await dbContext.FinancialCredits
                        .AsNoTracking()
                        .Where(item => permitIds.Contains(item.FinancialPermitId))
                        .ToListAsync(cancellationToken);

                var creditIds = credits.Select(item => item.Id).ToArray();
                var commissionCounts = creditIds.Length == 0
                    ? new Dictionary<Guid, int>()
                    : await dbContext.FinancialCreditCommissions
                        .AsNoTracking()
                        .Where(item => creditIds.Contains(item.FinancialCreditId))
                        .GroupBy(item => item.FinancialCreditId)
                        .Select(grouping => new { FinancialCreditId = grouping.Key, Count = grouping.Count() })
                        .ToDictionaryAsync(item => item.FinancialCreditId, item => item.Count, cancellationToken);

                var summaries = permits
                    .Select(permit =>
                    {
                        var permitCredits = credits.Where(item => item.FinancialPermitId == permit.Id).ToList();
                        var daysUntilExpiration = GetDaysUntilExpiration(permit.ValidTo);
                        var alertState = GetPermitAlertState(permit, daysUntilExpiration);

                        return new FinancialPermitSummaryResponse(
                            permit.Id,
                            permit.FinancialName,
                            permit.InstitutionOrDependency,
                            permit.PlaceOrStand,
                            permit.ValidFrom,
                            permit.ValidTo,
                            permit.Schedule,
                            permit.NegotiatedTerms,
                            permit.StatusCatalogEntryId,
                            permit.StatusCatalogEntry!.StatusCode,
                            permit.StatusCatalogEntry.StatusName,
                            permit.StatusCatalogEntry.IsClosed,
                            permit.StatusCatalogEntry.AlertsEnabledByDefault,
                            daysUntilExpiration,
                            alertState,
                            permit.RenewedFromPermitId,
                            permit.CurrentRootPermitId,
                            permit.IsCurrentVersion,
                            permit.RenewalSequence,
                            permitCredits.Count,
                            permitCredits.Sum(item => commissionCounts.GetValueOrDefault(item.Id)),
                            permit.Notes,
                            permit.CreatedUtc,
                            permit.UpdatedUtc);
                    })
                    .ToList();

                if (alertsOnly == true)
                {
                    summaries = summaries
                        .Where(item => item.AlertState is DueSoonAlertState or ExpiredAlertState or RenewalAlertState)
                        .ToList();
                }

                return Results.Ok(summaries);
            });

        readGroup.MapGet(
            "/current-permit",
            async (
                string? financialName,
                string? institutionOrDependency,
                string? placeOrStand,
                PlatformDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var errors = ValidateCurrentPermitResolutionRequest(financialName, institutionOrDependency, placeOrStand);
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var resolutionResult = await BuildFinancialPermitContextResolutionResultAsync(
                    dbContext,
                    financialName!,
                    institutionOrDependency!,
                    placeOrStand!,
                    cancellationToken);

                if (resolutionResult.AmbiguousResponse is not null)
                {
                    return Results.Conflict(resolutionResult.AmbiguousResponse);
                }

                return Results.Ok(resolutionResult.Resolution);
            });

        readGroup.MapGet(
            "/context-card",
            async (
                string? financialName,
                string? institutionOrDependency,
                string? placeOrStand,
                PlatformDbContext dbContext,
                CancellationToken cancellationToken) =>
            {
                var errors = ValidateCurrentPermitResolutionRequest(financialName, institutionOrDependency, placeOrStand);
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var resolutionResult = await BuildFinancialPermitContextResolutionResultAsync(
                    dbContext,
                    financialName!,
                    institutionOrDependency!,
                    placeOrStand!,
                    cancellationToken);

                if (resolutionResult.AmbiguousResponse is not null)
                {
                    return Results.Conflict(resolutionResult.AmbiguousResponse);
                }

                var card = await BuildFinancialContextCardAsync(
                    dbContext,
                    resolutionResult.Resolution!,
                    cancellationToken);

                return Results.Ok(card);
            });

        readGroup.MapGet(
            "/{permitId:guid}",
            async (Guid permitId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var permit = await dbContext.FinancialPermits
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == permitId, cancellationToken);

                if (permit is null)
                {
                    return Results.NotFound();
                }

                var detail = await BuildPermitDetailAsync(dbContext, permit, cancellationToken);
                return Results.Ok(detail);
            });

        readGroup.MapGet(
            "/{permitId:guid}/renewal-chain",
            async (Guid permitId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var permit = await dbContext.FinancialPermits
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == permitId, cancellationToken);

                if (permit is null)
                {
                    return Results.NotFound();
                }

                var chain = await BuildPermitRenewalChainAsync(dbContext, permit, cancellationToken);
                return Results.Ok(chain);
            });

        readGroup.MapGet(
            "/{permitId:guid}/renewal-draft",
            async (Guid permitId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var sourcePermit = await dbContext.FinancialPermits
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == permitId, cancellationToken);

                if (sourcePermit is null)
                {
                    return Results.NotFound();
                }

                if (StateTransitionSupport.IsTerminal(sourcePermit.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        BuildTerminalPermitOperationBlockedResponse(
                            sourcePermit,
                            "preparar una renovacion"));
                }

                var currentRootPermitId = GetCurrentRootPermitId(sourcePermit);
                var renewalTargetPermit = await FindCurrentPermitInChainAsync(
                    dbContext,
                    currentRootPermitId,
                    cancellationToken);
                if (renewalTargetPermit is null)
                {
                    return Results.Conflict(
                        new
                        {
                            message = "No es posible preparar la renovacion porque la cadena no tiene un oficio vigente identificado.",
                            reasonCode = NotCurrentPermitOperationReasonCode,
                            permitId = sourcePermit.Id,
                            currentRootPermitId,
                            currentPermitId = (Guid?)null
                        });
                }

                if (StateTransitionSupport.IsTerminal(renewalTargetPermit.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        BuildTerminalPermitOperationBlockedResponse(
                            renewalTargetPermit,
                            "preparar una renovacion"));
                }

                return Results.Ok(BuildFinancialPermitRenewalDraftResponse(sourcePermit, renewalTargetPermit));
            });

        adminGroup.MapPost(
            "/{permitId:guid}/close",
            async (Guid permitId, CloseRecordRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var permit = await dbContext.FinancialPermits
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == permitId, cancellationToken);

                if (permit is null)
                {
                    return Results.NotFound();
                }

                if (await AuditEventSupport.HasCloseEventAsync(dbContext, FinancialsModuleCode, FinancialPermitEntityType, permit.Id, cancellationToken))
                {
                    return Results.Conflict(new { message = StateTransitionSupport.BuildDuplicateCloseEventMessage("el oficio") });
                }

                var currentStatus = permit.StatusCatalogEntry!;
                if (StateTransitionSupport.IsTerminal(currentStatus))
                {
                    return Results.Conflict(new { message = StateTransitionSupport.BuildTerminalCloseMessage("el oficio", currentStatus) });
                }

                if (!currentStatus.IsClosed)
                {
                    var closedStatus = await AuditEventSupport.ResolveContextStatusByCodeAsync(
                        dbContext,
                        FinancialsModuleCode,
                        FinancialPermitContextCode,
                        ClosedStatusCode,
                        cancellationToken);

                    if (closedStatus is null)
                    {
                        return Results.ValidationProblem(
                            new Dictionary<string, string[]>
                            {
                                ["statusCatalogEntryId"] = ["The closed status for financial permits is not configured."]
                            });
                    }

                    permit.SyncStatus(closedStatus.Id);
                    currentStatus = closedStatus;
                }

                var reason = NormalizeOptionalText(request.Reason);
                var auditEvent = AuditEventSupport.CreateAuditEvent(
                    FinancialsModuleCode,
                    FinancialsModuleName,
                    FinancialPermitEntityType,
                    permit.Id,
                    AuditEventSupport.FormalCloseActionType,
                    permit.FinancialName,
                    reason is null
                        ? $"Cierre formal registrado para el oficio de {permit.FinancialName}."
                        : $"Cierre formal registrado para el oficio de {permit.FinancialName}. Motivo: {reason}.",
                    currentStatus.StatusCode,
                    permit.PlaceOrStand,
                    "/financials",
                    isCloseEvent: true,
                    metadata: new
                    {
                        reason,
                        permit.InstitutionOrDependency,
                        permit.ValidTo
                    });

                dbContext.AuditEvents.Add(auditEvent);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Ok(
                    new CloseRecordResponse(
                        auditEvent.Id,
                        FinancialsModuleCode,
                        FinancialsModuleName,
                        FinancialPermitEntityType,
                        permit.Id.ToString(),
                        currentStatus.StatusCode,
                        currentStatus.StatusName,
                        auditEvent.OccurredUtc,
                        reason));
            });

        writeGroup.MapPost(
            string.Empty,
            async (CreateFinancialPermitRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var errors = ValidateCreateFinancialPermitRequest(request);
                var permitStatus = await ResolveModuleStatusAsync(request.StatusCatalogEntryId, dbContext, cancellationToken);
                if (permitStatus is null)
                {
                    errors["statusCatalogEntryId"] = ["The selected financial permit status does not exist."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                if (!StateTransitionSupport.IsTerminal(permitStatus))
                {
                    var activeConflict = await FindActiveOperationalPermitConflictAsync(
                        dbContext,
                        request.FinancialName,
                        request.InstitutionOrDependency,
                        request.PlaceOrStand,
                        excludedRootPermitId: null,
                        cancellationToken);
                    if (activeConflict is not null)
                    {
                        return Results.Conflict(BuildActivePermitConflictResponse(activeConflict, "registrar el oficio"));
                    }
                }

                var permit = new FinancialPermit(
                    request.FinancialName,
                    request.InstitutionOrDependency,
                    request.PlaceOrStand,
                    request.ValidFrom,
                    request.ValidTo,
                    request.Schedule,
                    request.NegotiatedTerms,
                    request.StatusCatalogEntryId,
                    request.Notes);

                dbContext.FinancialPermits.Add(permit);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        FinancialsModuleCode,
                        FinancialsModuleName,
                        FinancialPermitEntityType,
                        permit.Id,
                        "CREATED",
                        permit.FinancialName,
                        $"Oficio o autorización registrada para {permit.PlaceOrStand}.",
                        permitStatus!.StatusCode,
                        permit.InstitutionOrDependency,
                        "/financials",
                        metadata: new
                        {
                            permit.ValidFrom,
                            permit.ValidTo
                        }));
                await dbContext.SaveChangesAsync(cancellationToken);

                var daysUntilExpiration = GetDaysUntilExpiration(permit.ValidTo);
                var response = new FinancialPermitSummaryResponse(
                    permit.Id,
                    permit.FinancialName,
                    permit.InstitutionOrDependency,
                    permit.PlaceOrStand,
                    permit.ValidFrom,
                    permit.ValidTo,
                    permit.Schedule,
                    permit.NegotiatedTerms,
                    permit.StatusCatalogEntryId,
                    permitStatus!.StatusCode,
                    permitStatus.StatusName,
                    permitStatus.IsClosed,
                    permitStatus.AlertsEnabledByDefault,
                    daysUntilExpiration,
                    GetPermitAlertState(permit, daysUntilExpiration, permitStatus),
                    permit.RenewedFromPermitId,
                    permit.CurrentRootPermitId,
                    permit.IsCurrentVersion,
                    permit.RenewalSequence,
                    0,
                    0,
                    permit.Notes,
                    permit.CreatedUtc,
                    permit.UpdatedUtc);

                return Results.Created($"/api/financials/{permit.Id}", response);
            });

        writeGroup.MapPost(
            "/{permitId:guid}/renew",
            async (Guid permitId, RenewFinancialPermitRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var previousPermit = await dbContext.FinancialPermits
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == permitId, cancellationToken);

                if (previousPermit is null)
                {
                    return Results.NotFound();
                }

                if (!previousPermit.IsCurrentVersion)
                {
                    return Results.Conflict(
                        new
                        {
                            message = "No es posible renovar un oficio historico. Selecciona el oficio vigente de la cadena."
                        });
                }

                if (StateTransitionSupport.IsTerminal(previousPermit.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        new
                        {
                            message = StateTransitionSupport.BuildTerminalMutationMessage(
                                $"el oficio de {previousPermit.FinancialName}",
                                "renovar el oficio",
                                previousPermit.StatusCatalogEntry!)
                        });
                }

                var errors = ValidateRenewFinancialPermitRequest(request);
                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var placeOrStand = NormalizeOptionalText(request.PlaceOrStand) ?? previousPermit.PlaceOrStand;
                var schedule = NormalizeOptionalText(request.Schedule) ?? previousPermit.Schedule;
                var negotiatedTerms = NormalizeOptionalText(request.NegotiatedTerms) ?? previousPermit.NegotiatedTerms;
                var notes = NormalizeOptionalText(request.Notes);

                var renewalConflict = await FindActiveOperationalPermitConflictAsync(
                    dbContext,
                    previousPermit.FinancialName,
                    previousPermit.InstitutionOrDependency,
                    placeOrStand,
                    GetCurrentRootPermitId(previousPermit),
                    cancellationToken);
                if (renewalConflict is not null)
                {
                    return Results.Conflict(BuildActivePermitConflictResponse(renewalConflict, "renovar el oficio"));
                }

                var renewedPermit = new FinancialPermit(
                    previousPermit.FinancialName,
                    previousPermit.InstitutionOrDependency,
                    placeOrStand,
                    request.ValidFrom,
                    request.ValidTo,
                    schedule,
                    negotiatedTerms,
                    previousPermit.StatusCatalogEntryId,
                    notes);
                renewedPermit.LinkAsRenewalOf(previousPermit);
                previousPermit.MarkRenewed();

                dbContext.FinancialPermits.Add(renewedPermit);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        FinancialsModuleCode,
                        FinancialsModuleName,
                        FinancialPermitEntityType,
                        renewedPermit.Id,
                        "FINANCIAL_PERMIT_RENEWED",
                        renewedPermit.FinancialName,
                        $"Oficio renovado desde el permiso {previousPermit.Id} con nueva vigencia de {renewedPermit.ValidFrom} a {renewedPermit.ValidTo}.",
                        previousPermit.StatusCatalogEntry!.StatusCode,
                        renewedPermit.PlaceOrStand,
                        "/financials",
                        metadata: new
                        {
                            renewedFromPermitId = previousPermit.Id,
                            renewedPermitId = renewedPermit.Id,
                            renewedPermit.CurrentRootPermitId,
                            renewedPermit.RenewalSequence,
                            renewedPermit.ValidFrom,
                            renewedPermit.ValidTo
                        }));

                await dbContext.SaveChangesAsync(cancellationToken);

                var responsePermit = await dbContext.FinancialPermits
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .SingleAsync(item => item.Id == renewedPermit.Id, cancellationToken);
                var response = await BuildPermitDetailAsync(dbContext, responsePermit, cancellationToken);

                return Results.Created($"/api/financials/{renewedPermit.Id}", response);
            });

        readGroup.MapGet(
            "/{permitId:guid}/credits",
            async (Guid permitId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var permitExists = await dbContext.FinancialPermits
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == permitId, cancellationToken);

                if (!permitExists)
                {
                    return Results.NotFound();
                }

                var credits = await dbContext.FinancialCredits
                    .AsNoTracking()
                    .Where(item => item.FinancialPermitId == permitId)
                    .OrderByDescending(item => item.AuthorizationDate)
                    .ThenByDescending(item => item.CreatedUtc)
                    .ToListAsync(cancellationToken);

                var commissionLookup = await BuildCommissionLookupAsync(dbContext, credits, cancellationToken);
                var response = credits
                    .Select(item => MapFinancialCreditResponse(item, commissionLookup.GetValueOrDefault(item.Id) ?? []))
                    .ToList();

                return Results.Ok(response);
            });

        writeGroup.MapPost(
            "/{permitId:guid}/credits",
            async (Guid permitId, CreateFinancialCreditRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var permit = await dbContext.FinancialPermits
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == permitId, cancellationToken);

                if (permit is null)
                {
                    return Results.NotFound();
                }

                if (!permit.IsCurrentVersion)
                {
                    return Results.Conflict(
                        await BuildNotCurrentPermitOperationBlockedResponseAsync(
                            dbContext,
                            permit,
                            "registrar un crédito",
                            cancellationToken));
                }

                if (StateTransitionSupport.IsTerminal(permit.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        BuildTerminalPermitOperationBlockedResponse(
                            permit,
                            "registrar un crédito"));
                }

                var errors = ValidateCreateFinancialCreditRequest(request);

                if (request.PromoterContactId is Guid promoterContactId)
                {
                    var promoterExists = await dbContext.Contacts
                        .AnyAsync(item => item.Id == promoterContactId, cancellationToken);

                    if (!promoterExists)
                    {
                        errors["promoterContactId"] = ["The selected promoter contact does not exist."];
                    }
                }

                if (request.BeneficiaryContactId is Guid beneficiaryContactId)
                {
                    var beneficiaryExists = await dbContext.Contacts
                        .AnyAsync(item => item.Id == beneficiaryContactId, cancellationToken);

                    if (!beneficiaryExists)
                    {
                        errors["beneficiaryContactId"] = ["The selected beneficiary contact does not exist."];
                    }
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var credit = new FinancialCredit(
                    permitId,
                    request.PromoterContactId,
                    request.PromoterName,
                    request.BeneficiaryContactId,
                    request.BeneficiaryName,
                    request.PhoneNumber,
                    request.WhatsAppPhone,
                    request.AuthorizationDate,
                    request.Amount,
                    request.Notes);

                dbContext.FinancialCredits.Add(credit);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        FinancialsModuleCode,
                        FinancialsModuleName,
                        FinancialCreditEntityType,
                        credit.Id,
                        "REGISTERED",
                        credit.BeneficiaryName,
                        $"Crédito individual registrado por {credit.Amount:0.##} para {permit.FinancialName}.",
                        null,
                        permit.FinancialName,
                        "/financials",
                        metadata: new
                        {
                            credit.FinancialPermitId,
                            credit.AuthorizationDate,
                            credit.PromoterContactId,
                            credit.BeneficiaryContactId
                        }));

                if (request.PromoterContactId is Guid validPromoterContactId)
                {
                    dbContext.ContactParticipations.Add(
                        new ContactParticipation(
                            validPromoterContactId,
                            FinancialsModuleCode,
                            "FINANCIAL_CREDIT_PROMOTER",
                            credit.Id.ToString(),
                            "Promotor",
                            $"Credito vinculado: {permit.FinancialName}"));
                }

                if (request.BeneficiaryContactId is Guid validBeneficiaryContactId)
                {
                    dbContext.ContactParticipations.Add(
                        new ContactParticipation(
                            validBeneficiaryContactId,
                            FinancialsModuleCode,
                            "FINANCIAL_CREDIT_BENEFICIARY",
                            credit.Id.ToString(),
                            "Beneficiario",
                            $"Credito vinculado: {permit.FinancialName}"));
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var response = new FinancialCreditResponse(
                    credit.Id,
                    credit.FinancialPermitId,
                    credit.PromoterContactId,
                    credit.PromoterName,
                    credit.BeneficiaryContactId,
                    credit.BeneficiaryName,
                    credit.PhoneNumber,
                    credit.WhatsAppPhone,
                    credit.AuthorizationDate,
                    credit.Amount,
                    credit.Notes,
                    0,
                    [],
                    credit.CreatedUtc);

                return Results.Created($"/api/financials/{permitId}/credits/{credit.Id}", response);
            });

        readGroup.MapGet(
            "/credits/{creditId:guid}/commissions",
            async (Guid creditId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var creditExists = await dbContext.FinancialCredits
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == creditId, cancellationToken);

                if (!creditExists)
                {
                    return Results.NotFound();
                }

                var commissions = await dbContext.FinancialCreditCommissions
                    .AsNoTracking()
                    .Where(item => item.FinancialCreditId == creditId)
                    .Include(item => item.CommissionType)
                    .OrderByDescending(item => item.CreatedUtc)
                    .ToListAsync(cancellationToken);

                return Results.Ok(commissions.Select(MapFinancialCreditCommissionResponse).ToList());
            });

        writeGroup.MapPost(
            "/credits/{creditId:guid}/commissions",
            async (Guid creditId, CreateFinancialCreditCommissionRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var credit = await dbContext.FinancialCredits
                    .AsNoTracking()
                    .Include(item => item.FinancialPermit)
                    .ThenInclude(item => item!.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == creditId, cancellationToken);

                if (credit is null)
                {
                    return Results.NotFound();
                }

                if (credit.FinancialPermit is not { } permit)
                {
                    return Results.Conflict(
                        new FinancialPermitOperationBlockedResponse(
                            "No es posible registrar una comisión porque el crédito no tiene un oficio válido asociado.",
                            "FINANCIAL_PERMIT_CONTEXT_INVALID",
                            Guid.Empty,
                            Guid.Empty,
                            null));
                }

                if (!permit.IsCurrentVersion)
                {
                    return Results.Conflict(
                        await BuildNotCurrentPermitOperationBlockedResponseAsync(
                            dbContext,
                            permit,
                            "registrar una comisión",
                            cancellationToken));
                }

                if (StateTransitionSupport.IsTerminal(permit.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        BuildTerminalPermitOperationBlockedResponse(
                            permit,
                            "registrar una comisión"));
                }

                var errors = ValidateCreateFinancialCreditCommissionRequest(request);
                var commissionType = await dbContext.CommissionTypes
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == request.CommissionTypeId, cancellationToken);

                if (commissionType is null)
                {
                    errors["commissionTypeId"] = ["The selected commission type does not exist."];
                }

                if (request.RecipientContactId is Guid recipientContactId)
                {
                    var recipientExists = await dbContext.Contacts
                        .AnyAsync(item => item.Id == recipientContactId, cancellationToken);

                    if (!recipientExists)
                    {
                        errors["recipientContactId"] = ["The selected recipient contact does not exist."];
                    }
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var commission = new FinancialCreditCommission(
                    creditId,
                    request.CommissionTypeId,
                    request.RecipientCategory,
                    request.RecipientContactId,
                    request.RecipientName,
                    request.BaseAmount,
                    request.CommissionAmount,
                    request.Notes);

                dbContext.FinancialCreditCommissions.Add(commission);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        FinancialsModuleCode,
                        FinancialsModuleName,
                        FinancialCommissionEntityType,
                        commission.Id,
                        "REGISTERED",
                        commission.RecipientName,
                        $"Comisión registrada por {commission.CommissionAmount:0.##} para el crédito {credit.BeneficiaryName}.",
                        null,
                        credit.BeneficiaryName,
                        "/financials",
                        metadata: new
                        {
                            commission.FinancialCreditId,
                            commission.CommissionTypeId,
                            commission.RecipientCategory
                        }));

                if (request.RecipientContactId is Guid validRecipientContactId)
                {
                    dbContext.ContactParticipations.Add(
                        new ContactParticipation(
                            validRecipientContactId,
                            FinancialsModuleCode,
                            "FINANCIAL_CREDIT_COMMISSION_RECIPIENT",
                            commission.Id.ToString(),
                            "Destinatario de comision",
                            $"Credito vinculado: {credit.BeneficiaryName}"));
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var response = new FinancialCreditCommissionResponse(
                    commission.Id,
                    commission.FinancialCreditId,
                    commission.CommissionTypeId,
                    commissionType!.Code,
                    commissionType.Name,
                    commission.RecipientCategory,
                    commission.RecipientContactId,
                    commission.RecipientName,
                    commission.BaseAmount,
                    commission.CommissionAmount,
                    commission.Notes,
                    commission.CreatedUtc);

                return Results.Created($"/api/financials/credits/{creditId}/commissions/{commission.Id}", response);
            });

        return app;
    }

    private static async Task<FinancialPermitContextResolutionResult> BuildFinancialPermitContextResolutionResultAsync(
        PlatformDbContext dbContext,
        string financialName,
        string institutionOrDependency,
        string placeOrStand,
        CancellationToken cancellationToken)
    {
        var currentMatches = await FindCurrentOperationalPermitMatchesAsync(
            dbContext,
            financialName,
            institutionOrDependency,
            placeOrStand,
            excludedRootPermitId: null,
            cancellationToken);
        var matches = currentMatches
            .Where(item => !StateTransitionSupport.IsTerminal(item.StatusCatalogEntry))
            .ToList();

        var resolvedMatches = matches
            .Select(BuildFinancialPermitContextPermitResponse)
            .ToList();

        if (resolvedMatches.Count > 1)
        {
            return new FinancialPermitContextResolutionResult(
                Resolution: null,
                new FinancialPermitCurrentResolutionAmbiguousResponse(
                    "Se encontraron varios oficios vigentes/no terminales para la misma combinacion operativa. Revisa la cadena o depura datos antes de capturar.",
                    CurrentPermitAmbiguousReasonCode,
                    financialName.Trim(),
                    institutionOrDependency.Trim(),
                    placeOrStand.Trim(),
                    matches
                        .Select(BuildCurrentPermitResolutionResponse)
                        .ToList()));
        }

        if (resolvedMatches.Count == 1)
        {
            var currentPermit = resolvedMatches[0];
            return new FinancialPermitContextResolutionResult(
                BuildFinancialPermitContextResolutionResponse(
                    financialName,
                    institutionOrDependency,
                    placeOrStand,
                    currentPermit,
                    lastKnownPermit: null,
                    UseCurrentPermitSuggestionCode,
                    "Usa el oficio vigente/no terminal encontrado para operar o capturar el crédito.",
                    BuildPermitRouteHint(currentPermit.PermitId, includeChain: false)),
                AmbiguousResponse: null);
        }

        var terminalMatch = currentMatches
            .Where(item => StateTransitionSupport.IsTerminal(item.StatusCatalogEntry))
            .OrderByDescending(item => item.RenewalSequence)
            .ThenByDescending(item => item.CreatedUtc)
            .FirstOrDefault();
        if (terminalMatch is not null)
        {
            var lastKnownPermit = BuildFinancialPermitContextPermitResponse(terminalMatch);
            return new FinancialPermitContextResolutionResult(
                BuildFinancialPermitContextResolutionResponse(
                    financialName,
                    institutionOrDependency,
                    placeOrStand,
                    currentPermit: null,
                    lastKnownPermit,
                    ReviewTerminalChainSuggestionCode,
                    "La cadena encontrada esta en estado terminal. Revisa el ultimo permiso o la cadena antes de decidir si corresponde abrir un nuevo oficio.",
                    BuildPermitRouteHint(lastKnownPermit.PermitId, includeChain: true)),
                AmbiguousResponse: null);
        }

        var historicalMatch = await FindHistoricalOperationalPermitMatchAsync(
            dbContext,
            financialName,
            institutionOrDependency,
            placeOrStand,
            cancellationToken);
        if (historicalMatch is not null)
        {
            var lastKnownPermit = BuildFinancialPermitContextPermitResponse(historicalMatch);
            var suggestionCode = StateTransitionSupport.IsTerminal(historicalMatch.StatusCatalogEntry)
                ? ReviewTerminalChainSuggestionCode
                : RenewLastPermitSuggestionCode;
            var suggestionMessage = suggestionCode == ReviewTerminalChainSuggestionCode
                ? "Solo se encontro antecedente terminal para este contexto. Revisa la cadena antes de crear o renovar un oficio."
                : "No hay oficio vigente para este contexto, pero existe antecedente historico. Abre el ultimo permiso o su cadena y prepara una renovacion operativa si corresponde.";

            return new FinancialPermitContextResolutionResult(
                BuildFinancialPermitContextResolutionResponse(
                    financialName,
                    institutionOrDependency,
                    placeOrStand,
                    currentPermit: null,
                    lastKnownPermit,
                    suggestionCode,
                    suggestionMessage,
                    BuildPermitRouteHint(lastKnownPermit.PermitId, includeChain: true)),
                AmbiguousResponse: null);
        }

        return new FinancialPermitContextResolutionResult(
            BuildFinancialPermitContextResolutionResponse(
                financialName,
                institutionOrDependency,
                placeOrStand,
                currentPermit: null,
                lastKnownPermit: null,
                CreateNewPermitSuggestionCode,
                "No hay oficio vigente ni antecedente para este contexto. Registra un nuevo oficio antes de capturar créditos.",
                "/financials"),
            AmbiguousResponse: null);
    }

    private static async Task<FinancialContextCardResponse> BuildFinancialContextCardAsync(
        PlatformDbContext dbContext,
        FinancialPermitContextResolutionResponse resolution,
        CancellationToken cancellationToken)
    {
        var contextPermitId = resolution.CurrentPermit?.PermitId ?? resolution.LastKnownPermit?.PermitId;
        if (contextPermitId is not Guid permitId)
        {
            return new FinancialContextCardResponse(
                resolution,
                RenewalChainSummary: null,
                CreditSummary: null,
                CommissionSummary: null,
                BuildFinancialContextCardAvailableActions(resolution));
        }

        var permit = await dbContext.FinancialPermits
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .SingleOrDefaultAsync(item => item.Id == permitId, cancellationToken);
        if (permit is null)
        {
            return new FinancialContextCardResponse(
                resolution,
                RenewalChainSummary: null,
                CreditSummary: null,
                CommissionSummary: null,
                BuildFinancialContextCardAvailableActions(resolution));
        }

        var chain = await BuildPermitRenewalChainAsync(dbContext, permit, cancellationToken);
        var periodFrom = chain.Permits.Count == 0
            ? (DateOnly?)null
            : chain.Permits.Min(item => item.ValidFrom);
        var periodTo = chain.Permits.Count == 0
            ? (DateOnly?)null
            : chain.Permits.Max(item => item.ValidTo);

        return new FinancialContextCardResponse(
            resolution,
            new FinancialContextCardRenewalChainSummaryResponse(
                chain.CurrentPermitId,
                chain.CurrentPermit.FinancialName + " · " + chain.CurrentPermit.InstitutionOrDependency + " · " + chain.CurrentPermit.PlaceOrStand,
                chain.Summary.PermitsCount,
                chain.CurrentPermit.RenewalSequence,
                periodFrom,
                periodTo),
            new FinancialContextCardCreditSummaryResponse(
                chain.Summary.TotalCreditsCount,
                chain.Summary.TotalCreditsAmount),
            new FinancialContextCardCommissionSummaryResponse(
                chain.Summary.TotalCommissionsCount,
                chain.Summary.TotalCommissionsAmount,
                chain.Summary.TotalPromoterCommission,
                chain.Summary.TotalAdminCommission,
                chain.Summary.TotalThirdPartyCommission),
            BuildFinancialContextCardAvailableActions(resolution));
    }

    private static IReadOnlyList<string> BuildFinancialContextCardAvailableActions(
        FinancialPermitContextResolutionResponse resolution)
    {
        return resolution.SuggestionCode switch
        {
            UseCurrentPermitSuggestionCode => [CaptureCreditAction, ViewCurrentPermitAction, ViewChainAction],
            RenewLastPermitSuggestionCode => [PrepareRenewalAction, ViewCurrentPermitAction, ViewChainAction],
            CreateNewPermitSuggestionCode => [CreatePermitAction],
            ReviewTerminalChainSuggestionCode => [ViewCurrentPermitAction, ViewChainAction],
            _ => []
        };
    }

    private static async Task<FinancialPermitDetailResponse> BuildPermitDetailAsync(
        PlatformDbContext dbContext,
        FinancialPermit permit,
        CancellationToken cancellationToken)
    {
        var credits = await dbContext.FinancialCredits
            .AsNoTracking()
            .Where(item => item.FinancialPermitId == permit.Id)
            .OrderByDescending(item => item.AuthorizationDate)
            .ThenByDescending(item => item.CreatedUtc)
            .ToListAsync(cancellationToken);

        var commissionLookup = await BuildCommissionLookupAsync(dbContext, credits, cancellationToken);
        var daysUntilExpiration = GetDaysUntilExpiration(permit.ValidTo);
        var rootPermitId = permit.CurrentRootPermitId == Guid.Empty ? permit.Id : permit.CurrentRootPermitId;
        var renewalHistory = await dbContext.FinancialPermits
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .Where(item => item.CurrentRootPermitId == rootPermitId || item.Id == rootPermitId)
            .OrderBy(item => item.RenewalSequence)
            .ThenBy(item => item.CreatedUtc)
            .Select(item => new FinancialPermitRenewalHistoryResponse(
                item.Id,
                item.RenewedFromPermitId,
                item.CurrentRootPermitId,
                item.IsCurrentVersion,
                item.RenewalSequence,
                item.ValidFrom,
                item.ValidTo,
                item.PlaceOrStand,
                item.Schedule,
                item.StatusCatalogEntry!.StatusCode,
                item.StatusCatalogEntry.StatusName,
                item.CreatedUtc,
                item.UpdatedUtc))
            .ToListAsync(cancellationToken);

        return new FinancialPermitDetailResponse(
            permit.Id,
            permit.FinancialName,
            permit.InstitutionOrDependency,
            permit.PlaceOrStand,
            permit.ValidFrom,
            permit.ValidTo,
            permit.Schedule,
            permit.NegotiatedTerms,
            permit.StatusCatalogEntryId,
            permit.StatusCatalogEntry!.StatusCode,
            permit.StatusCatalogEntry.StatusName,
            permit.StatusCatalogEntry.IsClosed,
            permit.StatusCatalogEntry.AlertsEnabledByDefault,
            daysUntilExpiration,
            GetPermitAlertState(permit, daysUntilExpiration),
            permit.RenewedFromPermitId,
            permit.CurrentRootPermitId,
            permit.IsCurrentVersion,
            permit.RenewalSequence,
            permit.Notes,
            permit.CreatedUtc,
            permit.UpdatedUtc,
            renewalHistory,
            credits.Select(item => MapFinancialCreditResponse(item, commissionLookup.GetValueOrDefault(item.Id) ?? [])).ToList());
    }

    private static async Task<FinancialPermitRenewalChainResponse> BuildPermitRenewalChainAsync(
        PlatformDbContext dbContext,
        FinancialPermit permit,
        CancellationToken cancellationToken)
    {
        var rootPermitId = permit.CurrentRootPermitId == Guid.Empty ? permit.Id : permit.CurrentRootPermitId;
        var chainPermits = await dbContext.FinancialPermits
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .Where(item => item.CurrentRootPermitId == rootPermitId || item.Id == rootPermitId)
            .OrderBy(item => item.RenewalSequence)
            .ThenBy(item => item.CreatedUtc)
            .ToListAsync(cancellationToken);

        if (chainPermits.Count == 0)
        {
            chainPermits = [permit];
        }

        var currentPermit = chainPermits
            .OrderByDescending(item => item.IsCurrentVersion)
            .ThenByDescending(item => item.RenewalSequence)
            .ThenByDescending(item => item.CreatedUtc)
            .First();
        var permitIds = chainPermits.Select(item => item.Id).ToArray();
        var credits = await dbContext.FinancialCredits
            .AsNoTracking()
            .Where(item => permitIds.Contains(item.FinancialPermitId))
            .OrderByDescending(item => item.AuthorizationDate)
            .ThenByDescending(item => item.CreatedUtc)
            .ToListAsync(cancellationToken);
        var commissionLookup = await BuildCommissionLookupAsync(dbContext, credits, cancellationToken);
        var commissions = commissionLookup.Values.SelectMany(item => item).ToList();
        var operationFrom = credits.Count == 0 ? (DateOnly?)null : credits.Min(item => item.AuthorizationDate);
        var operationTo = credits.Count == 0 ? (DateOnly?)null : credits.Max(item => item.AuthorizationDate);
        var summary = new FinancialPermitRenewalChainSummaryResponse(
            chainPermits.Count,
            credits.Count,
            credits.Sum(item => item.Amount),
            commissions.Count,
            commissions.Sum(item => item.CommissionAmount),
            commissions
                .Where(item => item.CommissionType?.Code == PromoterCommissionTypeCode)
                .Sum(item => item.CommissionAmount),
            commissions
                .Where(item => item.CommissionType?.Code == AdministrationCommissionTypeCode)
                .Sum(item => item.CommissionAmount),
            commissions
                .Where(item => item.RecipientCategory == "THIRD_PARTY")
                .Sum(item => item.CommissionAmount),
            operationFrom,
            operationTo);

        return new FinancialPermitRenewalChainResponse(
            rootPermitId,
            currentPermit.Id,
            MapFinancialPermitRenewalChainPermitResponse(currentPermit),
            chainPermits.Select(MapFinancialPermitRenewalChainPermitResponse).ToList(),
            summary,
            credits.Select(item => MapFinancialCreditResponse(item, commissionLookup.GetValueOrDefault(item.Id) ?? [])).ToList());
    }

    private static async Task<List<FinancialPermitAlertResponse>> BuildPermitAlertsAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var permits = await dbContext.FinancialPermits
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .Where(item => item.IsCurrentVersion)
            .OrderBy(item => item.ValidTo)
            .ThenBy(item => item.FinancialName)
            .ToListAsync(cancellationToken);

        return permits
            .Select(permit =>
            {
                var daysUntilExpiration = GetDaysUntilExpiration(permit.ValidTo);
                var alertState = GetPermitAlertState(permit.StatusCatalogEntry!, daysUntilExpiration);

                return new
                {
                    Permit = permit,
                    DaysUntilExpiration = daysUntilExpiration,
                    AlertState = alertState
                };
            })
            .Where(item => item.AlertState is DueSoonAlertState or ExpiredAlertState or RenewalAlertState)
            .Select(item => new FinancialPermitAlertResponse(
                item.Permit.Id,
                item.Permit.FinancialName,
                item.Permit.InstitutionOrDependency,
                item.Permit.PlaceOrStand,
                item.Permit.StatusCatalogEntry!.StatusCode,
                item.Permit.StatusCatalogEntry.StatusName,
                item.Permit.ValidTo,
                item.DaysUntilExpiration,
                item.AlertState))
            .ToList();
    }

    private static FinancialPermitRenewalChainPermitResponse MapFinancialPermitRenewalChainPermitResponse(FinancialPermit permit)
    {
        var daysUntilExpiration = GetDaysUntilExpiration(permit.ValidTo);

        return new FinancialPermitRenewalChainPermitResponse(
            permit.Id,
            permit.RenewedFromPermitId,
            permit.CurrentRootPermitId,
            permit.IsCurrentVersion,
            permit.RenewalSequence,
            permit.FinancialName,
            permit.InstitutionOrDependency,
            permit.PlaceOrStand,
            permit.ValidFrom,
            permit.ValidTo,
            permit.Schedule,
            permit.StatusCatalogEntryId,
            permit.StatusCatalogEntry!.StatusCode,
            permit.StatusCatalogEntry.StatusName,
            permit.StatusCatalogEntry.IsClosed,
            daysUntilExpiration,
            GetPermitAlertState(permit, daysUntilExpiration),
            permit.CreatedUtc,
            permit.UpdatedUtc);
    }

    private static FinancialCreditResponse MapFinancialCreditResponse(
        FinancialCredit credit,
        IReadOnlyList<FinancialCreditCommission> commissions)
    {
        return new FinancialCreditResponse(
            credit.Id,
            credit.FinancialPermitId,
            credit.PromoterContactId,
            credit.PromoterName,
            credit.BeneficiaryContactId,
            credit.BeneficiaryName,
            credit.PhoneNumber,
            credit.WhatsAppPhone,
            credit.AuthorizationDate,
            credit.Amount,
            credit.Notes,
            commissions.Count,
            commissions.Select(MapFinancialCreditCommissionResponse).ToList(),
            credit.CreatedUtc);
    }

    private static FinancialCreditCommissionResponse MapFinancialCreditCommissionResponse(FinancialCreditCommission commission)
    {
        return new FinancialCreditCommissionResponse(
            commission.Id,
            commission.FinancialCreditId,
            commission.CommissionTypeId,
            commission.CommissionType!.Code,
            commission.CommissionType.Name,
            commission.RecipientCategory,
            commission.RecipientContactId,
            commission.RecipientName,
            commission.BaseAmount,
            commission.CommissionAmount,
            commission.Notes,
            commission.CreatedUtc);
    }

    private static async Task<Dictionary<Guid, List<FinancialCreditCommission>>> BuildCommissionLookupAsync(
        PlatformDbContext dbContext,
        IReadOnlyList<FinancialCredit> credits,
        CancellationToken cancellationToken)
    {
        if (credits.Count == 0)
        {
            return [];
        }

        var creditIds = credits.Select(item => item.Id).ToArray();
        var commissions = await dbContext.FinancialCreditCommissions
            .AsNoTracking()
            .Where(item => creditIds.Contains(item.FinancialCreditId))
            .Include(item => item.CommissionType)
            .OrderByDescending(item => item.CreatedUtc)
            .ToListAsync(cancellationToken);

        return commissions
            .GroupBy(item => item.FinancialCreditId)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
    }

    private static async Task<FinancialPermitOperationBlockedResponse> BuildNotCurrentPermitOperationBlockedResponseAsync(
        PlatformDbContext dbContext,
        FinancialPermit permit,
        string operationLabel,
        CancellationToken cancellationToken)
    {
        var currentRootPermitId = GetCurrentRootPermitId(permit);
        var currentPermitId = await dbContext.FinancialPermits
            .AsNoTracking()
            .Where(item => item.CurrentRootPermitId == currentRootPermitId || item.Id == currentRootPermitId)
            .OrderByDescending(item => item.IsCurrentVersion)
            .ThenByDescending(item => item.RenewalSequence)
            .ThenByDescending(item => item.CreatedUtc)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new FinancialPermitOperationBlockedResponse(
            $"No es posible {operationLabel} porque el oficio de {permit.FinancialName} es histórico/no vigente. Captura la operación en el permiso vigente de la cadena.",
            NotCurrentPermitOperationReasonCode,
            permit.Id,
            currentRootPermitId,
            currentPermitId);
    }

    private static FinancialPermitOperationBlockedResponse BuildTerminalPermitOperationBlockedResponse(
        FinancialPermit permit,
        string operationLabel)
    {
        var currentRootPermitId = GetCurrentRootPermitId(permit);

        return new FinancialPermitOperationBlockedResponse(
            StateTransitionSupport.BuildTerminalMutationMessage(
                $"el oficio de {permit.FinancialName}",
                operationLabel,
                permit.StatusCatalogEntry!),
            TerminalPermitOperationReasonCode,
            permit.Id,
            currentRootPermitId,
            permit.IsCurrentVersion ? permit.Id : null);
    }

    private static async Task<FinancialPermit?> FindActiveOperationalPermitConflictAsync(
        PlatformDbContext dbContext,
        string financialName,
        string institutionOrDependency,
        string placeOrStand,
        Guid? excludedRootPermitId,
        CancellationToken cancellationToken)
    {
        var matches = await FindActiveOperationalPermitMatchesAsync(
            dbContext,
            financialName,
            institutionOrDependency,
            placeOrStand,
            excludedRootPermitId,
            cancellationToken);

        return matches.FirstOrDefault();
    }

    private static async Task<IReadOnlyList<FinancialPermit>> FindActiveOperationalPermitMatchesAsync(
        PlatformDbContext dbContext,
        string financialName,
        string institutionOrDependency,
        string placeOrStand,
        Guid? excludedRootPermitId,
        CancellationToken cancellationToken)
    {
        var matches = await FindCurrentOperationalPermitMatchesAsync(
            dbContext,
            financialName,
            institutionOrDependency,
            placeOrStand,
            excludedRootPermitId,
            cancellationToken);

        return matches
            .Where(item => !StateTransitionSupport.IsTerminal(item.StatusCatalogEntry))
            .ToList();
    }

    private static async Task<IReadOnlyList<FinancialPermit>> FindCurrentOperationalPermitMatchesAsync(
        PlatformDbContext dbContext,
        string financialName,
        string institutionOrDependency,
        string placeOrStand,
        Guid? excludedRootPermitId,
        CancellationToken cancellationToken)
    {
        var requestedKey = BuildOperationalKey(financialName, institutionOrDependency, placeOrStand);
        var candidates = await dbContext.FinancialPermits
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .Where(item => item.IsCurrentVersion)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(item => excludedRootPermitId is null || GetCurrentRootPermitId(item) != excludedRootPermitId.Value)
            .Where(item => BuildOperationalKey(item.FinancialName, item.InstitutionOrDependency, item.PlaceOrStand) == requestedKey)
            .OrderByDescending(item => item.RenewalSequence)
            .ThenByDescending(item => item.CreatedUtc)
            .ToList();
    }

    private static async Task<FinancialPermit?> FindHistoricalOperationalPermitMatchAsync(
        PlatformDbContext dbContext,
        string financialName,
        string institutionOrDependency,
        string placeOrStand,
        CancellationToken cancellationToken)
    {
        var requestedKey = BuildOperationalKey(financialName, institutionOrDependency, placeOrStand);
        var candidates = await dbContext.FinancialPermits
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .Where(item => !item.IsCurrentVersion)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(item => BuildOperationalKey(item.FinancialName, item.InstitutionOrDependency, item.PlaceOrStand) == requestedKey)
            .OrderByDescending(item => item.RenewalSequence)
            .ThenByDescending(item => item.CreatedUtc)
            .FirstOrDefault();
    }

    private static async Task<FinancialPermit?> FindCurrentPermitInChainAsync(
        PlatformDbContext dbContext,
        Guid currentRootPermitId,
        CancellationToken cancellationToken)
    {
        return await dbContext.FinancialPermits
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .Where(item => item.CurrentRootPermitId == currentRootPermitId || item.Id == currentRootPermitId)
            .Where(item => item.IsCurrentVersion)
            .OrderByDescending(item => item.RenewalSequence)
            .ThenByDescending(item => item.CreatedUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static FinancialPermitCurrentResolutionResponse BuildCurrentPermitResolutionResponse(FinancialPermit permit)
    {
        var daysUntilExpiration = GetDaysUntilExpiration(permit.ValidTo);
        var alertState = GetPermitAlertState(permit, daysUntilExpiration);

        return new FinancialPermitCurrentResolutionResponse(
            permit.Id,
            GetCurrentRootPermitId(permit),
            permit.RenewalSequence,
            permit.FinancialName,
            permit.InstitutionOrDependency,
            permit.PlaceOrStand,
            permit.ValidFrom,
            permit.ValidTo,
            permit.Schedule,
            permit.StatusCatalogEntryId,
            permit.StatusCatalogEntry!.StatusCode,
            permit.StatusCatalogEntry.StatusName,
            permit.StatusCatalogEntry.IsClosed,
            daysUntilExpiration,
            alertState,
            $"{permit.FinancialName} · {permit.InstitutionOrDependency} · {permit.PlaceOrStand}");
    }

    private static FinancialPermitContextResolutionResponse BuildFinancialPermitContextResolutionResponse(
        string financialName,
        string institutionOrDependency,
        string placeOrStand,
        FinancialPermitContextPermitResponse? currentPermit,
        FinancialPermitContextPermitResponse? lastKnownPermit,
        string suggestionCode,
        string suggestionMessage,
        string routeHint)
    {
        return new FinancialPermitContextResolutionResponse(
            financialName.Trim(),
            institutionOrDependency.Trim(),
            placeOrStand.Trim(),
            currentPermit,
            currentPermit?.CurrentRootPermitId ?? lastKnownPermit?.CurrentRootPermitId,
            lastKnownPermit,
            suggestionCode,
            suggestionMessage,
            routeHint);
    }

    private static FinancialPermitContextPermitResponse BuildFinancialPermitContextPermitResponse(FinancialPermit permit)
    {
        var daysUntilExpiration = GetDaysUntilExpiration(permit.ValidTo);
        var alertState = GetPermitAlertState(permit, daysUntilExpiration);

        return new FinancialPermitContextPermitResponse(
            permit.Id,
            GetCurrentRootPermitId(permit),
            permit.RenewedFromPermitId,
            permit.IsCurrentVersion,
            permit.RenewalSequence,
            permit.FinancialName,
            permit.InstitutionOrDependency,
            permit.PlaceOrStand,
            permit.ValidFrom,
            permit.ValidTo,
            permit.Schedule,
            permit.StatusCatalogEntryId,
            permit.StatusCatalogEntry!.StatusCode,
            permit.StatusCatalogEntry.StatusName,
            permit.StatusCatalogEntry.IsClosed,
            daysUntilExpiration,
            alertState,
            $"{permit.FinancialName} · {permit.InstitutionOrDependency} · {permit.PlaceOrStand}");
    }

    private static FinancialPermitRenewalDraftResponse BuildFinancialPermitRenewalDraftResponse(
        FinancialPermit sourcePermit,
        FinancialPermit renewalTargetPermit)
    {
        var latestKnownValidTo = sourcePermit.ValidTo > renewalTargetPermit.ValidTo
            ? sourcePermit.ValidTo
            : renewalTargetPermit.ValidTo;

        return new FinancialPermitRenewalDraftResponse(
            sourcePermit.Id,
            renewalTargetPermit.Id,
            GetCurrentRootPermitId(renewalTargetPermit),
            sourcePermit.IsCurrentVersion,
            sourcePermit.RenewalSequence,
            renewalTargetPermit.RenewalSequence,
            sourcePermit.FinancialName,
            sourcePermit.InstitutionOrDependency,
            sourcePermit.PlaceOrStand,
            sourcePermit.Schedule,
            sourcePermit.NegotiatedTerms,
            sourcePermit.Notes,
            sourcePermit.ValidFrom,
            sourcePermit.ValidTo,
            latestKnownValidTo.AddDays(1),
            latestKnownValidTo.AddDays(31),
            renewalTargetPermit.StatusCatalogEntryId,
            renewalTargetPermit.StatusCatalogEntry!.StatusCode,
            renewalTargetPermit.StatusCatalogEntry.StatusName,
            renewalTargetPermit.StatusCatalogEntry.IsClosed,
            RenewLastPermitSuggestionCode,
            sourcePermit.Id == renewalTargetPermit.Id
                ? "Renovacion preparada con los datos del permiso vigente seleccionado. Confirma nuevo periodo antes de crearla."
                : "Renovacion preparada con el contexto del ultimo permiso aplicable; la operacion real se ejecutara sobre el permiso vigente de la cadena.",
            [
                "newStartDate",
                "newEndDate",
                "status"
            ]);
    }

    private static string BuildPermitRouteHint(Guid permitId, bool includeChain)
    {
        return includeChain
            ? $"/financials?permitId={permitId}&chain=1"
            : $"/financials?permitId={permitId}";
    }

    private static FinancialPermitActiveConflictResponse BuildActivePermitConflictResponse(
        FinancialPermit conflictingPermit,
        string operationLabel)
    {
        return new FinancialPermitActiveConflictResponse(
            $"No es posible {operationLabel} porque ya existe un oficio vigente/no terminal para la misma financiera, dependencia o institucion y lugar/stand. Opera sobre el oficio vigente en conflicto o revisa la cadena antes de continuar.",
            ActivePermitConflictReasonCode,
            conflictingPermit.Id,
            GetCurrentRootPermitId(conflictingPermit),
            conflictingPermit.FinancialName,
            conflictingPermit.InstitutionOrDependency,
            conflictingPermit.PlaceOrStand,
            conflictingPermit.ValidFrom,
            conflictingPermit.ValidTo);
    }

    private static FinancialPermitOperationalKey BuildOperationalKey(
        string financialName,
        string institutionOrDependency,
        string placeOrStand)
    {
        return new FinancialPermitOperationalKey(
            NormalizeOperationalText(financialName),
            NormalizeOperationalText(institutionOrDependency),
            NormalizeOperationalText(placeOrStand));
    }

    private static string NormalizeOperationalText(string value)
    {
        var normalizedValue = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalizedValue.Length);
        var previousWasWhitespace = false;

        foreach (var character in normalizedValue)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace && builder.Length > 0)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            builder.Append(char.ToUpperInvariant(character));
            previousWasWhitespace = false;
        }

        return builder.ToString().Trim();
    }

    private static Guid GetCurrentRootPermitId(FinancialPermit permit)
    {
        return permit.CurrentRootPermitId == Guid.Empty ? permit.Id : permit.CurrentRootPermitId;
    }

    private static async Task<ModuleStatusCatalogEntry?> ResolveModuleStatusAsync(
        int statusCatalogEntryId,
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.ModuleStatusCatalogEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == statusCatalogEntryId
                    && item.ModuleCode == FinancialsModuleCode
                    && item.ContextCode == FinancialPermitContextCode,
                cancellationToken);
    }

    private static int GetDaysUntilExpiration(DateOnly validTo)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return validTo.DayNumber - today.DayNumber;
    }

    private static string GetPermitAlertState(FinancialPermit permit, int daysUntilExpiration)
    {
        return GetPermitAlertState(permit, daysUntilExpiration, permit.StatusCatalogEntry!);
    }

    private static string GetPermitAlertState(FinancialPermit permit, int daysUntilExpiration, ModuleStatusCatalogEntry permitStatus)
    {
        if (!permit.IsCurrentVersion)
        {
            return HistoricalAlertState;
        }

        return GetPermitAlertState(permitStatus, daysUntilExpiration);
    }

    private static string GetPermitAlertState(ModuleStatusCatalogEntry permitStatus, int daysUntilExpiration)
    {
        if (permitStatus.IsClosed || !permitStatus.AlertsEnabledByDefault)
        {
            return AlertsDisabledState;
        }

        if (permitStatus.StatusCode == RenewStatusCode)
        {
            return RenewalAlertState;
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

    private static Dictionary<string, string[]> ValidateCurrentPermitResolutionRequest(
        string? financialName,
        string? institutionOrDependency,
        string? placeOrStand)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(financialName))
        {
            errors["financialName"] = ["FinancialName is required."];
        }

        if (string.IsNullOrWhiteSpace(institutionOrDependency))
        {
            errors["institutionOrDependency"] = ["InstitutionOrDependency is required."];
        }

        if (string.IsNullOrWhiteSpace(placeOrStand))
        {
            errors["placeOrStand"] = ["PlaceOrStand is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateCreateFinancialPermitRequest(CreateFinancialPermitRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.FinancialName))
        {
            errors["financialName"] = ["FinancialName is required."];
        }

        if (string.IsNullOrWhiteSpace(request.InstitutionOrDependency))
        {
            errors["institutionOrDependency"] = ["InstitutionOrDependency is required."];
        }

        if (string.IsNullOrWhiteSpace(request.PlaceOrStand))
        {
            errors["placeOrStand"] = ["PlaceOrStand is required."];
        }

        if (request.ValidFrom == default)
        {
            errors["validFrom"] = ["ValidFrom is required."];
        }

        if (request.ValidTo == default)
        {
            errors["validTo"] = ["ValidTo is required."];
        }
        else if (request.ValidFrom != default && request.ValidTo < request.ValidFrom)
        {
            errors["validTo"] = ["ValidTo cannot be earlier than ValidFrom."];
        }

        if (string.IsNullOrWhiteSpace(request.Schedule))
        {
            errors["schedule"] = ["Schedule is required."];
        }

        if (string.IsNullOrWhiteSpace(request.NegotiatedTerms))
        {
            errors["negotiatedTerms"] = ["NegotiatedTerms is required."];
        }

        if (request.StatusCatalogEntryId <= 0)
        {
            errors["statusCatalogEntryId"] = ["StatusCatalogEntryId is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateRenewFinancialPermitRequest(RenewFinancialPermitRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.ValidFrom == default)
        {
            errors["validFrom"] = ["ValidFrom is required."];
        }

        if (request.ValidTo == default)
        {
            errors["validTo"] = ["ValidTo is required."];
        }
        else if (request.ValidFrom != default && request.ValidTo < request.ValidFrom)
        {
            errors["validTo"] = ["ValidTo cannot be earlier than ValidFrom."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateCreateFinancialCreditRequest(CreateFinancialCreditRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.PromoterName))
        {
            errors["promoterName"] = ["PromoterName is required."];
        }

        if (string.IsNullOrWhiteSpace(request.BeneficiaryName))
        {
            errors["beneficiaryName"] = ["BeneficiaryName is required."];
        }

        if (request.AuthorizationDate == default)
        {
            errors["authorizationDate"] = ["AuthorizationDate is required."];
        }

        if (request.Amount <= 0)
        {
            errors["amount"] = ["Amount must be greater than zero."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateCreateFinancialCreditCommissionRequest(CreateFinancialCreditCommissionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.CommissionTypeId <= 0)
        {
            errors["commissionTypeId"] = ["CommissionTypeId is required."];
        }

        if (string.IsNullOrWhiteSpace(request.RecipientCategory))
        {
            errors["recipientCategory"] = ["RecipientCategory is required."];
        }
        else
        {
            var normalizedRecipientCategory = request.RecipientCategory.Trim().ToUpperInvariant();
            if (!AllowedRecipientCategories.Contains(normalizedRecipientCategory))
            {
                errors["recipientCategory"] =
                [
                    "RecipientCategory must be one of COMPANY, THIRD_PARTY or OTHER_PARTICIPANT."
                ];
            }
        }

        if (string.IsNullOrWhiteSpace(request.RecipientName))
        {
            errors["recipientName"] = ["RecipientName is required."];
        }

        if (request.BaseAmount <= 0)
        {
            errors["baseAmount"] = ["BaseAmount must be greater than zero."];
        }

        if (request.CommissionAmount <= 0)
        {
            errors["commissionAmount"] = ["CommissionAmount must be greater than zero."];
        }

        return errors;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record FinancialPermitContextResolutionResult(
        FinancialPermitContextResolutionResponse? Resolution,
        FinancialPermitCurrentResolutionAmbiguousResponse? AmbiguousResponse);

    private sealed record FinancialPermitOperationalKey(
        string FinancialName,
        string InstitutionOrDependency,
        string PlaceOrStand);
}
