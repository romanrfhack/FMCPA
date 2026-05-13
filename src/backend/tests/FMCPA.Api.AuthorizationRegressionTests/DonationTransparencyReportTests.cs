using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Domain.Entities.Donations;
using FMCPA.Domain.Entities.Documents;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FMCPA.Api.AuthorizationRegressionTests;

public sealed class DonationTransparencyReportTests : IClassFixture<AuthorizationRegressionWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AuthorizationRegressionWebApplicationFactory _factory;

    public DonationTransparencyReportTests(AuthorizationRegressionWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Transparency_report_returns_not_ready_for_donation_without_applications()
    {
        var donation = await SeedDonationAsync([], baseAmount: 1000);
        var token = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var response = await SendGetAsync($"/api/donations/{donation.DonationId}/transparency-report", token);
        var body = await response.Content.ReadFromJsonAsync<DonationTransparencyReportTestResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(donation.DonationId, body!.DonationId);
        Assert.Equal("NOT_READY", body.PresentationReadiness.ReadinessCode);
        Assert.Equal(0, body.FinancialSummary.ApplicationCount);
        Assert.Equal("NO_APPLICATIONS", body.DocumentarySummary.DocumentaryStatusCode);
        Assert.Empty(body.Applications);
        Assert.Contains("No hay aplicaciones registradas.", body.PresentationReadiness.Reasons);
        Assert.Contains(
            "La evidencia mínima registrada acredita presencia documental en el sistema. No sustituye revisión legal, fiscal o contable.",
            body.ScopeNotes);
    }

    [Fact]
    public async Task Transparency_report_returns_partial_for_pending_balance_and_missing_evidence()
    {
        var donation = await SeedDonationAsync(
        [
            new ApplicationSeedDefinition("Beneficiario pendiente", 500, HasEvidence: false, HasActiveDocument: false)
        ],
        baseAmount: 1000);
        var token = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var response = await SendGetAsync($"/api/donations/{donation.DonationId}/transparency-report", token);
        var body = await response.Content.ReadFromJsonAsync<DonationTransparencyReportTestResponse>();
        var application = Assert.Single(body!.Applications);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("PARTIAL", body.PresentationReadiness.ReadinessCode);
        Assert.Equal(500, body.FinancialSummary.AppliedAmountTotal);
        Assert.Equal(500, body.FinancialSummary.RemainingAmount);
        Assert.Equal("EVIDENCE_PENDING", body.DocumentarySummary.DocumentaryStatusCode);
        Assert.Equal(1, body.DocumentarySummary.ApplicationsMissingEvidence);
        Assert.Contains("Aún existe recurso pendiente de aplicar.", body.PresentationReadiness.Reasons);
        Assert.Contains("Existen aplicaciones con evidencia pendiente.", body.PresentationReadiness.Reasons);
        Assert.Equal(0, application.EvidenceCount);
        Assert.Equal(0, application.ActiveDocumentCount);
        Assert.Equal("EVIDENCE_PENDING", application.RequirementStatus);
        Assert.Equal("MISSING_EVIDENCE", application.MissingReasonCode);
        Assert.Empty(application.Evidences);
    }

    [Fact]
    public async Task Transparency_report_returns_ready_for_fully_applied_donation_with_complete_evidence()
    {
        var donation = await SeedDonationAsync(
        [
            new ApplicationSeedDefinition("Beneficiario completo", 1000, HasEvidence: true, HasActiveDocument: true)
        ],
        baseAmount: 1000);
        var token = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var response = await SendGetAsync($"/api/donations/{donation.DonationId}/transparency-report", token);
        var body = await response.Content.ReadFromJsonAsync<DonationTransparencyReportTestResponse>();
        var application = Assert.Single(body!.Applications);
        var evidence = Assert.Single(application.Evidences);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("READY", body.PresentationReadiness.ReadinessCode);
        Assert.Equal(1000, body.FinancialSummary.BaseAmount);
        Assert.Equal(1000, body.FinancialSummary.AppliedAmountTotal);
        Assert.Equal(0, body.FinancialSummary.RemainingAmount);
        Assert.Equal(100, body.FinancialSummary.AppliedPercentage);
        Assert.Equal("MINIMUM_EVIDENCE_COMPLETE", body.DocumentarySummary.DocumentaryStatusCode);
        Assert.True(body.DocumentarySummary.IsMinimumEvidenceComplete);
        Assert.Equal("MINIMUM_EVIDENCE_REGISTERED", application.RequirementStatus);
        Assert.Equal(1, application.EvidenceCount);
        Assert.Equal(1, application.ActiveDocumentCount);
        Assert.Equal(100, application.PercentageOfDonation);
        Assert.StartsWith("/api/donations/applications/evidences/", evidence.DownloadUrl);
        Assert.EndsWith("/download", evidence.DownloadUrl);
    }

    [Fact]
    public async Task Transparency_report_does_not_expose_physical_or_relative_storage_paths()
    {
        var donation = await SeedDonationAsync(
        [
            new ApplicationSeedDefinition("Beneficiario con ruta interna", 1000, HasEvidence: true, HasActiveDocument: true)
        ],
        baseAmount: 1000);
        var token = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var response = await SendGetAsync($"/api/donations/{donation.DonationId}/transparency-report", token);
        var rawBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("storedRelativePath", rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("donation-transparency-report/", rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Path.GetTempPath(), rawBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Transparency_report_rejects_anonymous_access()
    {
        var donation = await SeedDonationAsync([], baseAmount: 1000);

        using var response = await SendGetAsync($"/api/donations/{donation.DonationId}/transparency-report");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<DonationSeedResult> SeedDonationAsync(
        IReadOnlyList<ApplicationSeedDefinition> applications,
        decimal baseAmount)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var donationStatus = new ModuleStatusCatalogEntry(
            "DONATARIAS",
            "Donatarias",
            "DONATION",
            "Donacion",
            "NOT_APPLIED",
            "No aplicada",
            description: null,
            sortOrder: 1,
            isClosed: false,
            alertsEnabledByDefault: true);
        var applicationStatus = new ModuleStatusCatalogEntry(
            "DONATARIAS",
            "Donatarias",
            "DONATION_APPLICATION",
            "Aplicacion de donacion",
            "PARTIALLY_APPLIED",
            "Aplicacion parcial",
            description: null,
            sortOrder: 1,
            isClosed: false,
            alertsEnabledByDefault: true);
        var evidenceType = new EvidenceType($"TRR{suffix}", "Evidencia transparencia");
        dbContext.ModuleStatusCatalogEntries.AddRange(donationStatus, applicationStatus);
        dbContext.EvidenceTypes.Add(evidenceType);
        await dbContext.SaveChangesAsync();

        var donation = new Donation(
            $"Donante reporte {suffix}",
            today,
            "Efectivo",
            baseAmount,
            $"DON-RPT-{suffix}",
            "Donacion de prueba para reporte de transparencia.",
            donationStatus.Id);
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        var seededApplications = new List<Guid>();
        for (var index = 0; index < applications.Count; index++)
        {
            var definition = applications[index];
            var application = new DonationApplication(
                donation.Id,
                definition.BeneficiaryName,
                responsibleContactId: null,
                "Responsable del reporte",
                today.AddDays(index),
                definition.AppliedAmount,
                applicationStatus.Id,
                verificationDetails: "Aplicacion validada operativamente para prueba.",
                closingDetails: null);
            dbContext.DonationApplications.Add(application);
            await dbContext.SaveChangesAsync();
            seededApplications.Add(application.Id);

            if (!definition.HasEvidence)
            {
                continue;
            }

            var evidence = new DonationApplicationEvidence(
                application.Id,
                evidenceType.Id,
                "Evidencia de prueba para reporte",
                $"evidencia-reporte-{suffix}-{index}.pdf",
                $"donation-transparency-report/{Guid.NewGuid():N}.pdf",
                "application/pdf",
                42,
                DateTimeOffset.UtcNow);
            dbContext.DonationApplicationEvidences.Add(evidence);
            await dbContext.SaveChangesAsync();

            if (definition.HasActiveDocument)
            {
                dbContext.StoredDocuments.Add(CreateStoredDocument(evidence));
            }

            await dbContext.SaveChangesAsync();
        }

        return new DonationSeedResult(donation.Id, seededApplications);
    }

    private static StoredDocument CreateStoredDocument(DonationApplicationEvidence evidence)
    {
        var fileContent = Encoding.UTF8.GetBytes("transparency report regression");

        return new StoredDocument(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            evidence.Id,
            evidence.OriginalFileName,
            evidence.StoredRelativePath,
            evidence.ContentType ?? "application/pdf",
            evidence.FileSizeBytes,
            evidence.UploadedUtc,
            Convert.ToHexString(SHA256.HashData(fileContent)),
            isLegacyBackfill: false,
            documentClassCode: DocumentClassCodes.SupportingDocument,
            businessPurpose: "Acreditar evidencia minima de aplicacion de donacion.");
    }

    private async Task<string> LoginAsync(string userName, string password)
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                userName,
                password
            });
        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginTestResponse>();
        return login!.AccessToken;
    }

    private Task<HttpResponseMessage> SendGetAsync(string path, string? accessToken = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return _client.SendAsync(request);
    }

    private sealed record ApplicationSeedDefinition(
        string BeneficiaryName,
        decimal AppliedAmount,
        bool HasEvidence,
        bool HasActiveDocument);

    private sealed record DonationSeedResult(Guid DonationId, IReadOnlyList<Guid> ApplicationIds);

    private sealed record LoginTestResponse(string AccessToken);

    private sealed record DonationTransparencyReportTestResponse(
        Guid DonationId,
        string DonorEntityName,
        DateOnly DonationDate,
        string DonationType,
        string Reference,
        string? Notes,
        DateTimeOffset ReportGeneratedUtc,
        DonationTransparencyFinancialSummaryTestResponse FinancialSummary,
        DonationTransparencyOperationalStatusTestResponse OperationalStatus,
        DonationTransparencyDocumentarySummaryTestResponse DocumentarySummary,
        IReadOnlyList<DonationTransparencyApplicationTestResponse> Applications,
        DonationTransparencyReadinessTestResponse PresentationReadiness,
        IReadOnlyList<string> ScopeNotes);

    private sealed record DonationTransparencyFinancialSummaryTestResponse(
        decimal BaseAmount,
        decimal AppliedAmountTotal,
        decimal RemainingAmount,
        decimal AppliedPercentage,
        int ApplicationCount);

    private sealed record DonationTransparencyOperationalStatusTestResponse(
        string DonationStatusCode,
        string DonationStatusName,
        bool StatusIsClosed,
        string FinancialStatusLabel,
        string OperationalStatusLabel);

    private sealed record DonationTransparencyDocumentarySummaryTestResponse(
        string DocumentaryStatusCode,
        string DocumentaryStatusLabel,
        int TotalApplications,
        int ApplicationsWithEvidence,
        int ApplicationsMissingEvidence,
        bool IsMinimumEvidenceComplete);

    private sealed record DonationTransparencyApplicationTestResponse(
        Guid ApplicationId,
        string BeneficiaryName,
        DateOnly ApplicationDate,
        string ResponsibleName,
        decimal AppliedAmount,
        decimal PercentageOfDonation,
        string StatusCode,
        string StatusName,
        string? VerificationDetails,
        string? ClosingDetails,
        int EvidenceCount,
        int ActiveDocumentCount,
        string RequirementStatus,
        string? MissingReasonCode,
        IReadOnlyList<DonationTransparencyEvidenceTestResponse> Evidences);

    private sealed record DonationTransparencyEvidenceTestResponse(
        Guid EvidenceId,
        string EvidenceTypeName,
        string OriginalFileName,
        string? Description,
        DateTimeOffset UploadedUtc,
        string DownloadUrl);

    private sealed record DonationTransparencyReadinessTestResponse(
        string ReadinessCode,
        string ReadinessLabel,
        IReadOnlyList<string> Reasons);
}
