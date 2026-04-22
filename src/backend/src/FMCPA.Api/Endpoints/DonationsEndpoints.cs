using FMCPA.Api.Contracts.Closeout;
using FMCPA.Api.Contracts.Donations;
using FMCPA.Api.Extensions;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Domain.Entities.Donations;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class DonationsEndpoints
{
    private const string DonationsModuleCode = "DONATARIAS";
    private const string DonationsModuleName = "Donatarias";
    private const string DonationContextCode = "DONATION";
    private const string DonationApplicationContextCode = "DONATION_APPLICATION";
    private const string DonationEntityType = "DONATION";
    private const string DonationApplicationEntityType = "DONATION_APPLICATION";
    private const string DonationEvidenceEntityType = "DONATION_APPLICATION_EVIDENCE";
    private const string DonationEvidenceAreaCode = DocumentAreaCodes.DonationsApplicationEvidences;
    private const string NotAppliedStatusCode = "NOT_APPLIED";
    private const string PartiallyAppliedStatusCode = "PARTIALLY_APPLIED";
    private const string AppliedStatusCode = "APPLIED";
    private const string ClosedStatusCode = "CLOSED";
    private const string NoAlertState = "NONE";

    public static IEndpointRouteBuilder MapDonationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/donations")
            .WithTags("Donations");

        group.MapGet(
            "/alerts",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var alerts = await BuildDonationAlertsAsync(dbContext, cancellationToken);
                return Results.Ok(alerts);
            });

        group.MapGet(
            "/applications/evidences/{evidenceId:guid}/download",
            async (Guid evidenceId, PlatformDbContext dbContext, IDonationApplicationEvidenceStorage evidenceStorage, IDocumentBinaryStore documentBinaryStore, CancellationToken cancellationToken) =>
            {
                var evidence = await dbContext.DonationApplicationEvidences
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == evidenceId, cancellationToken);

                if (evidence is null)
                {
                    return Results.NotFound();
                }

                var storedDocument = await StoredDocumentSupport.FindStoredDocumentAsync(
                    dbContext,
                    DonationEvidenceAreaCode,
                    DonationEvidenceEntityType,
                    evidence.Id,
                    cancellationToken);

                var relativePath = storedDocument?.StoredRelativePath ?? evidence.StoredRelativePath;
                var originalFileName = storedDocument?.OriginalFileName ?? evidence.OriginalFileName;
                var contentType = storedDocument?.ContentType ?? evidence.ContentType;
                var expectedSizeBytes = storedDocument?.SizeBytes ?? evidence.FileSizeBytes;
                var inspection = await documentBinaryStore.InspectAsync(
                    DonationEvidenceAreaCode,
                    relativePath,
                    expectedSizeBytes,
                    cancellationToken);

                if (!string.Equals(inspection.IntegrityState, "VALID", StringComparison.Ordinal))
                {
                    return Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Inconsistencia documental detectada",
                        detail: $"La evidencia de donatarias no puede descargarse. Estado de integridad: {inspection.IntegrityState}.");
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
            string.Empty,
            async (string? statusCode, bool? alertsOnly, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var donations = await dbContext.Donations
                    .AsNoTracking()
                    .Include(item => item.StatusCatalogEntry)
                    .OrderByDescending(item => item.DonationDate)
                    .ThenBy(item => item.DonorEntityName)
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
                    : await dbContext.DonationApplications
                        .AsNoTracking()
                        .Where(item => donationIds.Contains(item.DonationId))
                        .ToListAsync(cancellationToken);

                var evidenceCounts = await BuildEvidenceCountLookupAsync(dbContext, applications, cancellationToken);

                var summaries = donations
                    .Select(donation =>
                    {
                        var donationApplications = applications.Where(item => item.DonationId == donation.Id).ToList();
                        var metrics = CalculateProgress(donation.BaseAmount, donationApplications);
                        var alertState = GetDonationAlertState(donation.StatusCatalogEntry!, metrics.AppliedAmountTotal, donation.BaseAmount);
                        var evidenceCount = donationApplications.Sum(item => evidenceCounts.GetValueOrDefault(item.Id));

                        return new DonationSummaryResponse(
                            donation.Id,
                            donation.DonorEntityName,
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
            "/{donationId:guid}",
            async (Guid donationId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var donation = await dbContext.Donations
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

        group.MapGet(
            "/{donationId:guid}/progress",
            async (Guid donationId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var donation = await dbContext.Donations
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == donationId, cancellationToken);

                if (donation is null)
                {
                    return Results.NotFound();
                }

                var applications = await dbContext.DonationApplications
                    .AsNoTracking()
                    .Where(item => item.DonationId == donationId)
                    .ToListAsync(cancellationToken);

                var metrics = CalculateProgress(donation.BaseAmount, applications);

                return Results.Ok(
                    new DonationProgressResponse(
                        donation.Id,
                        donation.BaseAmount,
                        metrics.AppliedAmountTotal,
                        metrics.RemainingAmount,
                        metrics.AppliedPercentage,
                        applications.Count));
            });

        group.MapPost(
            "/{donationId:guid}/close",
            async (Guid donationId, CloseRecordRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var donation = await dbContext.Donations
                    .Include(item => item.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == donationId, cancellationToken);

                if (donation is null)
                {
                    return Results.NotFound();
                }

                if (await AuditEventSupport.HasCloseEventAsync(dbContext, DonationsModuleCode, DonationEntityType, donation.Id, cancellationToken))
                {
                    return Results.Conflict(new { message = StateTransitionSupport.BuildDuplicateCloseEventMessage("la donación") });
                }

                var currentStatus = donation.StatusCatalogEntry!;
                if (StateTransitionSupport.IsTerminal(currentStatus))
                {
                    return Results.Conflict(new { message = StateTransitionSupport.BuildTerminalCloseMessage("la donación", currentStatus) });
                }

                if (!currentStatus.IsClosed)
                {
                    var closedStatus = await AuditEventSupport.ResolveContextStatusByCodeAsync(
                        dbContext,
                        DonationsModuleCode,
                        DonationContextCode,
                        ClosedStatusCode,
                        cancellationToken);

                    if (closedStatus is null)
                    {
                        return Results.ValidationProblem(
                            new Dictionary<string, string[]>
                            {
                                ["statusCatalogEntryId"] = ["The closed status for donations is not configured."]
                            });
                    }

                    donation.SyncStatus(closedStatus.Id);
                    currentStatus = closedStatus;
                }

                var reason = NormalizeOptionalText(request.Reason);
                var auditEvent = AuditEventSupport.CreateAuditEvent(
                    DonationsModuleCode,
                    DonationsModuleName,
                    DonationEntityType,
                    donation.Id,
                    AuditEventSupport.FormalCloseActionType,
                    donation.DonorEntityName,
                    reason is null
                        ? $"Cierre formal registrado para la donación {donation.Reference}."
                        : $"Cierre formal registrado para la donación {donation.Reference}. Motivo: {reason}.",
                    currentStatus.StatusCode,
                    donation.Reference,
                    "/donatarias",
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
                        DonationsModuleCode,
                        DonationsModuleName,
                        DonationEntityType,
                        donation.Id.ToString(),
                        currentStatus.StatusCode,
                        currentStatus.StatusName,
                        auditEvent.OccurredUtc,
                        reason));
            });

        group.MapPost(
            string.Empty,
            async (CreateDonationRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var errors = ValidateCreateDonationRequest(request);
                var donationStatus = await ResolveModuleStatusAsync(
                    request.StatusCatalogEntryId,
                    DonationContextCode,
                    dbContext,
                    cancellationToken);

                if (donationStatus is null)
                {
                    errors["statusCatalogEntryId"] = ["The selected donation status does not exist."];
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

                var donation = new Donation(
                    request.DonorEntityName,
                    request.DonationDate,
                    request.DonationType,
                    request.BaseAmount,
                    request.Reference,
                    request.Notes,
                    request.StatusCatalogEntryId);

                dbContext.Donations.Add(donation);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        DonationsModuleCode,
                        DonationsModuleName,
                        DonationEntityType,
                        donation.Id,
                        "CREATED",
                        donation.DonorEntityName,
                        $"Donación registrada por {donation.BaseAmount:0.##} con referencia {donation.Reference}.",
                        donationStatus!.StatusCode,
                        donation.Reference,
                        "/donatarias",
                        metadata: new
                        {
                            donation.DonationDate,
                            donation.DonationType
                        }));
                await dbContext.SaveChangesAsync(cancellationToken);

                var response = new DonationSummaryResponse(
                    donation.Id,
                    donation.DonorEntityName,
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
                    GetDonationAlertState(donationStatus, 0, donation.BaseAmount),
                    donation.CreatedUtc,
                    donation.UpdatedUtc);

                return Results.Created($"/api/donations/{donation.Id}", response);
            });

        group.MapGet(
            "/{donationId:guid}/applications",
            async (Guid donationId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var donationExists = await dbContext.Donations
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == donationId, cancellationToken);

                if (!donationExists)
                {
                    return Results.NotFound();
                }

                var applications = await dbContext.DonationApplications
                    .AsNoTracking()
                    .Where(item => item.DonationId == donationId)
                    .Include(item => item.StatusCatalogEntry)
                    .OrderByDescending(item => item.ApplicationDate)
                    .ThenByDescending(item => item.CreatedUtc)
                    .ToListAsync(cancellationToken);

                var evidences = await BuildEvidenceLookupAsync(dbContext, applications, cancellationToken);

                var response = applications
                    .Select(item => MapDonationApplicationResponse(item, evidences.GetValueOrDefault(item.Id) ?? []))
                    .ToList();

                return Results.Ok(response);
            });

        group.MapPost(
            "/{donationId:guid}/applications",
            async (Guid donationId, CreateDonationApplicationRequest request, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var donation = await dbContext.Donations
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
                                $"la donación {donation.Reference}",
                                "registrar una aplicación",
                                donation.StatusCatalogEntry!)
                        });
                }

                var errors = ValidateCreateDonationApplicationRequest(request);
                var applicationStatus = await ResolveModuleStatusAsync(
                    request.StatusCatalogEntryId,
                    DonationApplicationContextCode,
                    dbContext,
                    cancellationToken);

                if (applicationStatus is null)
                {
                    errors["statusCatalogEntryId"] = ["The selected application status does not exist."];
                }

                if (request.ResponsibleContactId is Guid responsibleContactId)
                {
                    var contactExists = await dbContext.Contacts
                        .AnyAsync(item => item.Id == responsibleContactId, cancellationToken);

                    if (!contactExists)
                    {
                        errors["responsibleContactId"] = ["The selected responsible contact does not exist."];
                    }
                }

                var currentAppliedAmount = await dbContext.DonationApplications
                    .AsNoTracking()
                    .Where(item => item.DonationId == donationId)
                    .SumAsync(item => item.AppliedAmount, cancellationToken);

                var nextAppliedAmount = currentAppliedAmount + decimal.Round(request.AppliedAmount, 2, MidpointRounding.AwayFromZero);
                if (nextAppliedAmount > donation.BaseAmount)
                {
                    errors["appliedAmount"] = ["AppliedAmount cannot exceed the remaining donation amount."];
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var application = new DonationApplication(
                    donation.Id,
                    request.BeneficiaryName,
                    request.ResponsibleContactId,
                    request.ResponsibleName,
                    request.ApplicationDate,
                    request.AppliedAmount,
                    request.StatusCatalogEntryId,
                    request.VerificationDetails,
                    request.ClosingDetails);

                dbContext.DonationApplications.Add(application);
                dbContext.AuditEvents.Add(
                    AuditEventSupport.CreateAuditEvent(
                        DonationsModuleCode,
                        DonationsModuleName,
                        DonationApplicationEntityType,
                        application.Id,
                        "APPLIED",
                        application.BeneficiaryName,
                        $"Aplicación registrada por {application.AppliedAmount:0.##} para la donación {donation.Reference}.",
                        applicationStatus!.StatusCode,
                        donation.Reference,
                        "/donatarias",
                        metadata: new
                        {
                            application.DonationId,
                            application.ApplicationDate,
                            application.ResponsibleContactId
                        }));

                if (request.ResponsibleContactId is Guid validResponsibleContactId)
                {
                    dbContext.ContactParticipations.Add(
                        new ContactParticipation(
                            validResponsibleContactId,
                            DonationsModuleCode,
                            "DONATION_APPLICATION_RESPONSIBLE",
                            application.Id.ToString(),
                            "Responsable de aplicacion",
                            $"Donacion vinculada: {donation.DonorEntityName}"));
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                var calculatedStatusId = await ResolveCalculatedDonationStatusIdAsync(
                    nextAppliedAmount,
                    donation.BaseAmount,
                    dbContext,
                    cancellationToken);

                donation.SyncStatus(calculatedStatusId);
                await dbContext.SaveChangesAsync(cancellationToken);

                var response = new DonationApplicationResponse(
                    application.Id,
                    application.DonationId,
                    application.BeneficiaryName,
                    application.ResponsibleContactId,
                    application.ResponsibleName,
                    application.ApplicationDate,
                    application.AppliedAmount,
                    application.StatusCatalogEntryId,
                    applicationStatus!.StatusCode,
                    applicationStatus.StatusName,
                    applicationStatus.IsClosed,
                    application.VerificationDetails,
                    application.ClosingDetails,
                    0,
                    [],
                    application.CreatedUtc);

                return Results.Created($"/api/donations/{donationId}/applications/{application.Id}", response);
            });

        group.MapGet(
            "/applications/{applicationId:guid}/evidences",
            async (Guid applicationId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var applicationExists = await dbContext.DonationApplications
                    .AsNoTracking()
                    .AnyAsync(item => item.Id == applicationId, cancellationToken);

                if (!applicationExists)
                {
                    return Results.NotFound();
                }

                var evidences = await dbContext.DonationApplicationEvidences
                    .AsNoTracking()
                    .Where(item => item.DonationApplicationId == applicationId)
                    .Include(item => item.EvidenceType)
                    .OrderByDescending(item => item.UploadedUtc)
                    .ToListAsync(cancellationToken);

                return Results.Ok(evidences.Select(MapDonationApplicationEvidenceResponse).ToList());
            });

        group.MapPost(
            "/applications/{applicationId:guid}/evidences",
            async ([FromRoute] Guid applicationId, [FromForm] CreateDonationApplicationEvidenceRequest request, PlatformDbContext dbContext, IDonationApplicationEvidenceStorage evidenceStorage, CancellationToken cancellationToken) =>
            {
                var application = await dbContext.DonationApplications
                    .AsNoTracking()
                    .Include(item => item.Donation)
                    .ThenInclude(item => item!.StatusCatalogEntry)
                    .SingleOrDefaultAsync(item => item.Id == applicationId, cancellationToken);

                if (application is null)
                {
                    return Results.NotFound();
                }

                if (StateTransitionSupport.IsTerminal(application.Donation?.StatusCatalogEntry))
                {
                    return Results.Conflict(
                        new
                        {
                            message = StateTransitionSupport.BuildTerminalMutationMessage(
                                $"la donación {application.Donation!.Reference}",
                                "adjuntar una evidencia",
                                application.Donation.StatusCatalogEntry!)
                        });
                }

                var errors = ValidateCreateDonationApplicationEvidenceRequest(request);

                var evidenceType = await dbContext.EvidenceTypes
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == request.EvidenceTypeId, cancellationToken);

                if (evidenceType is null)
                {
                    errors["evidenceTypeId"] = ["The selected evidence type does not exist."];
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
                    var evidence = new DonationApplicationEvidence(
                        applicationId,
                        request.EvidenceTypeId,
                        request.Description,
                        storedEvidence.OriginalFileName,
                        storedEvidence.RelativePath,
                        storedEvidence.ContentType,
                        storedEvidence.SizeBytes,
                        storedEvidence.UploadedUtc);

                    dbContext.DonationApplicationEvidences.Add(evidence);
                    dbContext.StoredDocuments.Add(
                        StoredDocumentSupport.CreateStoredDocument(
                            DonationsModuleCode,
                            DonationEvidenceAreaCode,
                            DonationEvidenceEntityType,
                            evidence.Id,
                            storedEvidence.OriginalFileName,
                            storedEvidence.RelativePath,
                            storedEvidence.ContentType,
                            storedEvidence.SizeBytes,
                            storedEvidence.UploadedUtc,
                            storedEvidence.Sha256Hex));
                    dbContext.AuditEvents.Add(
                        AuditEventSupport.CreateAuditEvent(
                            DonationsModuleCode,
                            DonationsModuleName,
                            DonationEvidenceEntityType,
                            evidence.Id,
                            "ATTACHED",
                            evidence.OriginalFileName,
                            "Evidencia registrada para aplicación de donación.",
                            null,
                            evidence.OriginalFileName,
                            "/donatarias",
                            metadata: new
                            {
                                evidence.DonationApplicationId,
                                evidence.EvidenceTypeId,
                                evidence.FileSizeBytes
                            }));
                    await dbContext.SaveChangesAsync(cancellationToken);

                    var response = new DonationApplicationEvidenceResponse(
                        evidence.Id,
                        evidence.DonationApplicationId,
                        evidence.EvidenceTypeId,
                        evidenceType!.Code,
                        evidenceType.Name,
                        evidence.Description,
                        evidence.OriginalFileName,
                        evidence.ContentType,
                        evidence.FileSizeBytes,
                        evidence.UploadedUtc);

                    return Results.Created($"/api/donations/applications/{applicationId}/evidences/{evidence.Id}", response);
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

        return app;
    }

    private static async Task<DonationDetailResponse> BuildDonationDetailAsync(
        PlatformDbContext dbContext,
        Donation donation,
        CancellationToken cancellationToken)
    {
        var applications = await dbContext.DonationApplications
            .AsNoTracking()
            .Where(item => item.DonationId == donation.Id)
            .Include(item => item.StatusCatalogEntry)
            .OrderByDescending(item => item.ApplicationDate)
            .ThenByDescending(item => item.CreatedUtc)
            .ToListAsync(cancellationToken);

        var evidenceLookup = await BuildEvidenceLookupAsync(dbContext, applications, cancellationToken);
        var metrics = CalculateProgress(donation.BaseAmount, applications);
        var alertState = GetDonationAlertState(donation.StatusCatalogEntry!, metrics.AppliedAmountTotal, donation.BaseAmount);

        return new DonationDetailResponse(
            donation.Id,
            donation.DonorEntityName,
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
            alertState,
            donation.CreatedUtc,
            donation.UpdatedUtc,
            applications.Select(item => MapDonationApplicationResponse(item, evidenceLookup.GetValueOrDefault(item.Id) ?? [])).ToList());
    }

    private static async Task<List<DonationAlertResponse>> BuildDonationAlertsAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var donations = await dbContext.Donations
            .AsNoTracking()
            .Include(item => item.StatusCatalogEntry)
            .OrderByDescending(item => item.DonationDate)
            .ThenBy(item => item.DonorEntityName)
            .ToListAsync(cancellationToken);

        var donationIds = donations.Select(item => item.Id).ToArray();
        var applications = donationIds.Length == 0
            ? []
            : await dbContext.DonationApplications
                .AsNoTracking()
                .Where(item => donationIds.Contains(item.DonationId))
                .ToListAsync(cancellationToken);

        return donations
            .Select(donation =>
            {
                var donationApplications = applications.Where(item => item.DonationId == donation.Id).ToList();
                var metrics = CalculateProgress(donation.BaseAmount, donationApplications);
                var alertState = GetDonationAlertState(donation.StatusCatalogEntry!, metrics.AppliedAmountTotal, donation.BaseAmount);

                return new DonationAlertResponse(
                    donation.Id,
                    donation.DonorEntityName,
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

    private static DonationApplicationResponse MapDonationApplicationResponse(
        DonationApplication application,
        IReadOnlyList<DonationApplicationEvidence> evidences)
    {
        return new DonationApplicationResponse(
            application.Id,
            application.DonationId,
            application.BeneficiaryName,
            application.ResponsibleContactId,
            application.ResponsibleName,
            application.ApplicationDate,
            application.AppliedAmount,
            application.StatusCatalogEntryId,
            application.StatusCatalogEntry!.StatusCode,
            application.StatusCatalogEntry.StatusName,
            application.StatusCatalogEntry.IsClosed,
            application.VerificationDetails,
            application.ClosingDetails,
            evidences.Count,
            evidences.Select(MapDonationApplicationEvidenceResponse).ToList(),
            application.CreatedUtc);
    }

    private static DonationApplicationEvidenceResponse MapDonationApplicationEvidenceResponse(DonationApplicationEvidence evidence)
    {
        return new DonationApplicationEvidenceResponse(
            evidence.Id,
            evidence.DonationApplicationId,
            evidence.EvidenceTypeId,
            evidence.EvidenceType!.Code,
            evidence.EvidenceType.Name,
            evidence.Description,
            evidence.OriginalFileName,
            evidence.ContentType,
            evidence.FileSizeBytes,
            evidence.UploadedUtc);
    }

    private static async Task<Dictionary<Guid, int>> BuildEvidenceCountLookupAsync(
        PlatformDbContext dbContext,
        IReadOnlyList<DonationApplication> applications,
        CancellationToken cancellationToken)
    {
        if (applications.Count == 0)
        {
            return [];
        }

        var applicationIds = applications.Select(item => item.Id).ToArray();

        return await dbContext.DonationApplicationEvidences
            .AsNoTracking()
            .Where(item => applicationIds.Contains(item.DonationApplicationId))
            .GroupBy(item => item.DonationApplicationId)
            .Select(grouping => new { DonationApplicationId = grouping.Key, Count = grouping.Count() })
            .ToDictionaryAsync(item => item.DonationApplicationId, item => item.Count, cancellationToken);
    }

    private static async Task<Dictionary<Guid, List<DonationApplicationEvidence>>> BuildEvidenceLookupAsync(
        PlatformDbContext dbContext,
        IReadOnlyList<DonationApplication> applications,
        CancellationToken cancellationToken)
    {
        if (applications.Count == 0)
        {
            return [];
        }

        var applicationIds = applications.Select(item => item.Id).ToArray();
        var evidences = await dbContext.DonationApplicationEvidences
            .AsNoTracking()
            .Where(item => applicationIds.Contains(item.DonationApplicationId))
            .Include(item => item.EvidenceType)
            .OrderByDescending(item => item.UploadedUtc)
            .ToListAsync(cancellationToken);

        return evidences
            .GroupBy(item => item.DonationApplicationId)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.ToList());
    }

    private static DonationProgressMetrics CalculateProgress(decimal baseAmount, IReadOnlyList<DonationApplication> applications)
    {
        var appliedAmountTotal = applications.Sum(item => item.AppliedAmount);
        var remainingAmount = decimal.Max(0, baseAmount - appliedAmountTotal);
        var appliedPercentage = baseAmount <= 0
            ? 0
            : decimal.Round((appliedAmountTotal / baseAmount) * 100m, 2, MidpointRounding.AwayFromZero);

        return new DonationProgressMetrics(appliedAmountTotal, remainingAmount, appliedPercentage);
    }

    private static string GetDonationAlertState(
        ModuleStatusCatalogEntry donationStatus,
        decimal appliedAmountTotal,
        decimal baseAmount)
    {
        if (donationStatus.IsClosed || !donationStatus.AlertsEnabledByDefault)
        {
            return NoAlertState;
        }

        if (appliedAmountTotal <= 0)
        {
            return NotAppliedStatusCode;
        }

        if (appliedAmountTotal < baseAmount)
        {
            return PartiallyAppliedStatusCode;
        }

        return NoAlertState;
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
                    && item.ModuleCode == DonationsModuleCode
                    && item.ContextCode == contextCode,
                cancellationToken);
    }

    private static async Task<int> ResolveCalculatedDonationStatusIdAsync(
        decimal appliedAmountTotal,
        decimal baseAmount,
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var calculatedStatusCode = appliedAmountTotal switch
        {
            <= 0 => NotAppliedStatusCode,
            _ when appliedAmountTotal < baseAmount => PartiallyAppliedStatusCode,
            _ => AppliedStatusCode
        };

        var calculatedStatus = await dbContext.ModuleStatusCatalogEntries
            .AsNoTracking()
            .SingleAsync(
                item => item.ModuleCode == DonationsModuleCode
                    && item.ContextCode == DonationContextCode
                    && item.StatusCode == calculatedStatusCode,
                cancellationToken);

        return calculatedStatus.Id;
    }

    private static Dictionary<string, string[]> ValidateCreateDonationRequest(CreateDonationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.DonorEntityName))
        {
            errors["donorEntityName"] = ["DonorEntityName is required."];
        }

        if (request.DonationDate == default)
        {
            errors["donationDate"] = ["DonationDate is required."];
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

    private static Dictionary<string, string[]> ValidateCreateDonationApplicationRequest(CreateDonationApplicationRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.BeneficiaryName))
        {
            errors["beneficiaryName"] = ["BeneficiaryName is required."];
        }

        if (string.IsNullOrWhiteSpace(request.ResponsibleName))
        {
            errors["responsibleName"] = ["ResponsibleName is required."];
        }

        if (request.ApplicationDate == default)
        {
            errors["applicationDate"] = ["ApplicationDate is required."];
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

    private static Dictionary<string, string[]> ValidateCreateDonationApplicationEvidenceRequest(CreateDonationApplicationEvidenceRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.EvidenceTypeId <= 0)
        {
            errors["evidenceTypeId"] = ["EvidenceTypeId is required."];
        }

        if (request.File is null || request.File.Length <= 0)
        {
            errors["file"] = ["A donation application evidence file is required."];
        }

        return errors;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed record DonationProgressMetrics(
        decimal AppliedAmountTotal,
        decimal RemainingAmount,
        decimal AppliedPercentage);
}
