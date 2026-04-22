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
    private const string DueSoonAlertState = "DUE_SOON";
    private const string ExpiredAlertState = "EXPIRED";
    private const string RenewalAlertState = "RENEWAL";
    private const string ValidAlertState = "VALID";
    private const string AlertsDisabledState = "ALERTS_DISABLED";
    private static readonly HashSet<string> AllowedRecipientCategories =
    [
        "COMPANY",
        "THIRD_PARTY",
        "OTHER_PARTICIPANT"
    ];

    public static IEndpointRouteBuilder MapFinancialsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/financials")
            .WithTags("Financials");

        group.MapGet(
            "/alerts/permits",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var alerts = await BuildPermitAlertsAsync(dbContext, cancellationToken);
                return Results.Ok(alerts);
            });

        group.MapGet(
            string.Empty,
            async (string? statusCode, bool? alertsOnly, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var permits = await dbContext.FinancialPermits
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .OrderBy(item => item.ValidTo)
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
                        var alertState = GetPermitAlertState(permit.StatusCatalogEntry!, daysUntilExpiration);

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

        group.MapGet(
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

        group.MapPost(
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

        group.MapPost(
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
                    GetPermitAlertState(permitStatus, daysUntilExpiration),
                    0,
                    0,
                    permit.Notes,
                    permit.CreatedUtc,
                    permit.UpdatedUtc);

                return Results.Created($"/api/financials/{permit.Id}", response);
            });

        group.MapGet(
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

        group.MapPost(
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

                if (StateTransitionSupport.IsTerminal(permit.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        new
                        {
                            message = StateTransitionSupport.BuildTerminalMutationMessage(
                                $"el oficio de {permit.FinancialName}",
                                "registrar un crédito",
                                permit.StatusCatalogEntry!)
                        });
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

        group.MapGet(
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

        group.MapPost(
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

                if (StateTransitionSupport.IsTerminal(credit.FinancialPermit?.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        new
                        {
                            message = StateTransitionSupport.BuildTerminalMutationMessage(
                                $"el oficio de {credit.FinancialPermit!.FinancialName}",
                                "registrar una comisión",
                                credit.FinancialPermit.StatusCatalogEntry!)
                        });
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
            GetPermitAlertState(permit.StatusCatalogEntry, daysUntilExpiration),
            permit.Notes,
            permit.CreatedUtc,
            permit.UpdatedUtc,
            credits.Select(item => MapFinancialCreditResponse(item, commissionLookup.GetValueOrDefault(item.Id) ?? [])).ToList());
    }

    private static async Task<List<FinancialPermitAlertResponse>> BuildPermitAlertsAsync(
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
}
