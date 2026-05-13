using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Domain.Entities.Donations;
using FMCPA.Domain.Entities.Documents;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FMCPA.Api.AuthorizationRegressionTests;

public sealed class DonationDocumentaryStatusTests : IClassFixture<AuthorizationRegressionWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly AuthorizationRegressionWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public DonationDocumentaryStatusTests(AuthorizationRegressionWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Documentary_status_returns_no_applications_for_empty_donation()
    {
        var donation = await SeedDonationAsync([]);
        var token = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var response = await SendGetAsync($"/api/donations/{donation.DonationId}/documentary-status", token);
        var body = await response.Content.ReadFromJsonAsync<DonationDocumentaryStatusTestResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(donation.DonationId, body!.DonationId);
        Assert.Equal(0, body.TotalApplications);
        Assert.Equal("NO_APPLICATIONS", body.DocumentaryStatusCode);
        Assert.False(body.IsMinimumEvidenceComplete);
        Assert.Empty(body.ApplicationStatuses);
    }

    [Fact]
    public async Task Documentary_status_returns_pending_when_application_has_no_evidence()
    {
        var donation = await SeedDonationAsync(
        [
            new ApplicationSeedDefinition("Beneficiario sin evidencia", 500, HasEvidence: false, HasActiveDocument: false)
        ]);
        var token = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var response = await SendGetAsync($"/api/donations/{donation.DonationId}/documentary-status", token);
        var body = await response.Content.ReadFromJsonAsync<DonationDocumentaryStatusTestResponse>();
        var application = Assert.Single(body!.ApplicationStatuses);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("EVIDENCE_PENDING", body.DocumentaryStatusCode);
        Assert.False(body.IsMinimumEvidenceComplete);
        Assert.Equal(0, body.ApplicationsWithEvidence);
        Assert.Equal(1, body.ApplicationsMissingEvidence);
        Assert.Equal(0, application.EvidenceCount);
        Assert.Equal(0, application.ActiveDocumentCount);
        Assert.Equal("EVIDENCE_PENDING", application.RequirementStatus);
        Assert.Equal("MISSING_EVIDENCE", application.MissingReasonCode);
    }

    [Fact]
    public async Task Documentary_status_uses_active_stored_documents_instead_of_only_evidence_rows()
    {
        var donation = await SeedDonationAsync(
        [
            new ApplicationSeedDefinition(
                "Beneficiario con evidencia archivada",
                500,
                HasEvidence: true,
                HasActiveDocument: false,
                HasArchivedDocument: true)
        ]);
        var token = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var response = await SendGetAsync($"/api/donations/{donation.DonationId}/documentary-status", token);
        var body = await response.Content.ReadFromJsonAsync<DonationDocumentaryStatusTestResponse>();
        var application = Assert.Single(body!.ApplicationStatuses);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("EVIDENCE_PENDING", body.DocumentaryStatusCode);
        Assert.Equal(1, application.EvidenceCount);
        Assert.Equal(0, application.ActiveDocumentCount);
        Assert.Equal("EVIDENCE_PENDING", application.RequirementStatus);
    }

    [Fact]
    public async Task Documentary_status_returns_complete_when_all_applications_have_active_evidence()
    {
        var donation = await SeedDonationAsync(
        [
            new ApplicationSeedDefinition("Beneficiario uno", 400, HasEvidence: true, HasActiveDocument: true),
            new ApplicationSeedDefinition("Beneficiario dos", 600, HasEvidence: true, HasActiveDocument: true)
        ]);
        var token = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var response = await SendGetAsync($"/api/donations/{donation.DonationId}/documentary-status", token);
        var rawBody = await response.Content.ReadAsStringAsync();
        var body = JsonSerializer.Deserialize<DonationDocumentaryStatusTestResponse>(rawBody, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("MINIMUM_EVIDENCE_COMPLETE", body!.DocumentaryStatusCode);
        Assert.True(body.IsMinimumEvidenceComplete);
        Assert.Equal(2, body.ApplicationsWithEvidence);
        Assert.Equal(0, body.ApplicationsMissingEvidence);
        Assert.All(body.ApplicationStatuses, application =>
        {
            Assert.Equal(1, application.EvidenceCount);
            Assert.Equal(1, application.ActiveDocumentCount);
            Assert.Equal("MINIMUM_EVIDENCE_REGISTERED", application.RequirementStatus);
            Assert.Null(application.MissingReasonCode);
            Assert.Contains("SUPPORTING_DOCUMENT", application.RequiredDocumentClassCodes);
            Assert.StartsWith($"/donatarias?donationId={donation.DonationId}&applicationId=", application.RouteHint);
        });
        Assert.DoesNotContain("storedRelativePath", rawBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("donation-documentary-status/", rawBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Documentary_status_rejects_anonymous_access()
    {
        var donation = await SeedDonationAsync([]);

        using var response = await SendGetAsync($"/api/donations/{donation.DonationId}/documentary-status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<DonationSeedResult> SeedDonationAsync(IReadOnlyList<ApplicationSeedDefinition> applications)
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
        var evidenceType = new EvidenceType($"TR{suffix}", "Evidencia transparencia");
        dbContext.ModuleStatusCatalogEntries.AddRange(donationStatus, applicationStatus);
        dbContext.EvidenceTypes.Add(evidenceType);
        await dbContext.SaveChangesAsync();

        var donation = new Donation(
            $"Donante transparencia {suffix}",
            today,
            "Efectivo",
            applications.Sum(item => item.AppliedAmount) + 100,
            $"DON-TR-{suffix}",
            "Donacion de prueba para semaforo documental.",
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
                "Responsable documental",
                today.AddDays(index),
                definition.AppliedAmount,
                applicationStatus.Id,
                verificationDetails: null,
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
                "Evidencia de prueba",
                $"evidencia-{suffix}-{index}.pdf",
                $"donation-documentary-status/{Guid.NewGuid():N}.pdf",
                "application/pdf",
                42,
                DateTimeOffset.UtcNow);
            dbContext.DonationApplicationEvidences.Add(evidence);
            await dbContext.SaveChangesAsync();

            if (definition.HasActiveDocument)
            {
                dbContext.StoredDocuments.Add(CreateStoredDocument(evidence));
            }

            if (definition.HasArchivedDocument)
            {
                var archivedDocument = CreateStoredDocument(evidence);
                archivedDocument.Archive("Archived regression evidence.", DateTimeOffset.UtcNow);
                dbContext.StoredDocuments.Add(archivedDocument);
            }

            await dbContext.SaveChangesAsync();
        }

        return new DonationSeedResult(donation.Id, seededApplications);
    }

    private static StoredDocument CreateStoredDocument(DonationApplicationEvidence evidence)
    {
        var fileContent = Encoding.UTF8.GetBytes("documentary status regression");

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
        bool HasActiveDocument,
        bool HasArchivedDocument = false);

    private sealed record DonationSeedResult(Guid DonationId, IReadOnlyList<Guid> ApplicationIds);

    private sealed record LoginTestResponse(string AccessToken);

    private sealed record DonationDocumentaryStatusTestResponse(
        Guid DonationId,
        int TotalApplications,
        int ApplicationsWithEvidence,
        int ApplicationsMissingEvidence,
        string DocumentaryStatusCode,
        string DocumentaryStatusLabel,
        bool IsMinimumEvidenceComplete,
        IReadOnlyList<DonationApplicationDocumentaryStatusTestResponse> ApplicationStatuses);

    private sealed record DonationApplicationDocumentaryStatusTestResponse(
        Guid ApplicationId,
        string BeneficiaryName,
        DateOnly ApplicationDate,
        decimal AppliedAmount,
        int EvidenceCount,
        int ActiveDocumentCount,
        string RequirementStatus,
        string? MissingReasonCode,
        IReadOnlyList<string> RequiredDocumentClassCodes,
        string? RouteHint);
}
