using FMCPA.Api.Contracts.Closeout;
using FMCPA.Api.Contracts.Federation;
using FMCPA.Api.Extensions;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Domain.Entities.Federation;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class FederationEndpoints
{
    private const string FederationModuleCode = "FEDERATION";
    private const string FederationModuleName = "Federacion";
    private const string FederationActionContextCode = "FEDERATION_ACTION";
    private const string FederationDonationContextCode = "FEDERATION_DONATION";
    private const string FederationDonationApplicationContextCode = "FEDERATION_DONATION_APPLICATION";
    private const string FederationActionEntityType = "FEDERATION_ACTION";
    private const string FederationActionParticipantEntityType = "FEDERATION_ACTION_PARTICIPANT";
    private const string FederationDonationEntityType = "FEDERATION_DONATION";
    private const string FederationDonationApplicationEntityType = "FEDERATION_DONATION_APPLICATION";
    private const string FederationDonationEvidenceEntityType = "FEDERATION_DONATION_APPLICATION_EVIDENCE";
    private const string FederationDonationCommissionEntityType = "FEDERATION_DONATION_APPLICATION_COMMISSION";
    private const string FederationDonationEvidenceAreaCode = DocumentAreaCodes.FederationApplicationEvidences;
    private const string InProcessStatusCode = "IN_PROCESS";
    private const string FollowUpPendingStatusCode = "FOLLOW_UP_PENDING";
    private const string ConcludedStatusCode = "CONCLUDED";
    private const string ClosedStatusCode = "CLOSED";
    private const string NotAppliedStatusCode = "NOT_APPLIED";
    private const string PartiallyAppliedStatusCode = "PARTIALLY_APPLIED";
    private const string AppliedStatusCode = "APPLIED";
    private const string NoAlertState = "NONE";
    private static readonly HashSet<string> AllowedActionTypeCodes =
    [
        "AGREEMENT",
        "MEETING",
        "INTERVIEW",
        "GOVERNMENT_MANAGEMENT"
    ];
    private static readonly HashSet<string> AllowedParticipantSides =
    [
        "INTERNAL",
        "EXTERNAL"
    ];
    private static readonly HashSet<string> AllowedRecipientCategories =
    [
        "COMPANY",
        "THIRD_PARTY",
        "OTHER_PARTICIPANT"
    ];

    public static IEndpointRouteBuilder MapFederationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/federation")
            .WithTags("Federation");

        group.MapGet(
            "/alerts",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var actionAlerts = await BuildActionAlertsAsync(dbContext, cancellationToken);
                var donationAlerts = await BuildDonationAlertsAsync(dbContext, cancellationToken);
                return Results.Ok(new FederationModuleAlertsResponse(actionAlerts, donationAlerts));
            });

        group.MapGet(
            "/actions",
            async (string? statusCode, bool? alertsOnly, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var actions = await dbContext.FederationActions
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .OrderByDescending(item => item.ActionDate)
                    .ThenBy(item => item.CounterpartyOrInstitution)
                    .ToListAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(statusCode))
                {
                    var normalizedStatusCode = statusCode.Trim().ToUpperInvariant();
                    actions = actions
                        .Where(item => item.StatusCatalogEntry?.StatusCode == normalizedStatusCode)
                        .ToList();
                }

                var actionIds = actions.Select(item => item.Id).ToArray();
                var participantCounts = actionIds.Length == 0
                    ? new Dictionary<Guid, int>()
                    : await dbContext.FederationActionParticipants
                        .AsNoTracking()
                        .Where(item => actionIds.Contains(item.FederationActionId))
                        .GroupBy(item => item.FederationActionId)
                        .Select(grouping => new { FederationActionId = grouping.Key, Count = grouping.Count() })
                        .ToDictionaryAsync(item => item.FederationActionId, item => item.Count, cancellationToken);

                var summaries = actions
                    .Select(action =>
                    {
                        var alertState = GetActionAlertState(action.StatusCatalogEntry!);

                        return new FederationActionSummaryResponse(
                            action.Id,
                            action.ActionTypeCode,
                            MapActionTypeName(action.ActionTypeCode),
                            action.CounterpartyOrInstitution,
                            action.ActionDate,
                            action.Objective,
                            action.StatusCatalogEntryId,
                            action.StatusCatalogEntry!.StatusCode,
                            action.StatusCatalogEntry.StatusName,
                            action.StatusCatalogEntry.IsClosed,
                            action.StatusCatalogEntry.AlertsEnabledByDefault,
                            participantCounts.GetValueOrDefault(action.Id),
                            alertState,
                            action.Notes,
                            action.CreatedUtc,
                            action.UpdatedUtc);
                    })
                    .ToList();

                if (alertsOnly == true)
                {
                    summaries = summaries
                        .Where(item => item.AlertState is InProcessStatusCode or FollowUpPendingStatusCode)
                        .ToList();
                }

                return Results.Ok(summaries);
            });

        group.MapGet(
            "/actions/{actionId:guid}",
            async (Guid actionId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var action = await dbContext.FederationActions
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == actionId, cancellationToken);

                if (action is null)
                {
                    return Results.NotFound();
                }

                var detail = await BuildActionDetailAsync(dbContext, action, cancellationToken);
                return Results.Ok(detail);
            });

        group.MapPost(
            "/actions/{actionId:guid}/close",
            async (Guid actionId, CloseRecordRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var action = await dbContext.FederationActions
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == actionId, cancellationToken);

                if (action is null)
                {
                    return Results.NotFound();
                }

                if (await AuditEventSupport.HasCloseEventAsync(dbContext, FederationModuleCode, FederationActionEntityType, action.Id, cancellationToken))
                {
                    return Results.Conflict(new { message = StateTransitionSupport.BuildDuplicateCloseEventMessage("la gestión") });
                }

                var currentStatus = action.StatusCatalogEntry!;
                if (StateTransitionSupport.IsTerminal(currentStatus))
                {
                    return Results.Conflict(new { message = StateTransitionSupport.BuildTerminalCloseMessage("la gestión", currentStatus) });
                }

                if (!currentStatus.IsClosed)
                {
                    var closedStatus = await AuditEventSupport.ResolveContextStatusByCodeAsync(
                        dbContext,
                        FederationModuleCode,
                        FederationActionContextCode,
                        ClosedStatusCode,
                        cancellationToken);

                    if (closedStatus is null)
                    {
                        return Results.ValidationProblem(
                            new Dictionary<string, string[]>
                            {
                                ["statusCatalogEntryId"] = ["The closed status for federation actions is not configured."]
                            });
                    }

                    action.SyncStatus(closedStatus.Id);
                    currentStatus = closedStatus;
                }

                var reason = NormalizeOptionalText(request.Reason);
                var auditEvent = AuditEventSupport.CreateAuditEvent(
                    FederationModuleCode,
                    FederationModuleName,
                    FederationActionEntityType,
                    action.Id,
                    AuditEventSupport.FormalCloseActionType,
                    MapActionTypeName(action.ActionTypeCode),
                    reason is null
                        ? $"Cierre formal registrado para la gestión con {action.CounterpartyOrInstitution}."
                        : $"Cierre formal registrado para la gestión con {action.CounterpartyOrInstitution}. Motivo: {reason}.",
                    currentStatus.StatusCode,
                    action.CounterpartyOrInstitution,
                    "/federation",
                    isCloseEvent: true,
                    metadata: new
                    {
                        reason,
                        action.ActionDate
                    });

                dbContext.AuditEvents.Add(auditEvent);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Ok(
                    new CloseRecordResponse(
                        auditEvent.Id,
                        FederationModuleCode,
                        FederationModuleName,
                        FederationActionEntityType,
                        action.Id.ToString(),
                        currentStatus.StatusCode,
                        currentStatus.StatusName,
                        auditEvent.OccurredUtc,
                        reason));
            });

        group.MapPost(
            "/actions",
            async (CreateFederationActionRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var errors = ValidateCreateFederationActionRequest(request);
                var actionStatus = await ResolveModuleStatusAsync(
                    request.StatusCatalogEntryId,
                    FederationActionContextCode,
                    dbContext,
                    cancellationToken);

                if (actionStatus is null)
                {
                    errors["statusCatalogEntryId"] = ["The selected federation action status does not exist."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var action = new FederationAction(
                    request.ActionTypeCode,
                    request.CounterpartyOrInstitution,
                    request.ActionDate,
                    request.Objective,
                    request.StatusCatalogEntryId,
                    request.Notes);

                dbContext.FederationActions.Add(action);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        FederationModuleCode,
                        FederationModuleName,
                        FederationActionEntityType,
                        action.Id,
                        "CREATED",
                        MapActionTypeName(action.ActionTypeCode),
                        $"Gestión registrada con {action.CounterpartyOrInstitution}.",
                        actionStatus!.StatusCode,
                        action.CounterpartyOrInstitution,
                        "/federation",
                        metadata: new
                        {
                            action.ActionDate,
                            action.Objective
                        }));
                await dbContext.SaveChangesAsync(cancellationToken);

                var response = new FederationActionSummaryResponse(
                    action.Id,
                    action.ActionTypeCode,
                    MapActionTypeName(action.ActionTypeCode),
                    action.CounterpartyOrInstitution,
                    action.ActionDate,
                    action.Objective,
                    action.StatusCatalogEntryId,
                    actionStatus!.StatusCode,
                    actionStatus.StatusName,
                    actionStatus.IsClosed,
                    actionStatus.AlertsEnabledByDefault,
                    0,
                    GetActionAlertState(actionStatus),
                    action.Notes,
                    action.CreatedUtc,
                    action.UpdatedUtc);

                return Results.Created($"/api/federation/actions/{action.Id}", response);
            });

        group.MapGet(
            "/actions/{actionId:guid}/participants",
            async (Guid actionId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var actionExists = await dbContext.FederationActions
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == actionId, cancellationToken);

                if (!actionExists)
                {
                    return Results.NotFound();
                }

                var participants = await dbContext.FederationActionParticipants
                    .AsNoTracking()
                    .Where(item => item.FederationActionId == actionId)
                    .Include(item => item.Contact)
                    .ThenInclude(contact => contact!.ContactType)
                    .OrderBy(item => item.ParticipantSide)
                    .ThenBy(item => item.ParticipantName)
                    .ToListAsync(cancellationToken);

                return Results.Ok(participants.Select(MapActionParticipantResponse).ToList());
            });

        group.MapPost(
            "/actions/{actionId:guid}/participants",
            async (Guid actionId, CreateFederationActionParticipantRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var action = await dbContext.FederationActions
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == actionId, cancellationToken);

                if (action is null)
                {
                    return Results.NotFound();
                }

                if (StateTransitionSupport.IsTerminal(action.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        new
                        {
                            message = StateTransitionSupport.BuildTerminalMutationMessage(
                                $"la gestión con {action.CounterpartyOrInstitution}",
                                "vincular un participante",
                                action.StatusCatalogEntry!)
                        });
                }

                var errors = ValidateCreateFederationActionParticipantRequest(request);
                var contact = await dbContext.Contacts
                    .AsNoTracking()
                    .Include(item => item.ContactType)
                    .SingleOrDefaultAsync(item => item.Id == request.ContactId, cancellationToken);

                if (contact is null)
                {
                    errors["contactId"] = ["The selected participant contact does not exist."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var participant = new FederationActionParticipant(
                    actionId,
                    request.ContactId,
                    request.ParticipantSide,
                    contact!.Name,
                    contact.OrganizationOrDependency,
                    contact.RoleTitle,
                    request.Notes);

                dbContext.FederationActionParticipants.Add(participant);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        FederationModuleCode,
                        FederationModuleName,
                        FederationActionParticipantEntityType,
                        participant.Id,
                        "LINKED",
                        participant.ParticipantName,
                        $"Participante {MapParticipantSideName(participant.ParticipantSide)} agregado a la gestión con {action.CounterpartyOrInstitution}.",
                        null,
                        action.CounterpartyOrInstitution,
                        "/federation",
                        metadata: new
                        {
                            participant.FederationActionId,
                            participant.ContactId,
                            participant.ParticipantSide
                        }));
                dbContext.ContactParticipations.Add(
                    new ContactParticipation(
                        request.ContactId,
                        FederationModuleCode,
                        "FEDERATION_ACTION_PARTICIPANT",
                        participant.Id.ToString(),
                        request.ParticipantSide.Trim().Equals("INTERNAL", StringComparison.OrdinalIgnoreCase)
                            ? "Participante interno"
                            : "Participante externo",
                        $"Gestion vinculada: {action.CounterpartyOrInstitution}"));

                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Created(
                    $"/api/federation/actions/{actionId}/participants/{participant.Id}",
                    new FederationActionParticipantResponse(
                        participant.Id,
                        participant.FederationActionId,
                        participant.ContactId,
                        participant.ParticipantSide,
                        contact.ContactType!.Code,
                        contact.ContactType.Name,
                        participant.ParticipantName,
                        participant.OrganizationOrDependency,
                        participant.RoleTitle,
                        participant.Notes,
                        participant.CreatedUtc));
            });

        group.MapGet(
            "/donations",
            async (string? statusCode, bool? alertsOnly, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var donations = await dbContext.FederationDonations
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .OrderByDescending(item => item.DonationDate)
                    .ThenBy(item => item.DonorName)
                    .ToListAsync(cancellationToken);

                if (!string.IsNullOrWhiteSpace(statusCode))
                {
                    var normalizedStatusCode = statusCode.Trim().ToUpperInvariant();
                    donations = donations
                        .Where(item => item.StatusCatalogEntry?.StatusCode == normalizedStatusCode)
                        .ToList();
                }

                var donationIds = donations.Select(item => item.Id).ToArray();
                var applications = donationIds.Length == 0
                    ? []
                    : await dbContext.FederationDonationApplications
                        .AsNoTracking()
                        .Where(item => donationIds.Contains(item.FederationDonationId))
                        .ToListAsync(cancellationToken);

                var evidenceCounts = await BuildEvidenceCountLookupAsync(dbContext, applications, cancellationToken);
                var commissionCounts = await BuildCommissionCountLookupAsync(dbContext, applications, cancellationToken);

                var summaries = donations
                    .Select(donation =>
                    {
                        var donationApplications = applications.Where(item => item.FederationDonationId == donation.Id).ToList();
                        var metrics = CalculateProgress(donation.BaseAmount, donationApplications);
                        var alertState = GetDonationAlertState(donation.StatusCatalogEntry!, metrics.AppliedAmountTotal, donation.BaseAmount);
                        var evidenceCount = donationApplications.Sum(item => evidenceCounts.GetValueOrDefault(item.Id));
                        var commissionCount = donationApplications.Sum(item => commissionCounts.GetValueOrDefault(item.Id));

                        return new FederationDonationSummaryResponse(
                            donation.Id,
                            donation.DonorName,
                            donation.DonationDate,
                            donation.DonationType,
                            donation.BaseAmount,
                            donation.Reference,
                            donation.Notes,
                            donation.StatusCatalogEntryId,
                            donation.StatusCatalogEntry!.StatusCode,
                            donation.StatusCatalogEntry.StatusName,
                            donation.StatusCatalogEntry.IsClosed,
                            donation.StatusCatalogEntry.AlertsEnabledByDefault,
                            metrics.AppliedAmountTotal,
                            metrics.RemainingAmount,
                            metrics.AppliedPercentage,
                            donationApplications.Count,
                            evidenceCount,
                            commissionCount,
                            alertState,
                            donation.CreatedUtc,
                            donation.UpdatedUtc);
                    })
                    .ToList();

                if (alertsOnly == true)
                {
                    summaries = summaries
                        .Where(item => item.AlertState is NotAppliedStatusCode or PartiallyAppliedStatusCode)
                        .ToList();
                }

                return Results.Ok(summaries);
            });

        group.MapGet(
            "/donations/{donationId:guid}",
            async (Guid donationId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var donation = await dbContext.FederationDonations
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == donationId, cancellationToken);

                if (donation is null)
                {
                    return Results.NotFound();
                }

                var detail = await BuildDonationDetailAsync(dbContext, donation, cancellationToken);
                return Results.Ok(detail);
            });

        group.MapPost(
            "/donations/{donationId:guid}/close",
            async (Guid donationId, CloseRecordRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var donation = await dbContext.FederationDonations
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == donationId, cancellationToken);

                if (donation is null)
                {
                    return Results.NotFound();
                }

                if (await AuditEventSupport.HasCloseEventAsync(dbContext, FederationModuleCode, FederationDonationEntityType, donation.Id, cancellationToken))
                {
                    return Results.Conflict(new { message = StateTransitionSupport.BuildDuplicateCloseEventMessage("la donación de federación") });
                }

                var currentStatus = donation.StatusCatalogEntry!;
                if (StateTransitionSupport.IsTerminal(currentStatus))
                {
                    return Results.Conflict(new { message = StateTransitionSupport.BuildTerminalCloseMessage("la donación de federación", currentStatus) });
                }

                if (!currentStatus.IsClosed)
                {
                    var closedStatus = await AuditEventSupport.ResolveContextStatusByCodeAsync(
                        dbContext,
                        FederationModuleCode,
                        FederationDonationContextCode,
                        ClosedStatusCode,
                        cancellationToken);

                    if (closedStatus is null)
                    {
                        return Results.ValidationProblem(
                            new Dictionary<string, string[]>
                            {
                                ["statusCatalogEntryId"] = ["The closed status for federation donations is not configured."]
                            });
                    }

                    donation.SyncStatus(closedStatus.Id);
                    currentStatus = closedStatus;
                }

                var reason = NormalizeOptionalText(request.Reason);
                var auditEvent = AuditEventSupport.CreateAuditEvent(
                    FederationModuleCode,
                    FederationModuleName,
                    FederationDonationEntityType,
                    donation.Id,
                    AuditEventSupport.FormalCloseActionType,
                    donation.DonorName,
                    reason is null
                        ? $"Cierre formal registrado para la donación de federación {donation.Reference}."
                        : $"Cierre formal registrado para la donación de federación {donation.Reference}. Motivo: {reason}.",
                    currentStatus.StatusCode,
                    donation.Reference,
                    "/federation",
                    isCloseEvent: true,
                    metadata: new
                    {
                        reason,
                        donation.BaseAmount
                    });

                dbContext.AuditEvents.Add(auditEvent);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Ok(
                    new CloseRecordResponse(
                        auditEvent.Id,
                        FederationModuleCode,
                        FederationModuleName,
                        FederationDonationEntityType,
                        donation.Id.ToString(),
                        currentStatus.StatusCode,
                        currentStatus.StatusName,
                        auditEvent.OccurredUtc,
                        reason));
            });

        group.MapPost(
            "/donations",
            async (CreateFederationDonationRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var errors = ValidateCreateFederationDonationRequest(request);
                var donationStatus = await ResolveModuleStatusAsync(
                    request.StatusCatalogEntryId,
                    FederationDonationContextCode,
                    dbContext,
                    cancellationToken);

                if (donationStatus is null)
                {
                    errors["statusCatalogEntryId"] = ["The selected federation donation status does not exist."];
                }
                else if (donationStatus.StatusCode is not NotAppliedStatusCode and not ClosedStatusCode)
                {
                    errors["statusCatalogEntryId"] =
                    [
                        "At creation time only the statuses NOT_APPLIED and CLOSED are allowed."
                    ];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var donation = new FederationDonation(
                    request.DonorName,
                    request.DonationDate,
                    request.DonationType,
                    request.BaseAmount,
                    request.Reference,
                    request.Notes,
                    request.StatusCatalogEntryId);

                dbContext.FederationDonations.Add(donation);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        FederationModuleCode,
                        FederationModuleName,
                        FederationDonationEntityType,
                        donation.Id,
                        "CREATED",
                        donation.DonorName,
                        $"Donación de federación registrada por {donation.BaseAmount:0.##} con referencia {donation.Reference}.",
                        donationStatus!.StatusCode,
                        donation.Reference,
                        "/federation",
                        metadata: new
                        {
                            donation.DonationDate,
                            donation.DonationType
                        }));
                await dbContext.SaveChangesAsync(cancellationToken);

                var response = new FederationDonationSummaryResponse(
                    donation.Id,
                    donation.DonorName,
                    donation.DonationDate,
                    donation.DonationType,
                    donation.BaseAmount,
                    donation.Reference,
                    donation.Notes,
                    donation.StatusCatalogEntryId,
                    donationStatus!.StatusCode,
                    donationStatus.StatusName,
                    donationStatus.IsClosed,
                    donationStatus.AlertsEnabledByDefault,
                    0,
                    donation.BaseAmount,
                    0,
                    0,
                    0,
                    0,
                    GetDonationAlertState(donationStatus, 0, donation.BaseAmount),
                    donation.CreatedUtc,
                    donation.UpdatedUtc);

                return Results.Created($"/api/federation/donations/{donation.Id}", response);
            });

        group.MapGet(
            "/donations/{donationId:guid}/applications",
            async (Guid donationId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var donationExists = await dbContext.FederationDonations
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == donationId, cancellationToken);

                if (!donationExists)
                {
                    return Results.NotFound();
                }

                var applications = await dbContext.FederationDonationApplications
                    .AsNoTracking()
                    .Where(item => item.FederationDonationId == donationId)
                    .Include(item => item.StatusCatalogEntry)
                    .OrderByDescending(item => item.ApplicationDate)
                    .ThenByDescending(item => item.CreatedUtc)
                    .ToListAsync(cancellationToken);

                var evidenceLookup = await BuildEvidenceLookupAsync(dbContext, applications, cancellationToken);
                var commissionLookup = await BuildCommissionLookupAsync(dbContext, applications, cancellationToken);

                return Results.Ok(
                    applications
                        .Select(item => MapDonationApplicationResponse(
                            item,
                            evidenceLookup.GetValueOrDefault(item.Id) ?? [],
                            commissionLookup.GetValueOrDefault(item.Id) ?? []))
                        .ToList());
            });

        group.MapPost(
            "/donations/{donationId:guid}/applications",
            async (Guid donationId, CreateFederationDonationApplicationRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var donation = await dbContext.FederationDonations
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == donationId, cancellationToken);

                if (donation is null)
                {
                    return Results.NotFound();
                }

                if (StateTransitionSupport.IsTerminal(donation.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        new
                        {
                            message = StateTransitionSupport.BuildTerminalMutationMessage(
                                $"la donación de federación {donation.Reference}",
                                "registrar una aplicación",
                                donation.StatusCatalogEntry!)
                        });
                }

                var errors = ValidateCreateFederationDonationApplicationRequest(request);
                var applicationStatus = await ResolveModuleStatusAsync(
                    request.StatusCatalogEntryId,
                    FederationDonationApplicationContextCode,
                    dbContext,
                    cancellationToken);

                if (applicationStatus is null)
                {
                    errors["statusCatalogEntryId"] = ["The selected federation application status does not exist."];
                }

                var currentAppliedAmount = await dbContext.FederationDonationApplications
                    .AsNoTracking()
                    .Where(item => item.FederationDonationId == donationId)
                    .SumAsync(item => item.AppliedAmount, cancellationToken);

                var nextAppliedAmount = currentAppliedAmount + decimal.Round(request.AppliedAmount, 2, MidpointRounding.AwayFromZero);
                if (nextAppliedAmount > donation.BaseAmount)
                {
                    errors["appliedAmount"] = ["AppliedAmount cannot exceed the remaining federation donation amount."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var application = new FederationDonationApplication(
                    donationId,
                    request.BeneficiaryOrDestinationName,
                    request.ApplicationDate,
                    request.AppliedAmount,
                    request.StatusCatalogEntryId,
                    request.VerificationDetails,
                    request.ClosingDetails);

                dbContext.FederationDonationApplications.Add(application);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        FederationModuleCode,
                        FederationModuleName,
                        FederationDonationApplicationEntityType,
                        application.Id,
                        "APPLIED",
                        application.BeneficiaryOrDestinationName,
                        $"Aplicación registrada por {application.AppliedAmount:0.##} para la donación {donation.Reference}.",
                        applicationStatus!.StatusCode,
                        donation.Reference,
                        "/federation",
                        metadata: new
                        {
                            application.FederationDonationId,
                            application.ApplicationDate
                        }));
                await dbContext.SaveChangesAsync(cancellationToken);

                var calculatedStatusId = await ResolveCalculatedDonationStatusIdAsync(
                    nextAppliedAmount,
                    donation.BaseAmount,
                    dbContext,
                    cancellationToken);

                donation.SyncStatus(calculatedStatusId);
                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Created(
                    $"/api/federation/donations/{donationId}/applications/{application.Id}",
                    new FederationDonationApplicationResponse(
                        application.Id,
                        application.FederationDonationId,
                        application.BeneficiaryOrDestinationName,
                        application.ApplicationDate,
                        application.AppliedAmount,
                        application.StatusCatalogEntryId,
                        applicationStatus!.StatusCode,
                        applicationStatus.StatusName,
                        applicationStatus.IsClosed,
                        application.VerificationDetails,
                        application.ClosingDetails,
                        0,
                        0,
                        [],
                        [],
                        application.CreatedUtc));
            });

        group.MapGet(
            "/applications/{applicationId:guid}/evidences",
            async (Guid applicationId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var applicationExists = await dbContext.FederationDonationApplications
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == applicationId, cancellationToken);

                if (!applicationExists)
                {
                    return Results.NotFound();
                }

                var evidences = await dbContext.FederationDonationApplicationEvidences
                    .AsNoTracking()
                    .Where(item => item.FederationDonationApplicationId == applicationId)
                    .Include(item => item.EvidenceType)
                    .OrderByDescending(item => item.UploadedUtc)
                    .ToListAsync(cancellationToken);

                return Results.Ok(evidences.Select(MapDonationApplicationEvidenceResponse).ToList());
            });

        group.MapPost(
            "/applications/{applicationId:guid}/evidences",
            async ([FromRoute] Guid applicationId, [FromForm] CreateFederationDonationApplicationEvidenceRequest request, PlatformDbContext dbContext, IFederationDonationApplicationEvidenceStorage evidenceStorage, CancellationToken cancellationToken) =>
            {
                var application = await dbContext.FederationDonationApplications
                    .AsNoTracking()
                    .Include(item => item.FederationDonation)
                    .ThenInclude(item => item!.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == applicationId, cancellationToken);

                if (application is null)
                {
                    return Results.NotFound();
                }

                if (StateTransitionSupport.IsTerminal(application.FederationDonation?.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        new
                        {
                            message = StateTransitionSupport.BuildTerminalMutationMessage(
                                $"la donación de federación {application.FederationDonation!.Reference}",
                                "adjuntar una evidencia",
                                application.FederationDonation.StatusCatalogEntry!)
                        });
                }

                var errors = ValidateCreateFederationDonationApplicationEvidenceRequest(request);
                var evidenceType = await dbContext.EvidenceTypes
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == request.EvidenceTypeId, cancellationToken);

                if (evidenceType is null)
                {
                    errors["evidenceTypeId"] = ["The selected federation evidence type does not exist."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                await using var fileContent = request.File!.OpenReadStream();
                var storedEvidence = await evidenceStorage.SaveAsync(
                    applicationId,
                    request.File.FileName,
                    request.File.ContentType,
                    fileContent,
                    cancellationToken);

                try
                {
                    var evidence = new FederationDonationApplicationEvidence(
                        applicationId,
                        request.EvidenceTypeId,
                        request.Description,
                        storedEvidence.OriginalFileName,
                        storedEvidence.RelativePath,
                        storedEvidence.ContentType,
                        storedEvidence.SizeBytes,
                        storedEvidence.UploadedUtc);

                    dbContext.FederationDonationApplicationEvidences.Add(evidence);
                    dbContext.StoredDocuments.Add(
                        StoredDocumentSupport.CreateStoredDocument(
                            FederationModuleCode,
                            FederationDonationEvidenceAreaCode,
                            FederationDonationEvidenceEntityType,
                            evidence.Id,
                            storedEvidence.OriginalFileName,
                            storedEvidence.RelativePath,
                            storedEvidence.ContentType,
                            storedEvidence.SizeBytes,
                            storedEvidence.UploadedUtc,
                            storedEvidence.Sha256Hex));
                    dbContext.AuditEvents.Add(
                        AuditEventSupport.CreateAuditEvent(
                            FederationModuleCode,
                            FederationModuleName,
                            FederationDonationEvidenceEntityType,
                            evidence.Id,
                            "ATTACHED",
                            evidence.OriginalFileName,
                            "Evidencia registrada para aplicación de federación.",
                            null,
                            evidence.OriginalFileName,
                            "/federation",
                            metadata: new
                            {
                                evidence.FederationDonationApplicationId,
                                evidence.EvidenceTypeId,
                                evidence.FileSizeBytes
                            }));
                    await dbContext.SaveChangesAsync(cancellationToken);

                    return Results.Created(
                        $"/api/federation/applications/{applicationId}/evidences/{evidence.Id}",
                        new FederationDonationApplicationEvidenceResponse(
                            evidence.Id,
                            evidence.FederationDonationApplicationId,
                            evidence.EvidenceTypeId,
                            evidenceType!.Code,
                            evidenceType.Name,
                            evidence.Description,
                            evidence.OriginalFileName,
                            evidence.ContentType,
                            evidence.FileSizeBytes,
                            evidence.UploadedUtc));
                }
                catch
                {
                    try
                    {
                        await evidenceStorage.DeleteIfExistsAsync(storedEvidence.RelativePath, cancellationToken);
                    }
                    catch
                    {
                    }

                    throw;
                }
            })
            .DisableAntiforgery();

        group.MapGet(
            "/applications/evidences/{evidenceId:guid}/download",
            async (Guid evidenceId, PlatformDbContext dbContext, IFederationDonationApplicationEvidenceStorage evidenceStorage, IDocumentBinaryStore documentBinaryStore, CancellationToken cancellationToken) =>
            {
                var evidence = await dbContext.FederationDonationApplicationEvidences
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == evidenceId, cancellationToken);

                if (evidence is null)
                {
                    return Results.NotFound();
                }

                var storedDocument = await StoredDocumentSupport.FindStoredDocumentAsync(
                    dbContext,
                    FederationDonationEvidenceAreaCode,
                    FederationDonationEvidenceEntityType,
                    evidence.Id,
                    cancellationToken);

                var relativePath = storedDocument?.StoredRelativePath ?? evidence.StoredRelativePath;
                var originalFileName = storedDocument?.OriginalFileName ?? evidence.OriginalFileName;
                var contentType = storedDocument?.ContentType ?? evidence.ContentType;
                var expectedSizeBytes = storedDocument?.SizeBytes ?? evidence.FileSizeBytes;
                var inspection = await documentBinaryStore.InspectAsync(
                    FederationDonationEvidenceAreaCode,
                    relativePath,
                    expectedSizeBytes,
                    cancellationToken);

                if (!string.Equals(inspection.IntegrityState, "VALID", StringComparison.Ordinal))
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Inconsistencia documental detectada",
                        detail: $"La evidencia de federación no puede descargarse. Estado de integridad: {inspection.IntegrityState}.");
                }

                var download = await evidenceStorage.OpenReadAsync(
                    relativePath,
                    originalFileName,
                    contentType,
                    cancellationToken);

                if (download is null)
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Inconsistencia documental detectada",
                        detail: "La evidencia física no se encuentra disponible en el storage local.");
                }

                return Results.File(download.Content, download.ContentType, download.OriginalFileName);
            });

        group.MapGet(
            "/applications/{applicationId:guid}/commissions",
            async (Guid applicationId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var applicationExists = await dbContext.FederationDonationApplications
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == applicationId, cancellationToken);

                if (!applicationExists)
                {
                    return Results.NotFound();
                }

                var commissions = await dbContext.FederationDonationApplicationCommissions
                    .AsNoTracking()
                    .Where(item => item.FederationDonationApplicationId == applicationId)
                    .Include(item => item.CommissionType)
                    .OrderByDescending(item => item.CreatedUtc)
                    .ToListAsync(cancellationToken);

                return Results.Ok(commissions.Select(MapDonationApplicationCommissionResponse).ToList());
            });

        group.MapPost(
            "/applications/{applicationId:guid}/commissions",
            async (Guid applicationId, CreateFederationDonationApplicationCommissionRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var application = await dbContext.FederationDonationApplications
                    .AsNoTracking()
                    .Include(item => item.FederationDonation)
                    .ThenInclude(item => item!.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == applicationId, cancellationToken);

                if (application is null)
                {
                    return Results.NotFound();
                }

                if (StateTransitionSupport.IsTerminal(application.FederationDonation?.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        new
                        {
                            message = StateTransitionSupport.BuildTerminalMutationMessage(
                                $"la donación de federación {application.FederationDonation!.Reference}",
                                "registrar una comisión",
                                application.FederationDonation.StatusCatalogEntry!)
                        });
                }

                var errors = ValidateCreateFederationDonationApplicationCommissionRequest(request);
                var commissionType = await dbContext.CommissionTypes
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == request.CommissionTypeId, cancellationToken);

                if (commissionType is null)
                {
                    errors["commissionTypeId"] = ["The selected federation commission type does not exist."];
                }

                if (request.RecipientContactId is Guid recipientContactId)
                {
                    var recipientExists = await dbContext.Contacts
                        .AnyAsync(item => item.Id == recipientContactId, cancellationToken);

                    if (!recipientExists)
                    {
                        errors["recipientContactId"] = ["The selected federation commission recipient does not exist."];
                    }
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var commission = new FederationDonationApplicationCommission(
                    applicationId,
                    request.CommissionTypeId,
                    request.RecipientCategory,
                    request.RecipientContactId,
                    request.RecipientName,
                    request.BaseAmount,
                    request.CommissionAmount,
                    request.Notes);

                dbContext.FederationDonationApplicationCommissions.Add(commission);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        FederationModuleCode,
                        FederationModuleName,
                        FederationDonationCommissionEntityType,
                        commission.Id,
                        "REGISTERED",
                        commission.RecipientName,
                        $"Comisión registrada por {commission.CommissionAmount:0.##} para la aplicación de donación de federación.",
                        null,
                        commission.RecipientName,
                        "/federation",
                        metadata: new
                        {
                            commission.FederationDonationApplicationId,
                            commission.CommissionTypeId,
                            commission.RecipientCategory
                        }));

                if (request.RecipientContactId is Guid validRecipientContactId)
                {
                    dbContext.ContactParticipations.Add(
                        new ContactParticipation(
                            validRecipientContactId,
                            FederationModuleCode,
                            "FEDERATION_DONATION_APPLICATION_COMMISSION_RECIPIENT",
                            commission.Id.ToString(),
                            "Destinatario de comision",
                            $"Aplicacion vinculada: {application.BeneficiaryOrDestinationName}"));
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Created(
                    $"/api/federation/applications/{applicationId}/commissions/{commission.Id}",
                    new FederationDonationApplicationCommissionResponse(
                        commission.Id,
                        commission.FederationDonationApplicationId,
                        commission.CommissionTypeId,
                        commissionType!.Code,
                        commissionType.Name,
                        commission.RecipientCategory,
                        commission.RecipientContactId,
                        commission.RecipientName,
                        commission.BaseAmount,
                        commission.CommissionAmount,
                        commission.Notes,
                        commission.CreatedUtc));
            });

        return app;
    }

    private static async Task<FederationActionDetailResponse> BuildActionDetailAsync(
        PlatformDbContext dbContext,
        FederationAction action,
        CancellationToken cancellationToken)
    {
        var participants = await dbContext.FederationActionParticipants
            .AsNoTracking()
            .Where(item => item.FederationActionId == action.Id)
            .Include(item => item.Contact)
            .ThenInclude(contact => contact!.ContactType)
            .OrderBy(item => item.ParticipantSide)
            .ThenBy(item => item.ParticipantName)
            .ToListAsync(cancellationToken);

        return new FederationActionDetailResponse(
            action.Id,
            action.ActionTypeCode,
            MapActionTypeName(action.ActionTypeCode),
            action.CounterpartyOrInstitution,
            action.ActionDate,
            action.Objective,
            action.StatusCatalogEntryId,
            action.StatusCatalogEntry!.StatusCode,
            action.StatusCatalogEntry.StatusName,
            action.StatusCatalogEntry.IsClosed,
            action.StatusCatalogEntry.AlertsEnabledByDefault,
            GetActionAlertState(action.StatusCatalogEntry),
            action.Notes,
            action.CreatedUtc,
            action.UpdatedUtc,
            participants.Select(MapActionParticipantResponse).ToList());
    }

    private static async Task<FederationDonationDetailResponse> BuildDonationDetailAsync(
        PlatformDbContext dbContext,
        FederationDonation donation,
        CancellationToken cancellationToken)
    {
        var applications = await dbContext.FederationDonationApplications
            .AsNoTracking()
            .Where(item => item.FederationDonationId == donation.Id)
            .Include(item => item.StatusCatalogEntry)
            .OrderByDescending(item => item.ApplicationDate)
            .ThenByDescending(item => item.CreatedUtc)
            .ToListAsync(cancellationToken);

        var evidenceLookup = await BuildEvidenceLookupAsync(dbContext, applications, cancellationToken);
        var commissionLookup = await BuildCommissionLookupAsync(dbContext, applications, cancellationToken);
        var metrics = CalculateProgress(donation.BaseAmount, applications);
        var alertState = GetDonationAlertState(donation.StatusCatalogEntry!, metrics.AppliedAmountTotal, donation.BaseAmount);
        var evidenceCount = applications.Sum(item => (evidenceLookup.GetValueOrDefault(item.Id) ?? []).Count);
        var commissionCount = applications.Sum(item => (commissionLookup.GetValueOrDefault(item.Id) ?? []).Count);

        return new FederationDonationDetailResponse(
            donation.Id,
            donation.DonorName,
            donation.DonationDate,
            donation.DonationType,
            donation.BaseAmount,
            donation.Reference,
            donation.Notes,
            donation.StatusCatalogEntryId,
            donation.StatusCatalogEntry!.StatusCode,
            donation.StatusCatalogEntry.StatusName,
            donation.StatusCatalogEntry.IsClosed,
            donation.StatusCatalogEntry.AlertsEnabledByDefault,
            metrics.AppliedAmountTotal,
            metrics.RemainingAmount,
            metrics.AppliedPercentage,
            commissionCount,
            evidenceCount,
            alertState,
            donation.CreatedUtc,
            donation.UpdatedUtc,
            applications
                .Select(item => MapDonationApplicationResponse(
                    item,
                    evidenceLookup.GetValueOrDefault(item.Id) ?? [],
                    commissionLookup.GetValueOrDefault(item.Id) ?? []))
                .ToList());
    }

    private static async Task<List<FederationActionAlertResponse>> BuildActionAlertsAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var actions = await dbContext.FederationActions
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .OrderByDescending(item => item.ActionDate)
            .ThenBy(item => item.CounterpartyOrInstitution)
            .ToListAsync(cancellationToken);

        return actions
            .Select(action => new
            {
                Action = action,
                AlertState = GetActionAlertState(action.StatusCatalogEntry!)
            })
            .Where(item => item.AlertState is InProcessStatusCode or FollowUpPendingStatusCode)
            .Select(item => new FederationActionAlertResponse(
                item.Action.Id,
                item.Action.ActionTypeCode,
                MapActionTypeName(item.Action.ActionTypeCode),
                item.Action.CounterpartyOrInstitution,
                item.Action.ActionDate,
                item.Action.StatusCatalogEntry!.StatusCode,
                item.Action.StatusCatalogEntry.StatusName,
                item.AlertState))
            .ToList();
    }

    private static async Task<List<FederationDonationAlertResponse>> BuildDonationAlertsAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var donations = await dbContext.FederationDonations
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .OrderByDescending(item => item.DonationDate)
            .ThenBy(item => item.DonorName)
            .ToListAsync(cancellationToken);

        var donationIds = donations.Select(item => item.Id).ToArray();
        var applications = donationIds.Length == 0
            ? []
            : await dbContext.FederationDonationApplications
                .AsNoTracking()
                .Where(item => donationIds.Contains(item.FederationDonationId))
                .ToListAsync(cancellationToken);

        return donations
            .Select(donation =>
            {
                var donationApplications = applications.Where(item => item.FederationDonationId == donation.Id).ToList();
                var metrics = CalculateProgress(donation.BaseAmount, donationApplications);
                var alertState = GetDonationAlertState(donation.StatusCatalogEntry!, metrics.AppliedAmountTotal, donation.BaseAmount);

                return new FederationDonationAlertResponse(
                    donation.Id,
                    donation.DonorName,
                    donation.DonationType,
                    donation.StatusCatalogEntry!.StatusCode,
                    donation.StatusCatalogEntry.StatusName,
                    donation.BaseAmount,
                    metrics.AppliedAmountTotal,
                    metrics.RemainingAmount,
                    metrics.AppliedPercentage,
                    alertState);
            })
            .Where(item => item.AlertState is NotAppliedStatusCode or PartiallyAppliedStatusCode)
            .ToList();
    }

    private static FederationActionParticipantResponse MapActionParticipantResponse(FederationActionParticipant participant)
    {
        return new FederationActionParticipantResponse(
            participant.Id,
            participant.FederationActionId,
            participant.ContactId,
            participant.ParticipantSide,
            participant.Contact!.ContactType!.Code,
            participant.Contact.ContactType.Name,
            participant.ParticipantName,
            participant.OrganizationOrDependency,
            participant.RoleTitle,
            participant.Notes,
            participant.CreatedUtc);
    }

    private static FederationDonationApplicationResponse MapDonationApplicationResponse(
        FederationDonationApplication application,
        IReadOnlyList<FederationDonationApplicationEvidence> evidences,
        IReadOnlyList<FederationDonationApplicationCommission> commissions)
    {
        return new FederationDonationApplicationResponse(
            application.Id,
            application.FederationDonationId,
            application.BeneficiaryOrDestinationName,
            application.ApplicationDate,
            application.AppliedAmount,
            application.StatusCatalogEntryId,
            application.StatusCatalogEntry!.StatusCode,
            application.StatusCatalogEntry.StatusName,
            application.StatusCatalogEntry.IsClosed,
            application.VerificationDetails,
            application.ClosingDetails,
            evidences.Count,
            commissions.Count,
            evidences.Select(MapDonationApplicationEvidenceResponse).ToList(),
            commissions.Select(MapDonationApplicationCommissionResponse).ToList(),
            application.CreatedUtc);
    }

    private static FederationDonationApplicationEvidenceResponse MapDonationApplicationEvidenceResponse(FederationDonationApplicationEvidence evidence)
    {
        return new FederationDonationApplicationEvidenceResponse(
            evidence.Id,
            evidence.FederationDonationApplicationId,
            evidence.EvidenceTypeId,
            evidence.EvidenceType!.Code,
            evidence.EvidenceType.Name,
            evidence.Description,
            evidence.OriginalFileName,
            evidence.ContentType,
            evidence.FileSizeBytes,
            evidence.UploadedUtc);
    }

    private static FederationDonationApplicationCommissionResponse MapDonationApplicationCommissionResponse(FederationDonationApplicationCommission commission)
    {
        return new FederationDonationApplicationCommissionResponse(
            commission.Id,
            commission.FederationDonationApplicationId,
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

    private static async Task<Dictionary<Guid, int>> BuildEvidenceCountLookupAsync(
        PlatformDbContext dbContext,
        IReadOnlyList<FederationDonationApplication> applications,
        CancellationToken cancellationToken)
    {
        if (applications.Count == 0)
        {
            return [];
        }

        var applicationIds = applications.Select(item => item.Id).ToArray();

        return await dbContext.FederationDonationApplicationEvidences
            .AsNoTracking()
            .Where(item => applicationIds.Contains(item.FederationDonationApplicationId))
            .GroupBy(item => item.FederationDonationApplicationId)
            .Select(grouping => new { FederationDonationApplicationId = grouping.Key, Count = grouping.Count() })
            .ToDictionaryAsync(item => item.FederationDonationApplicationId, item => item.Count, cancellationToken);
    }

    private static async Task<Dictionary<Guid, int>> BuildCommissionCountLookupAsync(
        PlatformDbContext dbContext,
        IReadOnlyList<FederationDonationApplication> applications,
        CancellationToken cancellationToken)
    {
        if (applications.Count == 0)
        {
            return [];
        }

        var applicationIds = applications.Select(item => item.Id).ToArray();

        return await dbContext.FederationDonationApplicationCommissions
            .AsNoTracking()
            .Where(item => applicationIds.Contains(item.FederationDonationApplicationId))
            .GroupBy(item => item.FederationDonationApplicationId)
            .Select(grouping => new { FederationDonationApplicationId = grouping.Key, Count = grouping.Count() })
            .ToDictionaryAsync(item => item.FederationDonationApplicationId, item => item.Count, cancellationToken);
    }

    private static async Task<Dictionary<Guid, List<FederationDonationApplicationEvidence>>> BuildEvidenceLookupAsync(
        PlatformDbContext dbContext,
        IReadOnlyList<FederationDonationApplication> applications,
        CancellationToken cancellationToken)
    {
        if (applications.Count == 0)
        {
            return [];
        }

        var applicationIds = applications.Select(item => item.Id).ToArray();
        var evidences = await dbContext.FederationDonationApplicationEvidences
            .AsNoTracking()
            .Where(item => applicationIds.Contains(item.FederationDonationApplicationId))
            .Include(item => item.EvidenceType)
            .OrderByDescending(item => item.UploadedUtc)
            .ToListAsync(cancellationToken);

        return evidences
            .GroupBy(item => item.FederationDonationApplicationId)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
    }

    private static async Task<Dictionary<Guid, List<FederationDonationApplicationCommission>>> BuildCommissionLookupAsync(
        PlatformDbContext dbContext,
        IReadOnlyList<FederationDonationApplication> applications,
        CancellationToken cancellationToken)
    {
        if (applications.Count == 0)
        {
            return [];
        }

        var applicationIds = applications.Select(item => item.Id).ToArray();
        var commissions = await dbContext.FederationDonationApplicationCommissions
            .AsNoTracking()
            .Where(item => applicationIds.Contains(item.FederationDonationApplicationId))
            .Include(item => item.CommissionType)
            .OrderByDescending(item => item.CreatedUtc)
            .ToListAsync(cancellationToken);

        return commissions
            .GroupBy(item => item.FederationDonationApplicationId)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
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
                    && item.ModuleCode == FederationModuleCode
                    && item.ContextCode == contextCode,
                cancellationToken);
    }

    private static async Task<int> ResolveCalculatedDonationStatusIdAsync(
        decimal appliedAmountTotal,
        decimal baseAmount,
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var statusCode = appliedAmountTotal <= 0
            ? NotAppliedStatusCode
            : appliedAmountTotal >= baseAmount
                ? AppliedStatusCode
                : PartiallyAppliedStatusCode;

        var statusEntry = await dbContext.ModuleStatusCatalogEntries
            .AsNoTracking()
            .SingleAsync(
                item => item.ModuleCode == FederationModuleCode
                    && item.ContextCode == FederationDonationContextCode
                    && item.StatusCode == statusCode,
                cancellationToken);

        return statusEntry.Id;
    }

    private static (decimal AppliedAmountTotal, decimal RemainingAmount, decimal AppliedPercentage) CalculateProgress(
        decimal baseAmount,
        IReadOnlyList<FederationDonationApplication> applications)
    {
        var appliedAmountTotal = decimal.Round(applications.Sum(item => item.AppliedAmount), 2, MidpointRounding.AwayFromZero);
        var remainingAmount = decimal.Round(Math.Max(baseAmount - appliedAmountTotal, 0), 2, MidpointRounding.AwayFromZero);
        var appliedPercentage = baseAmount <= 0
            ? 0
            : decimal.Round((appliedAmountTotal / baseAmount) * 100m, 2, MidpointRounding.AwayFromZero);

        return (appliedAmountTotal, remainingAmount, appliedPercentage);
    }

    private static string GetActionAlertState(ModuleStatusCatalogEntry status)
    {
        if (status.IsClosed || !status.AlertsEnabledByDefault)
        {
            return NoAlertState;
        }

        return status.StatusCode switch
        {
            InProcessStatusCode => InProcessStatusCode,
            FollowUpPendingStatusCode => FollowUpPendingStatusCode,
            _ => NoAlertState
        };
    }

    private static string GetDonationAlertState(
        ModuleStatusCatalogEntry status,
        decimal appliedAmountTotal,
        decimal baseAmount)
    {
        if (status.IsClosed || !status.AlertsEnabledByDefault)
        {
            return NoAlertState;
        }

        return status.StatusCode switch
        {
            NotAppliedStatusCode => NotAppliedStatusCode,
            PartiallyAppliedStatusCode => PartiallyAppliedStatusCode,
            AppliedStatusCode when appliedAmountTotal < baseAmount => PartiallyAppliedStatusCode,
            _ => NoAlertState
        };
    }

    private static Dictionary<string, string[]> ValidateCreateFederationActionRequest(CreateFederationActionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.ActionTypeCode))
        {
            errors["actionTypeCode"] = ["ActionTypeCode is required."];
        }
        else if (!AllowedActionTypeCodes.Contains(request.ActionTypeCode.Trim().ToUpperInvariant()))
        {
            errors["actionTypeCode"] = ["The selected federation action type is not supported."];
        }

        if (string.IsNullOrWhiteSpace(request.CounterpartyOrInstitution))
        {
            errors["counterpartyOrInstitution"] = ["CounterpartyOrInstitution is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Objective))
        {
            errors["objective"] = ["Objective is required."];
        }

        if (request.StatusCatalogEntryId <= 0)
        {
            errors["statusCatalogEntryId"] = ["StatusCatalogEntryId is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateCreateFederationActionParticipantRequest(CreateFederationActionParticipantRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.ContactId == Guid.Empty)
        {
            errors["contactId"] = ["ContactId is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ParticipantSide))
        {
            errors["participantSide"] = ["ParticipantSide is required."];
        }
        else if (!AllowedParticipantSides.Contains(request.ParticipantSide.Trim().ToUpperInvariant()))
        {
            errors["participantSide"] = ["ParticipantSide must be INTERNAL or EXTERNAL."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateCreateFederationDonationRequest(CreateFederationDonationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.DonorName))
        {
            errors["donorName"] = ["DonorName is required."];
        }

        if (string.IsNullOrWhiteSpace(request.DonationType))
        {
            errors["donationType"] = ["DonationType is required."];
        }

        if (request.BaseAmount <= 0)
        {
            errors["baseAmount"] = ["BaseAmount must be greater than zero."];
        }

        if (string.IsNullOrWhiteSpace(request.Reference))
        {
            errors["reference"] = ["Reference is required."];
        }

        if (request.StatusCatalogEntryId <= 0)
        {
            errors["statusCatalogEntryId"] = ["StatusCatalogEntryId is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateCreateFederationDonationApplicationRequest(CreateFederationDonationApplicationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.BeneficiaryOrDestinationName))
        {
            errors["beneficiaryOrDestinationName"] = ["BeneficiaryOrDestinationName is required."];
        }

        if (request.AppliedAmount <= 0)
        {
            errors["appliedAmount"] = ["AppliedAmount must be greater than zero."];
        }

        if (request.StatusCatalogEntryId <= 0)
        {
            errors["statusCatalogEntryId"] = ["StatusCatalogEntryId is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateCreateFederationDonationApplicationEvidenceRequest(CreateFederationDonationApplicationEvidenceRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.EvidenceTypeId <= 0)
        {
            errors["evidenceTypeId"] = ["EvidenceTypeId is required."];
        }

        if (request.File is null || request.File.Length <= 0)
        {
            errors["file"] = ["A non-empty evidence file is required."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateCreateFederationDonationApplicationCommissionRequest(CreateFederationDonationApplicationCommissionRequest request)
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
        else if (!AllowedRecipientCategories.Contains(request.RecipientCategory.Trim().ToUpperInvariant()))
        {
            errors["recipientCategory"] =
            [
                "RecipientCategory must be COMPANY, THIRD_PARTY or OTHER_PARTICIPANT."
            ];
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

    private static string MapActionTypeName(string actionTypeCode)
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

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
