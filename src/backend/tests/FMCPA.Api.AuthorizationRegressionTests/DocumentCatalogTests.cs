using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Domain.Entities.Donations;
using FMCPA.Domain.Entities.Documents;
using FMCPA.Domain.Entities.Federation;
using FMCPA.Domain.Entities.Markets;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FMCPA.Api.AuthorizationRegressionTests;

public sealed class DocumentCatalogTests : IClassFixture<AuthorizationRegressionWebApplicationFactory>
{
    private readonly AuthorizationRegressionWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DocumentCatalogTests(AuthorizationRegressionWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Document_catalog_lists_details_and_downloads_allowed_documents_without_exposing_storage_paths()
    {
        var documentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "cedula-transversal.pdf",
            "application/pdf",
            "document catalog regression",
            documentClassCode: DocumentClassCodes.Certificate,
            businessPurpose: "Acreditar la cedula digitalizada del locatario.",
            isPrimaryDocument: true);
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var list = await SendGetAsync($"/api/documents?moduleCode=MARKETS&take=20", token);
        var listBody = await list.Content.ReadAsStringAsync();
        var listResponse = await list.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        var listedDocument = Assert.Single(listResponse!.Items, item => item.Id == documentId);

        using var detail = await SendGetAsync($"/api/documents/{documentId}", token);
        var detailBody = await detail.Content.ReadAsStringAsync();
        var detailResponse = await detail.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();

        using var download = await SendGetAsync($"/api/documents/{documentId}/download", token);
        var downloadedContent = await download.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal("ACTIVE", listedDocument.StatusCode);
        Assert.Equal(DocumentClassCodes.Certificate, listedDocument.DocumentClassCode);
        Assert.Equal(DocumentRetentionPolicyCodes.CertificateReview, listedDocument.RetentionPolicyCode);
        Assert.Equal(DocumentRetentionPolicyCodes.ActiveRetention, listedDocument.RetentionStatusCode);
        Assert.Equal(DocumentOperationalStatusCodes.ActiveOk, listedDocument.DocumentOperationalStatusCode);
        Assert.Equal(DocumentOperationalStatusCodes.SeverityNone, listedDocument.DocumentOperationalSeverityCode);
        Assert.True(listedDocument.IsPrimaryDocument);
        Assert.Equal("VALID", listedDocument.IntegrityState);
        Assert.Equal($"/api/documents/{documentId}/download", listedDocument.DownloadUrl);
        Assert.DoesNotContain("storedRelativePath", listBody, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(documentId, detailResponse!.Id);
        Assert.Equal("ACTIVE", detailResponse.StatusCode);
        Assert.Equal(DocumentClassCodes.Certificate, detailResponse.DocumentClassCode);
        Assert.Equal(DocumentRetentionPolicyCodes.CertificateReview, detailResponse.RetentionPolicyCode);
        Assert.Equal(DocumentRetentionPolicyCodes.ActiveRetention, detailResponse.RetentionStatusCode);
        Assert.Equal(DocumentOperationalStatusCodes.ActiveOk, detailResponse.DocumentOperationalStatusCode);
        Assert.Equal(DocumentOperationalStatusCodes.SeverityNone, detailResponse.DocumentOperationalSeverityCode);
        Assert.Equal("Acreditar la cedula digitalizada del locatario.", detailResponse.BusinessPurpose);
        Assert.True(detailResponse.IsPrimaryDocument);
        Assert.Equal("VALID", detailResponse.IntegrityState);
        Assert.DoesNotContain("storedRelativePath", detailBody, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("document catalog regression", downloadedContent);
        Assert.Equal("nosniff", download.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Contains("no-store", download.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("cedula-transversal.pdf", download.Content.Headers.ContentDisposition?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Document_catalog_includes_origin_context_and_lists_documents_by_entity()
    {
        var marketTenant = await SeedMarketTenantAsync();
        var marketDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "cedula-contextual.pdf",
            "application/pdf",
            "market tenant context regression",
            entityId: marketTenant.TenantId,
            documentClassCode: DocumentClassCodes.Certificate,
            isPrimaryDocument: true);
        var donationEvidence = await SeedDonationApplicationEvidenceAsync();
        var donationDocumentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "evidencia-donacion-contextual.pdf",
            "application/pdf",
            "donation application context regression",
            entityId: donationEvidence.EvidenceId,
            documentClassCode: DocumentClassCodes.SupportingDocument);
        var federationEvidence = await SeedFederationApplicationEvidenceAsync();
        var federationDocumentId = await SeedStoredDocumentAsync(
            "FEDERATION",
            DocumentAreaCodes.FederationApplicationEvidences,
            "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            "evidencia-federacion-contextual.pdf",
            "application/pdf",
            "federation application context regression",
            entityId: federationEvidence.EvidenceId,
            documentClassCode: DocumentClassCodes.SupportingDocument);
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var list = await SendGetAsync("/api/documents?includeArchived=true&take=200", token);
        var listBody = await list.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var marketByEntity = await SendGetAsync($"/api/documents/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={marketTenant.TenantId}", token);
        var marketBody = await marketByEntity.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var donationByEntity = await SendGetAsync($"/api/documents/by-entity?moduleCode=DONATARIAS&entityType=DONATION_APPLICATION&entityId={donationEvidence.ApplicationId}", token);
        var donationBody = await donationByEntity.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var federationByEntity = await SendGetAsync($"/api/documents/by-entity?moduleCode=FEDERATION&entityType=FEDERATION_DONATION_APPLICATION&entityId={federationEvidence.ApplicationId}", token);
        var federationBody = await federationByEntity.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains(listBody!.Items, item =>
            item.Id == marketDocumentId
            && item.OriginContext.EntityType == "MARKET_TENANT"
            && item.OriginContext.EntityId == marketTenant.TenantId
            && item.OriginContext.DisplayName == "Locatario contextual"
            && item.OriginContext.RouteHint == $"/markets?marketId={marketTenant.MarketId}&tenantId={marketTenant.TenantId}");
        Assert.Contains(listBody.Items, item =>
            item.Id == donationDocumentId
            && item.OriginContext.EntityType == "DONATION_APPLICATION"
            && item.OriginContext.EntityId == donationEvidence.ApplicationId
            && item.OriginContext.DisplayName == "Beneficiario contextual");
        Assert.Contains(listBody.Items, item =>
            item.Id == federationDocumentId
            && item.OriginContext.EntityType == "FEDERATION_DONATION_APPLICATION"
            && item.OriginContext.EntityId == federationEvidence.ApplicationId
            && item.OriginContext.DisplayName == "Destino contextual");

        Assert.Equal(HttpStatusCode.OK, marketByEntity.StatusCode);
        Assert.Contains(marketBody!.Items, item => item.Id == marketDocumentId);
        Assert.Equal(HttpStatusCode.OK, donationByEntity.StatusCode);
        Assert.Contains(donationBody!.Items, item => item.Id == donationDocumentId);
        Assert.Equal(HttpStatusCode.OK, federationByEntity.StatusCode);
        Assert.Contains(federationBody!.Items, item => item.Id == federationDocumentId);
    }

    [Fact]
    public async Task Document_completeness_by_entity_and_pending_list_cover_key_entities()
    {
        var completeMarketTenant = await SeedMarketTenantAsync();
        var completeMarketDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "cedula-completa.pdf",
            "application/pdf",
            "complete market tenant regression",
            entityId: completeMarketTenant.TenantId,
            documentClassCode: DocumentClassCodes.Certificate,
            isPrimaryDocument: true);
        var incompleteMarketTenant = await SeedMarketTenantAsync();
        var completeDonation = await SeedDonationApplicationEvidenceAsync();
        var completeDonationDocumentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "evidencia-donacion-completa.pdf",
            "application/pdf",
            "complete donation application regression",
            entityId: completeDonation.EvidenceId,
            documentClassCode: DocumentClassCodes.SupportingDocument);
        var incompleteDonation = await SeedDonationApplicationEvidenceAsync();
        var completeFederation = await SeedFederationApplicationEvidenceAsync();
        var completeFederationDocumentId = await SeedStoredDocumentAsync(
            "FEDERATION",
            DocumentAreaCodes.FederationApplicationEvidences,
            "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            "evidencia-federacion-completa.pdf",
            "application/pdf",
            "complete federation application regression",
            entityId: completeFederation.EvidenceId,
            documentClassCode: DocumentClassCodes.SupportingDocument);
        var incompleteFederation = await SeedFederationApplicationEvidenceAsync();
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var rules = await SendGetAsync("/api/documents/rules", token);
        var rulesBody = await rules.Content.ReadFromJsonAsync<DocumentRuleListTestResponse>();
        using var marketRequirement = await SendGetAsync($"/api/documents/requirements/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={completeMarketTenant.TenantId}", token);
        var marketRequirementBody = await marketRequirement.Content.ReadFromJsonAsync<DocumentRequirementTestResponse>();
        using var donationRequirement = await SendGetAsync($"/api/documents/requirements/by-entity?moduleCode=DONATARIAS&entityType=DONATION_APPLICATION&entityId={incompleteDonation.ApplicationId}", token);
        var donationRequirementBody = await donationRequirement.Content.ReadFromJsonAsync<DocumentRequirementTestResponse>();
        using var federationRequirement = await SendGetAsync($"/api/documents/requirements/by-entity?moduleCode=FEDERATION&entityType=FEDERATION_DONATION_APPLICATION&entityId={completeFederation.ApplicationId}", token);
        var federationRequirementBody = await federationRequirement.Content.ReadFromJsonAsync<DocumentRequirementTestResponse>();
        using var marketComplete = await SendGetAsync($"/api/documents/completeness/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={completeMarketTenant.TenantId}", token);
        var marketCompleteBody = await marketComplete.Content.ReadFromJsonAsync<DocumentCompletenessTestResponse>();
        using var marketIncomplete = await SendGetAsync($"/api/documents/completeness/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={incompleteMarketTenant.TenantId}", token);
        var marketIncompleteBody = await marketIncomplete.Content.ReadFromJsonAsync<DocumentCompletenessTestResponse>();
        using var donationComplete = await SendGetAsync($"/api/documents/completeness/by-entity?moduleCode=DONATARIAS&entityType=DONATION_APPLICATION&entityId={completeDonation.ApplicationId}", token);
        var donationCompleteBody = await donationComplete.Content.ReadFromJsonAsync<DocumentCompletenessTestResponse>();
        using var federationComplete = await SendGetAsync($"/api/documents/completeness/by-entity?moduleCode=FEDERATION&entityType=FEDERATION_DONATION_APPLICATION&entityId={completeFederation.ApplicationId}", token);
        var federationCompleteBody = await federationComplete.Content.ReadFromJsonAsync<DocumentCompletenessTestResponse>();
        using var pendingMarkets = await SendGetAsync("/api/documents/pending?moduleCode=MARKETS&take=200", token);
        var pendingMarketsBody = await pendingMarkets.Content.ReadFromJsonAsync<DocumentCompletenessListTestResponse>();
        using var pendingAll = await SendGetAsync("/api/documents/pending?take=200", token);
        var pendingAllBody = await pendingAll.Content.ReadFromJsonAsync<DocumentCompletenessListTestResponse>();

        Assert.Equal(HttpStatusCode.OK, rules.StatusCode);
        Assert.Contains(rulesBody!.Items, rule =>
            rule.RuleCode == "MARKET_TENANT_CERTIFICATE_REQUIRED"
            && rule.ModuleCode == "MARKETS"
            && rule.EntityType == "MARKET_TENANT"
            && rule.RequiredDocumentClassCodes.Contains(DocumentClassCodes.Certificate)
            && rule.MinimumRequiredCount == 1);
        Assert.Contains(rulesBody.Items, rule =>
            rule.RuleCode == "DONATION_APPLICATION_EVIDENCE_REQUIRED"
            && rule.ModuleCode == "DONATARIAS"
            && rule.EntityType == "DONATION_APPLICATION"
            && rule.RequiredDocumentClassCodes.Contains(DocumentClassCodes.SupportingDocument)
            && rule.MinimumRequiredCount == 1);
        Assert.Contains(rulesBody.Items, rule =>
            rule.RuleCode == "FEDERATION_DONATION_APPLICATION_EVIDENCE_REQUIRED"
            && rule.ModuleCode == "FEDERATION"
            && rule.EntityType == "FEDERATION_DONATION_APPLICATION"
            && rule.RequiredDocumentClassCodes.Contains(DocumentClassCodes.SupportingDocument)
            && rule.MinimumRequiredCount == 1);

        Assert.Equal(HttpStatusCode.OK, marketRequirement.StatusCode);
        Assert.True(marketRequirementBody!.AppliesRule);
        Assert.True(marketRequirementBody.IsComplete);
        Assert.Equal("COMPLETE", marketRequirementBody.StatusCode);
        Assert.Equal("MARKET_TENANT_CERTIFICATE_REQUIRED", marketRequirementBody.RuleCode);
        Assert.Equal(1, marketRequirementBody.CurrentDocumentCount);
        Assert.Equal(1, marketRequirementBody.MinimumRequiredCount);
        Assert.Contains(completeMarketDocumentId, marketRequirementBody.CoveringDocumentIds);

        Assert.Equal(HttpStatusCode.OK, donationRequirement.StatusCode);
        Assert.True(donationRequirementBody!.AppliesRule);
        Assert.False(donationRequirementBody.IsComplete);
        Assert.Equal("INCOMPLETE", donationRequirementBody.StatusCode);
        Assert.Equal("DONATION_APPLICATION_EVIDENCE_REQUIRED", donationRequirementBody.RuleCode);
        Assert.Equal("MISSING_EVIDENCE", donationRequirementBody.MissingReasonCode);
        Assert.Equal(0, donationRequirementBody.CurrentDocumentCount);
        Assert.Contains(DocumentClassCodes.SupportingDocument, donationRequirementBody.RequiredDocumentClassCodes);
        Assert.Contains("Agrega una evidencia", donationRequirementBody.RemediationHint, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.OK, federationRequirement.StatusCode);
        Assert.True(federationRequirementBody!.AppliesRule);
        Assert.True(federationRequirementBody.IsComplete);
        Assert.Equal("FEDERATION_DONATION_APPLICATION_EVIDENCE_REQUIRED", federationRequirementBody.RuleCode);

        Assert.Equal(HttpStatusCode.OK, marketComplete.StatusCode);
        Assert.True(marketCompleteBody!.IsComplete);
        Assert.Equal("COMPLETE", marketCompleteBody.StatusCode);
        Assert.Equal("MARKET_TENANT_CERTIFICATE_REQUIRED", marketCompleteBody.RuleCode);
        Assert.Equal("MARKET_TENANT_CERTIFICATE", marketCompleteBody.RequiredDocumentCode);
        Assert.Contains(DocumentClassCodes.Certificate, marketCompleteBody.RequiredDocumentClassCodes);
        Assert.Equal(1, marketCompleteBody.MinimumRequiredCount);
        Assert.Equal(1, marketCompleteBody.RelatedDocumentCount);
        Assert.Contains(completeMarketDocumentId, marketCompleteBody.CoveringDocumentIds);

        Assert.Equal(HttpStatusCode.OK, marketIncomplete.StatusCode);
        Assert.False(marketIncompleteBody!.IsComplete);
        Assert.Equal("INCOMPLETE", marketIncompleteBody.StatusCode);
        Assert.Equal("MARKET_TENANT_CERTIFICATE_REQUIRED", marketIncompleteBody.RuleCode);
        Assert.Equal("MISSING_REQUIRED_DOCUMENT", marketIncompleteBody.MissingReasonCode);
        Assert.Contains("Carga la cedula", marketIncompleteBody.RemediationHint, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("MARKET_TENANT", marketIncompleteBody.OriginContext.EntityType);

        Assert.Equal(HttpStatusCode.OK, donationComplete.StatusCode);
        Assert.True(donationCompleteBody!.IsComplete);
        Assert.Equal("DONATION_APPLICATION_EVIDENCE_REQUIRED", donationCompleteBody.RuleCode);
        Assert.Equal("DONATION_APPLICATION_EVIDENCE", donationCompleteBody.RequiredDocumentCode);
        Assert.Contains(DocumentClassCodes.SupportingDocument, donationCompleteBody.RequiredDocumentClassCodes);
        Assert.Contains(completeDonationDocumentId, donationCompleteBody.CoveringDocumentIds);

        Assert.Equal(HttpStatusCode.OK, federationComplete.StatusCode);
        Assert.True(federationCompleteBody!.IsComplete);
        Assert.Equal("FEDERATION_DONATION_APPLICATION_EVIDENCE_REQUIRED", federationCompleteBody.RuleCode);
        Assert.Equal("FEDERATION_DONATION_APPLICATION_EVIDENCE", federationCompleteBody.RequiredDocumentCode);
        Assert.Contains(DocumentClassCodes.SupportingDocument, federationCompleteBody.RequiredDocumentClassCodes);
        Assert.Contains(completeFederationDocumentId, federationCompleteBody.CoveringDocumentIds);

        Assert.Equal(HttpStatusCode.OK, pendingMarkets.StatusCode);
        Assert.Contains(pendingMarketsBody!.Items, item =>
            item.EntityId == incompleteMarketTenant.TenantId
            && item.RuleCode == "MARKET_TENANT_CERTIFICATE_REQUIRED"
            && item.MissingReasonCode == "MISSING_REQUIRED_DOCUMENT"
            && item.RequiredDocumentClassCodes.Contains(DocumentClassCodes.Certificate)
            && item.MinimumRequiredCount == 1);
        Assert.DoesNotContain(pendingMarketsBody.Items, item => item.EntityId == completeMarketTenant.TenantId);

        Assert.Equal(HttpStatusCode.OK, pendingAll.StatusCode);
        Assert.Contains(pendingAllBody!.Items, item =>
            item.EntityId == incompleteDonation.ApplicationId
            && item.RuleCode == "DONATION_APPLICATION_EVIDENCE_REQUIRED"
            && item.MissingReasonCode == "MISSING_EVIDENCE"
            && item.RemediationHint.Contains("Agrega una evidencia", StringComparison.OrdinalIgnoreCase)
            && item.RequiredDocumentClassCodes.Contains(DocumentClassCodes.SupportingDocument)
            && item.MinimumRequiredCount == 1);
        Assert.Contains(pendingAllBody.Items, item =>
            item.EntityId == incompleteFederation.ApplicationId
            && item.RuleCode == "FEDERATION_DONATION_APPLICATION_EVIDENCE_REQUIRED"
            && item.MissingReasonCode == "MISSING_EVIDENCE"
            && item.RequiredDocumentClassCodes.Contains(DocumentClassCodes.SupportingDocument)
            && item.MinimumRequiredCount == 1);
        Assert.DoesNotContain(pendingAllBody.Items, item => item.EntityId == completeDonation.ApplicationId);
        Assert.DoesNotContain(pendingAllBody.Items, item => item.EntityId == completeFederation.ApplicationId);
    }

    [Fact]
    public async Task Document_work_queue_consolidates_completion_integrity_and_retention_signals()
    {
        var incompleteMarketTenant = await SeedMarketTenantAsync();
        var missingFileDocumentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "workqueue-integridad.pdf",
            "application/pdf",
            "missing work queue regression",
            createPhysicalFile: false,
            documentClassCode: DocumentClassCodes.SupportingDocument);
        var reviewDueDocumentId = await SeedStoredDocumentAsync(
            "FEDERATION",
            DocumentAreaCodes.FederationApplicationEvidences,
            "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            "workqueue-retencion.pdf",
            "application/pdf",
            "review due work queue regression",
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(10));
        var privateDocumentId = await SeedStoredDocumentAsync(
            "PRIVATE",
            DocumentAreaCodes.MarketsTenantCertificates,
            "PRIVATE_DOCUMENT",
            "workqueue-privado.pdf",
            "application/pdf",
            "private work queue regression",
            createPhysicalFile: false);
        var adminToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var readOnlyToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName, AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var allQueue = await SendGetAsync("/api/documents/work-queue?take=200", adminToken);
        var allQueueBody = await allQueue.Content.ReadFromJsonAsync<DocumentWorkQueueTestResponse>();
        using var marketsQueue = await SendGetAsync("/api/documents/work-queue?moduleCode=MARKETS&take=200", adminToken);
        var marketsQueueBody = await marketsQueue.Content.ReadFromJsonAsync<DocumentWorkQueueTestResponse>();
        using var integrityQueue = await SendGetAsync("/api/documents/work-queue?workItemType=DOCUMENT_INTEGRITY_ISSUE&take=200", adminToken);
        var integrityQueueBody = await integrityQueue.Content.ReadFromJsonAsync<DocumentWorkQueueTestResponse>();
        using var highQueue = await SendGetAsync("/api/documents/work-queue?severityCode=HIGH&take=200", adminToken);
        var highQueueBody = await highQueue.Content.ReadFromJsonAsync<DocumentWorkQueueTestResponse>();
        using var reviewDueQueue = await SendGetAsync("/api/documents/work-queue?workItemType=RETENTION_REVIEW&severityCode=LOW&take=200", adminToken);
        var reviewDueQueueBody = await reviewDueQueue.Content.ReadFromJsonAsync<DocumentWorkQueueTestResponse>();
        using var readOnlyFederationQueue = await SendGetAsync("/api/documents/work-queue?moduleCode=FEDERATION&take=20", readOnlyToken);
        var readOnlyFederationBody = await readOnlyFederationQueue.Content.ReadFromJsonAsync<DocumentWorkQueueTestResponse>();

        Assert.Equal(HttpStatusCode.OK, allQueue.StatusCode);
        Assert.Contains(allQueueBody!.Items, item =>
            item.WorkItemType == "COMPLETENESS_PENDING"
            && item.SeverityCode == "HIGH"
            && item.ModuleCode == "MARKETS"
            && item.EntityType == "MARKET_TENANT"
            && item.EntityId == incompleteMarketTenant.TenantId
            && item.ReasonCode == "MISSING_REQUIRED_DOCUMENT"
            && item.RouteHint == $"/markets?marketId={incompleteMarketTenant.MarketId}&tenantId={incompleteMarketTenant.TenantId}");
        Assert.Contains(allQueueBody.Items, item =>
            item.WorkItemType == "DOCUMENT_INTEGRITY_ISSUE"
            && item.SeverityCode == "HIGH"
            && item.ModuleCode == "DONATARIAS"
            && item.DocumentId == missingFileDocumentId
            && item.ReasonCode == "MISSING_FILE"
            && item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.IntegrityIssue
            && item.DocumentOperationalSeverityCode == DocumentOperationalStatusCodes.SeverityHigh
            && item.DocumentDetailUrl == $"/api/documents/{missingFileDocumentId}");
        Assert.Contains(allQueueBody.Items, item =>
            item.WorkItemType == "RETENTION_REVIEW"
            && item.SeverityCode == "LOW"
            && item.ModuleCode == "FEDERATION"
            && item.DocumentId == reviewDueDocumentId
            && item.ReasonCode == DocumentRetentionPolicyCodes.ReviewDue
            && item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.ReviewDue
            && item.DocumentOperationalSeverityCode == DocumentOperationalStatusCodes.SeverityLow);
        Assert.DoesNotContain(allQueueBody.Items, item => item.DocumentId == privateDocumentId || item.ModuleCode == "PRIVATE");

        Assert.Equal(HttpStatusCode.OK, marketsQueue.StatusCode);
        Assert.All(marketsQueueBody!.Items, item => Assert.Equal("MARKETS", item.ModuleCode));
        Assert.Contains(marketsQueueBody.Items, item => item.EntityId == incompleteMarketTenant.TenantId);
        Assert.DoesNotContain(marketsQueueBody.Items, item => item.DocumentId == missingFileDocumentId || item.DocumentId == reviewDueDocumentId);

        Assert.Equal(HttpStatusCode.OK, integrityQueue.StatusCode);
        Assert.All(integrityQueueBody!.Items, item => Assert.Equal("DOCUMENT_INTEGRITY_ISSUE", item.WorkItemType));
        Assert.Contains(integrityQueueBody.Items, item => item.DocumentId == missingFileDocumentId);

        Assert.Equal(HttpStatusCode.OK, highQueue.StatusCode);
        Assert.All(highQueueBody!.Items, item => Assert.Equal("HIGH", item.SeverityCode));
        Assert.Contains(highQueueBody.Items, item => item.DocumentId == missingFileDocumentId);
        Assert.DoesNotContain(highQueueBody.Items, item => item.DocumentId == reviewDueDocumentId);

        Assert.Equal(HttpStatusCode.OK, reviewDueQueue.StatusCode);
        Assert.All(reviewDueQueueBody!.Items, item =>
        {
            Assert.Equal("RETENTION_REVIEW", item.WorkItemType);
            Assert.Equal("LOW", item.SeverityCode);
        });
        Assert.Contains(reviewDueQueueBody.Items, item => item.DocumentId == reviewDueDocumentId);

        Assert.Equal(HttpStatusCode.OK, readOnlyFederationQueue.StatusCode);
        Assert.All(readOnlyFederationBody!.Items, item => Assert.Equal("FEDERATION", item.ModuleCode));
        Assert.Contains(readOnlyFederationBody.Items, item => item.DocumentId == reviewDueDocumentId);
    }

    [Fact]
    public async Task Document_light_exports_cover_catalog_work_queue_and_review_queue_with_safe_csv_headers()
    {
        var marketTenant = await SeedMarketTenantAsync();
        var catalogDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "export-catalogo.pdf",
            "application/pdf",
            "catalog export regression",
            entityId: marketTenant.TenantId,
            documentClassCode: DocumentClassCodes.Certificate,
            isPrimaryDocument: true);
        var missingFileDocumentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "export-workqueue.pdf",
            "application/pdf",
            "work queue export regression",
            createPhysicalFile: false,
            documentClassCode: DocumentClassCodes.SupportingDocument);
        var reviewDueDocumentId = await SeedStoredDocumentAsync(
            "FEDERATION",
            DocumentAreaCodes.FederationApplicationEvidences,
            "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            "export-reviewqueue.pdf",
            "application/pdf",
            "review queue export regression",
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(10));
        var adminToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var readOnlyToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName, AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var catalogExport = await SendGetAsync(
            $"/api/documents/export?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={marketTenant.TenantId}&take=200",
            adminToken);
        using var workQueueExport = await SendGetAsync(
            "/api/documents/work-queue/export?workItemType=DOCUMENT_INTEGRITY_ISSUE&take=200",
            adminToken);
        using var reviewQueueExport = await SendGetAsync(
            "/api/documents/review-queue/export?moduleCode=FEDERATION&take=200",
            adminToken);
        using var forbiddenReviewQueueExport = await SendGetAsync(
            "/api/documents/review-queue/export?take=20",
            readOnlyToken);
        var catalogCsv = await catalogExport.Content.ReadAsStringAsync();
        var workQueueCsv = await workQueueExport.Content.ReadAsStringAsync();
        var reviewQueueCsv = await reviewQueueExport.Content.ReadAsStringAsync();

        AssertCsvDownloadHeaders(catalogExport, "documents-catalog");
        Assert.Contains("documentId,moduleCode,moduleName,documentAreaCode", catalogCsv);
        Assert.Contains(catalogDocumentId.ToString(), catalogCsv);
        Assert.Contains("MARKETS", catalogCsv);
        Assert.DoesNotContain("storedRelativePath", catalogCsv, StringComparison.OrdinalIgnoreCase);

        AssertCsvDownloadHeaders(workQueueExport, "documents-work-queue");
        Assert.Contains("workItemKey,workItemType,severity,moduleCode", workQueueCsv);
        Assert.Contains(missingFileDocumentId.ToString(), workQueueCsv);
        Assert.Contains("DOCUMENT_INTEGRITY_ISSUE", workQueueCsv);
        Assert.Contains("REVIEW", workQueueCsv);
        Assert.DoesNotContain("storedRelativePath", workQueueCsv, StringComparison.OrdinalIgnoreCase);

        AssertCsvDownloadHeaders(reviewQueueExport, "documents-review-queue");
        Assert.Contains("documentId,moduleCode,moduleName,entityType", reviewQueueCsv);
        Assert.Contains(reviewDueDocumentId.ToString(), reviewQueueCsv);
        Assert.Contains(DocumentRetentionReviewStatusCodes.Pending, reviewQueueCsv);
        Assert.DoesNotContain("storedRelativePath", reviewQueueCsv, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenReviewQueueExport.StatusCode);
    }

    [Fact]
    public async Task Document_summary_reports_kpis_and_respects_mapped_module_access()
    {
        var incompleteMarketTenant = await SeedMarketTenantAsync();
        var activeMarketDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "summary-active-certificate.pdf",
            "application/pdf",
            "summary active certificate regression",
            documentClassCode: DocumentClassCodes.Certificate,
            isPrimaryDocument: true);
        var archivedMarketDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "summary-archived-certificate.pdf",
            "application/pdf",
            "summary archived certificate regression",
            documentClassCode: DocumentClassCodes.Certificate);
        var missingFileDocumentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "summary-integrity.pdf",
            "application/pdf",
            "summary missing file regression",
            createPhysicalFile: false,
            documentClassCode: DocumentClassCodes.SupportingDocument);
        var reviewDueDocumentId = await SeedStoredDocumentAsync(
            "FEDERATION",
            DocumentAreaCodes.FederationApplicationEvidences,
            "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            "summary-review-due.pdf",
            "application/pdf",
            "summary review due regression",
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(10));
        var expiredDocumentId = await SeedStoredDocumentAsync(
            "FEDERATION",
            DocumentAreaCodes.FederationApplicationEvidences,
            "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            "summary-expired.pdf",
            "application/pdf",
            "summary expired regression",
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(-2));
        var privateDocumentId = await SeedStoredDocumentAsync(
            "PRIVATE",
            DocumentAreaCodes.MarketsTenantCertificates,
            "PRIVATE_DOCUMENT",
            "summary-private.pdf",
            "application/pdf",
            "summary private regression",
            createPhysicalFile: false);
        await ArchiveStoredDocumentForTestAsync(archivedMarketDocumentId);
        var adminToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var readOnlyToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName, AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var summary = await SendGetAsync("/api/documents/summary", adminToken);
        var summaryBody = await summary.Content.ReadFromJsonAsync<DocumentSummaryTestResponse>();
        using var readOnlySummary = await SendGetAsync("/api/documents/summary", readOnlyToken);
        var readOnlySummaryBody = await readOnlySummary.Content.ReadFromJsonAsync<DocumentSummaryTestResponse>();

        var markets = Assert.Single(summaryBody!.Modules, item => item.ModuleCode == "MARKETS");
        var donations = Assert.Single(summaryBody.Modules, item => item.ModuleCode == "DONATARIAS");
        var federation = Assert.Single(summaryBody.Modules, item => item.ModuleCode == "FEDERATION");

        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        Assert.True(summaryBody.TotalDocuments >= 5);
        Assert.True(summaryBody.ActiveDocuments >= 4);
        Assert.True(summaryBody.ArchivedDocuments >= 1);
        Assert.True(summaryBody.IntegrityIssuesCount >= 1);
        Assert.True(summaryBody.IncompleteEntitiesCount >= 1);
        Assert.True(summaryBody.ReviewDueCount >= 1);
        Assert.True(summaryBody.ExpiredRetentionCount >= 1);
        Assert.DoesNotContain(summaryBody.Modules, item => item.ModuleCode == "PRIVATE");
        Assert.True(markets.TotalDocuments >= 2);
        Assert.True(markets.ActiveDocuments >= 1);
        Assert.True(markets.ArchivedDocuments >= 1);
        Assert.True(markets.IncompleteEntitiesCount >= 1);
        Assert.True(donations.IntegrityIssuesCount >= 1);
        Assert.True(federation.ReviewDueCount >= 1);
        Assert.True(federation.ExpiredRetentionCount >= 1);
        Assert.Contains(summaryBody.DocumentClasses, item =>
            item.DocumentClassCode == DocumentClassCodes.Certificate
            && item.TotalDocuments >= 2);
        Assert.Contains(summaryBody.DocumentClasses, item =>
            item.DocumentClassCode == DocumentClassCodes.SupportingDocument
            && item.TotalDocuments >= 3);
        Assert.Contains(summaryBody.OperationalStatuses, item =>
            item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.IntegrityIssue
            && item.DocumentOperationalSeverityCode == DocumentOperationalStatusCodes.SeverityHigh
            && item.TotalCount >= 1);
        Assert.Contains(summaryBody.OperationalStatuses, item =>
            item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.Archived
            && item.DocumentOperationalSeverityCode == DocumentOperationalStatusCodes.SeverityLow
            && item.TotalCount >= 1);
        Assert.Contains(summaryBody.OperationalStatuses, item =>
            item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.RetentionExpired
            && item.DocumentOperationalSeverityCode == DocumentOperationalStatusCodes.SeverityMedium
            && item.TotalCount >= 1);
        Assert.Contains(summaryBody.WorkQueueCategories, item =>
            item.WorkItemType == "COMPLETENESS_PENDING"
            && item.ReasonCode == "MISSING_REQUIRED_DOCUMENT"
            && item.SeverityCode == "HIGH"
            && item.TotalCount >= 1);
        Assert.Contains(summaryBody.WorkQueueCategories, item =>
            item.WorkItemType == "DOCUMENT_INTEGRITY_ISSUE"
            && item.ReasonCode == "MISSING_FILE"
            && item.SeverityCode == "HIGH"
            && item.TotalCount >= 1);
        Assert.Contains(summaryBody.WorkQueueCategories, item =>
            item.WorkItemType == "RETENTION_REVIEW"
            && item.ReasonCode == DocumentRetentionPolicyCodes.ReviewDue
            && item.SeverityCode == "LOW"
            && item.TotalCount >= 1);
        Assert.Contains(summaryBody.WorkQueueCategories, item =>
            item.WorkItemType == "RETENTION_REVIEW"
            && item.ReasonCode == DocumentRetentionPolicyCodes.ExpiredRetention
            && item.SeverityCode == "MEDIUM"
            && item.TotalCount >= 1);

        Assert.Equal(HttpStatusCode.OK, readOnlySummary.StatusCode);
        Assert.DoesNotContain(readOnlySummaryBody!.Modules, item => item.ModuleCode == "PRIVATE");
        Assert.True(readOnlySummaryBody.TotalDocuments >= 5);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        Assert.NotNull(dbContext.StoredDocuments.SingleOrDefault(item => item.Id == activeMarketDocumentId));
        Assert.NotNull(dbContext.StoredDocuments.SingleOrDefault(item => item.Id == missingFileDocumentId));
        Assert.NotNull(dbContext.StoredDocuments.SingleOrDefault(item => item.Id == reviewDueDocumentId));
        Assert.NotNull(dbContext.StoredDocuments.SingleOrDefault(item => item.Id == expiredDocumentId));
        Assert.NotNull(dbContext.StoredDocuments.SingleOrDefault(item => item.Id == privateDocumentId));
        Assert.NotNull(dbContext.MarketTenants.SingleOrDefault(item => item.Id == incompleteMarketTenant.TenantId));
    }

    [Fact]
    public async Task Document_catalog_filters_by_document_class_code_and_keeps_legacy_default_class()
    {
        var certificateId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "cedula-clasificada.pdf",
            "application/pdf",
            "classified certificate regression",
            documentClassCode: DocumentClassCodes.Certificate,
            businessPurpose: "Acreditar la cedula digitalizada del locatario.",
            isPrimaryDocument: true);
        var defaultId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "evidencia-default.pdf",
            "application/pdf",
            "default class regression");
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var filtered = await SendGetAsync($"/api/documents?documentClassCode={DocumentClassCodes.Certificate}&take=200", token);
        var filteredBody = await filtered.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var detail = await SendGetAsync($"/api/documents/{defaultId}", token);
        var detailBody = await detail.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();

        Assert.Equal(HttpStatusCode.OK, filtered.StatusCode);
        Assert.Contains(filteredBody!.Items, item => item.Id == certificateId && item.DocumentClassCode == DocumentClassCodes.Certificate);
        Assert.DoesNotContain(filteredBody.Items, item => item.Id == defaultId);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Equal(DocumentClassCodes.Other, detailBody!.DocumentClassCode);
        Assert.False(detailBody.IsPrimaryDocument);
    }

    [Fact]
    public async Task Document_catalog_filters_by_retention_policy_and_status_without_blocking_expired_downloads()
    {
        var signedDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "contrato-firmado.pdf",
            "application/pdf",
            "signed retention regression",
            documentClassCode: DocumentClassCodes.SignedDocument);
        var reviewDueDocumentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "evidencia-revision.pdf",
            "application/pdf",
            "review due retention regression",
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(10));
        var expiredDocumentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "evidencia-expirada.pdf",
            "application/pdf",
            "expired retention regression",
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(-1));
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var policyFiltered = await SendGetAsync($"/api/documents?retentionPolicyCode={DocumentRetentionPolicyCodes.EvidenceMediumTerm}&take=200", token);
        var policyBody = await policyFiltered.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var activeFiltered = await SendGetAsync($"/api/documents?retentionStatusCode={DocumentRetentionPolicyCodes.ActiveRetention}&take=200", token);
        var activeBody = await activeFiltered.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var reviewDueFiltered = await SendGetAsync($"/api/documents?retentionStatusCode={DocumentRetentionPolicyCodes.ReviewDue}&take=200", token);
        var reviewDueBody = await reviewDueFiltered.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var expiredFiltered = await SendGetAsync($"/api/documents?retentionStatusCode={DocumentRetentionPolicyCodes.ExpiredRetention}&take=200", token);
        var expiredBody = await expiredFiltered.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var expiredDownload = await SendGetAsync($"/api/documents/{expiredDocumentId}/download", token);

        Assert.Equal(HttpStatusCode.OK, policyFiltered.StatusCode);
        Assert.Contains(policyBody!.Items, item => item.Id == reviewDueDocumentId && item.RetentionPolicyCode == DocumentRetentionPolicyCodes.EvidenceMediumTerm);
        Assert.Contains(policyBody.Items, item => item.Id == expiredDocumentId && item.RetentionPolicyCode == DocumentRetentionPolicyCodes.EvidenceMediumTerm);

        Assert.Equal(HttpStatusCode.OK, activeFiltered.StatusCode);
        Assert.Contains(activeBody!.Items, item => item.Id == signedDocumentId && item.RetentionStatusCode == DocumentRetentionPolicyCodes.ActiveRetention);

        Assert.Equal(HttpStatusCode.OK, reviewDueFiltered.StatusCode);
        Assert.Contains(reviewDueBody!.Items, item => item.Id == reviewDueDocumentId && item.RetentionStatusCode == DocumentRetentionPolicyCodes.ReviewDue);
        Assert.DoesNotContain(reviewDueBody.Items, item => item.Id == expiredDocumentId);

        Assert.Equal(HttpStatusCode.OK, expiredFiltered.StatusCode);
        Assert.Contains(expiredBody!.Items, item => item.Id == expiredDocumentId && item.RetentionStatusCode == DocumentRetentionPolicyCodes.ExpiredRetention);

        Assert.Equal(HttpStatusCode.OK, expiredDownload.StatusCode);
        Assert.Equal("expired retention regression", await expiredDownload.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Document_catalog_exposes_operational_status_and_applies_documented_precedence()
    {
        var activeOkDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "operational-active-ok.pdf",
            "application/pdf",
            "operational active ok regression",
            documentClassCode: DocumentClassCodes.Certificate,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(90));
        var integrityHeldDocumentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "operational-integrity-held.pdf",
            "application/pdf",
            "operational integrity precedence regression",
            createPhysicalFile: false,
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(-2));
        var holdExpiredDocumentId = await SeedStoredDocumentAsync(
            "FEDERATION",
            DocumentAreaCodes.FederationApplicationEvidences,
            "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            "operational-hold-expired.pdf",
            "application/pdf",
            "operational hold precedence regression",
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(-2));
        var supersededExpiredDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "operational-superseded-expired.pdf",
            "application/pdf",
            "operational superseded precedence regression",
            documentClassCode: DocumentClassCodes.Certificate,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(-2));
        var replacementDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "operational-superseding.pdf",
            "application/pdf",
            "operational superseding regression",
            documentClassCode: DocumentClassCodes.Certificate,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(90));
        var archivedExpiredDocumentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "operational-archived-expired.pdf",
            "application/pdf",
            "operational archived precedence regression",
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(-2));
        var expiredDocumentId = await SeedStoredDocumentAsync(
            "FEDERATION",
            DocumentAreaCodes.FederationApplicationEvidences,
            "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            "operational-expired.pdf",
            "application/pdf",
            "operational expired regression",
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(-2));
        var reviewDueDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "operational-review-due.pdf",
            "application/pdf",
            "operational review due regression",
            documentClassCode: DocumentClassCodes.Certificate,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(5));
        await SetAdministrativeHoldForTestAsync(integrityHeldDocumentId);
        await SetAdministrativeHoldForTestAsync(holdExpiredDocumentId);
        await MarkStoredDocumentSupersededForTestAsync(supersededExpiredDocumentId, replacementDocumentId);
        await ArchiveStoredDocumentForTestAsync(archivedExpiredDocumentId);
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var list = await SendGetAsync("/api/documents?includeArchived=true&take=200", token);
        var listBody = await list.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var holdFiltered = await SendGetAsync($"/api/documents?documentOperationalStatusCode={DocumentOperationalStatusCodes.OnHold}&includeArchived=true&take=200", token);
        var holdFilteredBody = await holdFiltered.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var supersededDetail = await SendGetAsync($"/api/documents/{supersededExpiredDocumentId}", token);
        var supersededDetailBody = await supersededDetail.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();
        using var summary = await SendGetAsync("/api/documents/summary", token);
        var summaryBody = await summary.Content.ReadFromJsonAsync<DocumentSummaryTestResponse>();

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains(listBody!.Items, item =>
            item.Id == activeOkDocumentId
            && item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.ActiveOk
            && item.DocumentOperationalSeverityCode == DocumentOperationalStatusCodes.SeverityNone);
        Assert.Contains(listBody.Items, item =>
            item.Id == integrityHeldDocumentId
            && item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.IntegrityIssue
            && item.DocumentOperationalSeverityCode == DocumentOperationalStatusCodes.SeverityHigh);
        Assert.Contains(listBody.Items, item =>
            item.Id == holdExpiredDocumentId
            && item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.OnHold
            && item.DocumentOperationalSeverityCode == DocumentOperationalStatusCodes.SeverityLow);
        Assert.Contains(listBody.Items, item =>
            item.Id == supersededExpiredDocumentId
            && item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.Superseded
            && item.DocumentOperationalSeverityCode == DocumentOperationalStatusCodes.SeverityLow);
        Assert.Contains(listBody.Items, item =>
            item.Id == archivedExpiredDocumentId
            && item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.Archived
            && item.DocumentOperationalSeverityCode == DocumentOperationalStatusCodes.SeverityLow);
        Assert.Contains(listBody.Items, item =>
            item.Id == expiredDocumentId
            && item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.RetentionExpired
            && item.DocumentOperationalSeverityCode == DocumentOperationalStatusCodes.SeverityMedium);
        Assert.Contains(listBody.Items, item =>
            item.Id == reviewDueDocumentId
            && item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.ReviewDue
            && item.DocumentOperationalSeverityCode == DocumentOperationalStatusCodes.SeverityLow);

        Assert.Equal(HttpStatusCode.OK, holdFiltered.StatusCode);
        Assert.Contains(holdFilteredBody!.Items, item => item.Id == holdExpiredDocumentId);
        Assert.DoesNotContain(holdFilteredBody.Items, item => item.Id == integrityHeldDocumentId);
        Assert.Equal(HttpStatusCode.OK, supersededDetail.StatusCode);
        Assert.Equal(DocumentOperationalStatusCodes.Superseded, supersededDetailBody!.DocumentOperationalStatusCode);
        Assert.Equal(HttpStatusCode.OK, summary.StatusCode);
        Assert.Contains(summaryBody!.OperationalStatuses, item =>
            item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.IntegrityIssue
            && item.TotalCount >= 1);
        Assert.Contains(summaryBody.OperationalStatuses, item =>
            item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.OnHold
            && item.TotalCount >= 1);
        Assert.Contains(summaryBody.OperationalStatuses, item =>
            item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.Superseded
            && item.TotalCount >= 1);
        Assert.Contains(summaryBody.OperationalStatuses, item =>
            item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.RetentionExpired
            && item.TotalCount >= 1);
    }

    [Fact]
    public async Task Admin_can_review_and_defer_retention_queue_without_changing_downloads()
    {
        var reviewDueDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "cedula-review-due.pdf",
            "application/pdf",
            "review due queue regression",
            documentClassCode: DocumentClassCodes.Certificate,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(5));
        var expiredDocumentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "evidencia-review-expired.pdf",
            "application/pdf",
            "expired review queue regression",
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(-3));
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var initialQueue = await SendGetAsync("/api/documents/review-queue?take=200", token);
        var initialQueueBody = await initialQueue.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var reviewed = await SendPatchAsync(
            $"/api/documents/{expiredDocumentId}/retention-review",
            token,
            new
            {
                retentionReviewStatusCode = DocumentRetentionReviewStatusCodes.Completed,
                retentionReviewNotes = "Revision operativa completada."
            });
        var reviewedDetail = await reviewed.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();
        using var deferred = await SendPatchAsync(
            $"/api/documents/{reviewDueDocumentId}/retention-review",
            token,
            new
            {
                retentionReviewStatusCode = DocumentRetentionReviewStatusCodes.Deferred,
                nextRetentionReviewUtc = DateTimeOffset.UtcNow.AddDays(45),
                retentionReviewNotes = "Revisar de nuevo despues de documentacion pendiente."
            });
        var deferredDetail = await deferred.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();
        using var defaultQueueAfterActions = await SendGetAsync("/api/documents/review-queue?take=200", token);
        var defaultQueueBody = await defaultQueueAfterActions.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var deferredQueue = await SendGetAsync($"/api/documents/review-queue?retentionReviewStatusCode={DocumentRetentionReviewStatusCodes.Deferred}&take=200", token);
        var deferredQueueBody = await deferredQueue.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var expiredDownload = await SendGetAsync($"/api/documents/{expiredDocumentId}/download", token);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var actionTypes = dbContext.AuditEvents
            .Where(item => item.EntityId == expiredDocumentId.ToString()
                           || item.EntityId == reviewDueDocumentId.ToString())
            .Select(item => item.ActionType)
            .ToArray();

        Assert.Equal(HttpStatusCode.OK, initialQueue.StatusCode);
        Assert.Contains(initialQueueBody!.Items, item => item.Id == reviewDueDocumentId && item.RetentionReviewStatusCode == DocumentRetentionReviewStatusCodes.Pending);
        Assert.Contains(initialQueueBody.Items, item => item.Id == expiredDocumentId && item.RetentionReviewStatusCode == DocumentRetentionReviewStatusCodes.Pending);

        Assert.Equal(HttpStatusCode.OK, reviewed.StatusCode);
        Assert.Equal(DocumentRetentionReviewStatusCodes.Completed, reviewedDetail!.RetentionReviewStatusCode);
        Assert.NotNull(reviewedDetail.LastRetentionReviewUtc);
        Assert.Null(reviewedDetail.NextRetentionReviewUtc);
        Assert.Equal("Revision operativa completada.", reviewedDetail.RetentionReviewNotes);

        Assert.Equal(HttpStatusCode.OK, deferred.StatusCode);
        Assert.Equal(DocumentRetentionReviewStatusCodes.Deferred, deferredDetail!.RetentionReviewStatusCode);
        Assert.NotNull(deferredDetail.LastRetentionReviewUtc);
        Assert.NotNull(deferredDetail.NextRetentionReviewUtc);
        Assert.Equal("Revisar de nuevo despues de documentacion pendiente.", deferredDetail.RetentionReviewNotes);

        Assert.DoesNotContain(defaultQueueBody!.Items, item => item.Id == expiredDocumentId);
        Assert.DoesNotContain(defaultQueueBody.Items, item => item.Id == reviewDueDocumentId);
        Assert.Contains(deferredQueueBody!.Items, item => item.Id == reviewDueDocumentId && item.RetentionReviewStatusCode == DocumentRetentionReviewStatusCodes.Deferred);

        Assert.Equal(HttpStatusCode.OK, expiredDownload.StatusCode);
        Assert.Equal("expired review queue regression", await expiredDownload.Content.ReadAsStringAsync());
        Assert.Contains("DOCUMENT_RETENTION_REVIEWED", actionTypes);
        Assert.Contains("DOCUMENT_RETENTION_REVIEW_DEFERRED", actionTypes);
    }

    [Fact]
    public async Task Admin_can_set_and_clear_retention_override_and_effective_retention_drives_queues()
    {
        var documentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "evidencia-retention-override.pdf",
            "application/pdf",
            "retention override regression",
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionPolicyCode: DocumentRetentionPolicyCodes.EvidenceMediumTerm,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(90));
        var adminToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var operatorToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.OperatorUserName, AuthorizationRegressionWebApplicationFactory.OperatorPassword);

        using var summaryBefore = await SendGetAsync("/api/documents/summary", adminToken);
        var summaryBeforeBody = await summaryBefore.Content.ReadFromJsonAsync<DocumentSummaryTestResponse>();
        using var queueBefore = await SendGetAsync("/api/documents/review-queue?take=200", adminToken);
        var queueBeforeBody = await queueBefore.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();

        using var setOverride = await SendPatchAsync(
            $"/api/documents/{documentId}/retention-override",
            adminToken,
            new
            {
                retentionOverridePolicyCode = DocumentRetentionPolicyCodes.GenericReview,
                retentionOverrideUntilUtc = DateTimeOffset.UtcNow.AddDays(-2),
                retentionOverrideReason = "Excepcion operativa de retencion para regresion."
            });
        var setDetail = await setOverride.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();
        using var expiredFiltered = await SendGetAsync($"/api/documents?retentionStatusCode={DocumentRetentionPolicyCodes.ExpiredRetention}&take=200", adminToken);
        var expiredBody = await expiredFiltered.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var queueAfterSet = await SendGetAsync("/api/documents/review-queue?take=200", adminToken);
        var queueAfterSetBody = await queueAfterSet.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var workQueueAfterSet = await SendGetAsync("/api/documents/work-queue?workItemType=RETENTION_REVIEW&severityCode=MEDIUM&take=200", adminToken);
        var workQueueAfterSetBody = await workQueueAfterSet.Content.ReadFromJsonAsync<DocumentWorkQueueTestResponse>();
        using var summaryAfterSet = await SendGetAsync("/api/documents/summary", adminToken);
        var summaryAfterSetBody = await summaryAfterSet.Content.ReadFromJsonAsync<DocumentSummaryTestResponse>();

        using var forbiddenSet = await SendPatchAsync(
            $"/api/documents/{documentId}/retention-override",
            operatorToken,
            new
            {
                retentionOverrideUntilUtc = DateTimeOffset.UtcNow.AddDays(30),
                retentionOverrideReason = "Operator attempt"
            });
        using var forbiddenClear = await SendDeleteAsync($"/api/documents/{documentId}/retention-override", operatorToken);

        using var clearOverride = await SendDeleteAsync($"/api/documents/{documentId}/retention-override", adminToken);
        var clearDetail = await clearOverride.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();
        using var queueAfterClear = await SendGetAsync("/api/documents/review-queue?take=200", adminToken);
        var queueAfterClearBody = await queueAfterClear.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var workQueueAfterClear = await SendGetAsync("/api/documents/work-queue?workItemType=RETENTION_REVIEW&take=200", adminToken);
        var workQueueAfterClearBody = await workQueueAfterClear.Content.ReadFromJsonAsync<DocumentWorkQueueTestResponse>();
        using var summaryAfterClear = await SendGetAsync("/api/documents/summary", adminToken);
        var summaryAfterClearBody = await summaryAfterClear.Content.ReadFromJsonAsync<DocumentSummaryTestResponse>();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var actionTypes = dbContext.AuditEvents
            .Where(item => item.EntityId == documentId.ToString())
            .Select(item => item.ActionType)
            .ToArray();

        Assert.Equal(HttpStatusCode.OK, summaryBefore.StatusCode);
        Assert.Equal(HttpStatusCode.OK, queueBefore.StatusCode);
        Assert.DoesNotContain(queueBeforeBody!.Items, item => item.Id == documentId);

        Assert.Equal(HttpStatusCode.OK, setOverride.StatusCode);
        Assert.True(setDetail!.HasRetentionOverride);
        Assert.Equal(DocumentRetentionPolicyCodes.EvidenceMediumTerm, setDetail.RetentionBaselinePolicyCode);
        Assert.Equal(DocumentRetentionPolicyCodes.GenericReview, setDetail.RetentionEffectivePolicyCode);
        Assert.Equal(DocumentRetentionPolicyCodes.GenericReview, setDetail.RetentionPolicyCode);
        Assert.Equal(DocumentRetentionPolicyCodes.ExpiredRetention, setDetail.RetentionStatusCode);
        Assert.Equal(DocumentRetentionPolicyCodes.GenericReview, setDetail.RetentionOverridePolicyCode);
        Assert.NotNull(setDetail.RetentionOverrideUntilUtc);
        Assert.Equal("Excepcion operativa de retencion para regresion.", setDetail.RetentionOverrideReason);
        Assert.Equal(DocumentRetentionReviewStatusCodes.Pending, setDetail.RetentionReviewStatusCode);

        Assert.Contains(expiredBody!.Items, item =>
            item.Id == documentId
            && item.HasRetentionOverride
            && item.RetentionStatusCode == DocumentRetentionPolicyCodes.ExpiredRetention);
        Assert.Contains(queueAfterSetBody!.Items, item => item.Id == documentId);
        Assert.Contains(workQueueAfterSetBody!.Items, item =>
            item.DocumentId == documentId
            && item.ReasonCode == DocumentRetentionPolicyCodes.ExpiredRetention
            && item.SeverityCode == "MEDIUM");
        Assert.True(summaryAfterSetBody!.ExpiredRetentionCount >= summaryBeforeBody!.ExpiredRetentionCount + 1);

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenSet.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenClear.StatusCode);

        Assert.Equal(HttpStatusCode.OK, clearOverride.StatusCode);
        Assert.False(clearDetail!.HasRetentionOverride);
        Assert.Null(clearDetail.RetentionOverridePolicyCode);
        Assert.Null(clearDetail.RetentionOverrideUntilUtc);
        Assert.Null(clearDetail.RetentionOverrideReason);
        Assert.Equal(DocumentRetentionPolicyCodes.EvidenceMediumTerm, clearDetail.RetentionBaselinePolicyCode);
        Assert.Equal(clearDetail.RetentionBaselinePolicyCode, clearDetail.RetentionEffectivePolicyCode);
        Assert.Equal(clearDetail.RetentionBaselineUntilUtc, clearDetail.RetentionEffectiveUntilUtc);
        Assert.Equal(DocumentRetentionPolicyCodes.ActiveRetention, clearDetail.RetentionStatusCode);
        Assert.DoesNotContain(queueAfterClearBody!.Items, item => item.Id == documentId);
        Assert.DoesNotContain(workQueueAfterClearBody!.Items, item => item.DocumentId == documentId);
        Assert.True(summaryAfterClearBody!.ExpiredRetentionCount <= summaryAfterSetBody.ExpiredRetentionCount - 1);
        Assert.Contains("DOCUMENT_RETENTION_OVERRIDE_SET", actionTypes);
        Assert.Contains("DOCUMENT_RETENTION_OVERRIDE_CLEARED", actionTypes);
    }

    [Fact]
    public async Task Admin_can_set_and_clear_administrative_hold_and_retention_queues_pause_actionability()
    {
        var documentId = await SeedStoredDocumentAsync(
            "FEDERATION",
            DocumentAreaCodes.FederationApplicationEvidences,
            "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            "administrative-hold-retention.pdf",
            "application/pdf",
            "administrative hold regression",
            documentClassCode: DocumentClassCodes.SupportingDocument,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(7));
        var adminToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var operatorToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.OperatorUserName, AuthorizationRegressionWebApplicationFactory.OperatorPassword);
        var readOnlyToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName, AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var summaryBefore = await SendGetAsync("/api/documents/summary", adminToken);
        var summaryBeforeBody = await summaryBefore.Content.ReadFromJsonAsync<DocumentSummaryTestResponse>();
        using var reviewQueueBefore = await SendGetAsync("/api/documents/review-queue?take=200", adminToken);
        var reviewQueueBeforeBody = await reviewQueueBefore.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var workQueueBefore = await SendGetAsync("/api/documents/work-queue?workItemType=RETENTION_REVIEW&severityCode=LOW&take=200", adminToken);
        var workQueueBeforeBody = await workQueueBefore.Content.ReadFromJsonAsync<DocumentWorkQueueTestResponse>();

        using var setHold = await SendPostAsync(
            $"/api/documents/{documentId}/hold",
            adminToken,
            new { reason = "Revision administrativa temporal." });
        var holdDetail = await setHold.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();
        using var listAfterHold = await SendGetAsync("/api/documents?moduleCode=FEDERATION&take=200", adminToken);
        var listAfterHoldBody = await listAfterHold.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var detailAfterHold = await SendGetAsync($"/api/documents/{documentId}", adminToken);
        var detailAfterHoldBody = await detailAfterHold.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();
        using var forbiddenSetByReadOnly = await SendPostAsync(
            $"/api/documents/{documentId}/hold",
            readOnlyToken,
            new { reason = "Readonly attempt" });
        using var forbiddenClearByOperator = await SendDeleteAsync($"/api/documents/{documentId}/hold", operatorToken);
        using var blockedRetentionReview = await SendPatchAsync(
            $"/api/documents/{documentId}/retention-review",
            adminToken,
            new
            {
                retentionReviewStatusCode = DocumentRetentionReviewStatusCodes.Completed,
                retentionReviewNotes = "Review while held"
            });
        using var reviewQueueAfterHold = await SendGetAsync("/api/documents/review-queue?take=200", adminToken);
        var reviewQueueAfterHoldBody = await reviewQueueAfterHold.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var workQueueAfterHold = await SendGetAsync("/api/documents/work-queue?workItemType=RETENTION_REVIEW&take=200", adminToken);
        var workQueueAfterHoldBody = await workQueueAfterHold.Content.ReadFromJsonAsync<DocumentWorkQueueTestResponse>();
        using var summaryAfterHold = await SendGetAsync("/api/documents/summary", adminToken);
        var summaryAfterHoldBody = await summaryAfterHold.Content.ReadFromJsonAsync<DocumentSummaryTestResponse>();
        using var downloadWhileHeld = await SendGetAsync($"/api/documents/{documentId}/download", adminToken);

        using var clearHold = await SendDeleteAsync($"/api/documents/{documentId}/hold", adminToken);
        var clearDetail = await clearHold.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();
        using var reviewQueueAfterClear = await SendGetAsync("/api/documents/review-queue?take=200", adminToken);
        var reviewQueueAfterClearBody = await reviewQueueAfterClear.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var workQueueAfterClear = await SendGetAsync("/api/documents/work-queue?workItemType=RETENTION_REVIEW&severityCode=LOW&take=200", adminToken);
        var workQueueAfterClearBody = await workQueueAfterClear.Content.ReadFromJsonAsync<DocumentWorkQueueTestResponse>();
        using var summaryAfterClear = await SendGetAsync("/api/documents/summary", adminToken);
        var summaryAfterClearBody = await summaryAfterClear.Content.ReadFromJsonAsync<DocumentSummaryTestResponse>();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var actionTypes = dbContext.AuditEvents
            .Where(item => item.EntityId == documentId.ToString())
            .Select(item => item.ActionType)
            .ToArray();

        Assert.Equal(HttpStatusCode.OK, summaryBefore.StatusCode);
        Assert.Contains(reviewQueueBeforeBody!.Items, item => item.Id == documentId);
        Assert.Contains(workQueueBeforeBody!.Items, item => item.DocumentId == documentId);

        Assert.Equal(HttpStatusCode.OK, setHold.StatusCode);
        Assert.True(holdDetail!.IsAdministrativeHold);
        Assert.Equal("Revision administrativa temporal.", holdDetail.HoldReason);
        Assert.Equal(DocumentOperationalStatusCodes.OnHold, holdDetail.DocumentOperationalStatusCode);
        Assert.Equal(DocumentOperationalStatusCodes.SeverityLow, holdDetail.DocumentOperationalSeverityCode);
        Assert.NotNull(holdDetail.HoldPlacedUtc);
        Assert.Null(holdDetail.HoldReleasedUtc);
        Assert.Equal("admin", holdDetail.HoldPlacedBy);
        Assert.Contains(listAfterHoldBody!.Items, item =>
            item.Id == documentId
            && item.IsAdministrativeHold
            && item.DocumentOperationalStatusCode == DocumentOperationalStatusCodes.OnHold
            && item.HoldReason == "Revision administrativa temporal.");
        Assert.Equal(HttpStatusCode.OK, detailAfterHold.StatusCode);
        Assert.True(detailAfterHoldBody!.IsAdministrativeHold);
        Assert.Equal(DocumentOperationalStatusCodes.OnHold, detailAfterHoldBody.DocumentOperationalStatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, forbiddenSetByReadOnly.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenClearByOperator.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, blockedRetentionReview.StatusCode);
        Assert.DoesNotContain(reviewQueueAfterHoldBody!.Items, item => item.Id == documentId);
        Assert.DoesNotContain(workQueueAfterHoldBody!.Items, item => item.DocumentId == documentId);
        Assert.True(summaryAfterHoldBody!.AdministrativeHoldCount >= summaryBeforeBody!.AdministrativeHoldCount + 1);
        Assert.True(summaryAfterHoldBody.ReviewDueCount <= summaryBeforeBody.ReviewDueCount - 1);
        Assert.Equal(HttpStatusCode.OK, downloadWhileHeld.StatusCode);
        Assert.Equal("administrative hold regression", await downloadWhileHeld.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, clearHold.StatusCode);
        Assert.False(clearDetail!.IsAdministrativeHold);
        Assert.Null(clearDetail.HoldReason);
        Assert.Null(clearDetail.HoldPlacedUtc);
        Assert.Null(clearDetail.HoldPlacedBy);
        Assert.NotNull(clearDetail.HoldReleasedUtc);
        Assert.Contains(reviewQueueAfterClearBody!.Items, item => item.Id == documentId);
        Assert.Contains(workQueueAfterClearBody!.Items, item => item.DocumentId == documentId);
        Assert.True(summaryAfterClearBody!.AdministrativeHoldCount <= summaryAfterHoldBody.AdministrativeHoldCount - 1);
        Assert.Contains("DOCUMENT_HOLD_SET", actionTypes);
        Assert.Contains("DOCUMENT_HOLD_CLEARED", actionTypes);
    }

    [Fact]
    public async Task Non_admin_cannot_operate_retention_review_queue()
    {
        var documentId = await SeedStoredDocumentAsync(
            "FEDERATION",
            DocumentAreaCodes.FederationApplicationEvidences,
            "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            "retention-review-non-admin.pdf",
            "application/pdf",
            "retention review forbidden regression",
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(-1));
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.OperatorUserName, AuthorizationRegressionWebApplicationFactory.OperatorPassword);

        using var queue = await SendGetAsync("/api/documents/review-queue?take=20", token);
        using var update = await SendPatchAsync(
            $"/api/documents/{documentId}/retention-review",
            token,
            new
            {
                retentionReviewStatusCode = DocumentRetentionReviewStatusCodes.Completed,
                retentionReviewNotes = "Operator attempt"
            });

        Assert.Equal(HttpStatusCode.Forbidden, queue.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }

    [Fact]
    public async Task Market_tenant_upload_assigns_certificate_classification_to_new_stored_document()
    {
        var marketId = await SeedOpenMarketAsync();
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);
        using var content = new MultipartFormDataContent
        {
            { new StringContent("Tenant Classification"), "tenantName" },
            { new StringContent("CERT-001"), "certificateNumber" },
            { new StringContent(DateTime.UtcNow.AddYears(1).ToString("yyyy-MM-dd")), "certificateValidityTo" },
            { new StringContent("Alimentos"), "businessLine" }
        };
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("%PDF-1.4\nclassification regression\n%%EOF\n"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "certificateFile", "tenant-classification.pdf");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/markets/{marketId}/tenants")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _client.SendAsync(request);
        var tenant = await response.Content.ReadFromJsonAsync<MarketTenantUploadTestResponse>();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var storedDocument = dbContext.StoredDocuments.Single(item => item.EntityId == tenant!.Id);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(DocumentClassCodes.Certificate, storedDocument.DocumentClassCode);
        Assert.Equal(DocumentRetentionPolicyCodes.CertificateReview, storedDocument.RetentionPolicyCode);
        Assert.Equal("Acreditar la cedula digitalizada del locatario.", storedDocument.BusinessPurpose);
        Assert.True(storedDocument.IsPrimaryDocument);
    }

    [Fact]
    public async Task Contextual_market_tenant_certificate_remediation_completes_requirement_and_requires_write_permission()
    {
        var marketTenant = await SeedMarketTenantAsync();
        var adminToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var readOnlyToken = await LoginAsync(AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName, AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var before = await SendGetAsync($"/api/documents/completeness/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={marketTenant.TenantId}", adminToken);
        var beforeBody = await before.Content.ReadFromJsonAsync<DocumentCompletenessTestResponse>();

        using var forbiddenContent = BuildCertificateUploadContent("readonly-remediation.pdf");
        using var forbiddenRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/markets/tenants/{marketTenant.TenantId}/cedula")
        {
            Content = forbiddenContent
        };
        forbiddenRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", readOnlyToken);
        using var forbidden = await _client.SendAsync(forbiddenRequest);

        using var content = BuildCertificateUploadContent("tenant-remediated.pdf");
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/markets/tenants/{marketTenant.TenantId}/cedula")
        {
            Content = content
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var upload = await _client.SendAsync(request);

        using var replacementContent = BuildCertificateUploadContent("tenant-remediated-replacement.pdf");
        using var replacementRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/markets/tenants/{marketTenant.TenantId}/cedula")
        {
            Content = replacementContent
        };
        replacementRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        using var replacementUpload = await _client.SendAsync(replacementRequest);

        using var after = await SendGetAsync($"/api/documents/completeness/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={marketTenant.TenantId}", adminToken);
        var afterBody = await after.Content.ReadFromJsonAsync<DocumentCompletenessTestResponse>();
        using var byEntity = await SendGetAsync($"/api/documents/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={marketTenant.TenantId}", adminToken);
        var byEntityBody = await byEntity.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var byEntityWithArchived = await SendGetAsync($"/api/documents/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={marketTenant.TenantId}&includeArchived=true", adminToken);
        var byEntityWithArchivedBody = await byEntityWithArchived.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var storedDocument = dbContext.StoredDocuments.Single(item =>
            item.EntityId == marketTenant.TenantId
            && item.EntityType == "MARKET_TENANT"
            && item.StatusCode == StoredDocument.ActiveStatusCode
            && item.SupersededByDocumentId == null);
        var supersededDocument = dbContext.StoredDocuments.Single(item =>
            item.EntityId == marketTenant.TenantId
            && item.EntityType == "MARKET_TENANT"
            && item.SupersededByDocumentId == storedDocument.Id);
        var tenant = dbContext.MarketTenants.Single(item => item.Id == marketTenant.TenantId);
        var actionTypes = dbContext.AuditEvents
            .Where(item => item.EntityId == marketTenant.TenantId.ToString())
            .Select(item => item.ActionType)
            .ToArray();
        var documentActionTypes = dbContext.AuditEvents
            .Where(item => item.EntityId == storedDocument.Id.ToString())
            .Select(item => item.ActionType)
            .ToArray();

        using var currentTimeline = await SendGetAsync($"/api/documents/{storedDocument.Id}/timeline", adminToken);
        var currentTimelineBody = await currentTimeline.Content.ReadFromJsonAsync<DocumentTimelineTestResponse>();
        using var supersededTimeline = await SendGetAsync($"/api/documents/{supersededDocument.Id}/timeline", adminToken);
        var supersededTimelineBody = await supersededTimeline.Content.ReadFromJsonAsync<DocumentTimelineTestResponse>();
        using var entityTimeline = await SendGetAsync($"/api/documents/timeline/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={marketTenant.TenantId}", adminToken);
        var entityTimelineBody = await entityTimeline.Content.ReadFromJsonAsync<DocumentTimelineTestResponse>();

        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        Assert.False(beforeBody!.IsComplete);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        Assert.Equal(HttpStatusCode.OK, replacementUpload.StatusCode);
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        Assert.True(afterBody!.IsComplete);
        Assert.Equal("COMPLETE", afterBody.StatusCode);
        Assert.Contains(storedDocument.Id, afterBody.CoveringDocumentIds);
        Assert.DoesNotContain(supersededDocument.Id, afterBody.CoveringDocumentIds);
        Assert.Equal(HttpStatusCode.OK, byEntity.StatusCode);
        Assert.Contains(byEntityBody!.Items, item =>
            item.Id == storedDocument.Id
            && item.DocumentClassCode == DocumentClassCodes.Certificate
            && item.StatusCode == StoredDocument.ActiveStatusCode
            && item.ReplacedDocumentId == supersededDocument.Id
            && !item.IsSuperseded);
        Assert.Equal(HttpStatusCode.OK, byEntityWithArchived.StatusCode);
        Assert.Contains(byEntityWithArchivedBody!.Items, item =>
            item.Id == supersededDocument.Id
            && item.IsSuperseded
            && item.SupersededByDocumentId == storedDocument.Id
            && item.StatusCode == StoredDocument.ArchivedStatusCode);
        Assert.Equal("tenant-remediated-replacement.pdf", tenant.CertificateOriginalFileName);
        Assert.Equal(DocumentClassCodes.Certificate, storedDocument.DocumentClassCode);
        Assert.Equal(DocumentRetentionPolicyCodes.CertificateReview, storedDocument.RetentionPolicyCode);
        Assert.True(storedDocument.IsPrimaryDocument);
        Assert.Equal(supersededDocument.Id, storedDocument.ReplacedDocumentId);
        Assert.Equal(storedDocument.Id, supersededDocument.SupersededByDocumentId);
        Assert.Equal(StoredDocument.ArchivedStatusCode, supersededDocument.StatusCode);
        Assert.False(supersededDocument.IsPrimaryDocument);
        Assert.Equal(storedDocument.ReplacementGroupKey, supersededDocument.ReplacementGroupKey);
        Assert.Contains("TENANT_CERTIFICATE_REMEDIATED", actionTypes);
        Assert.Contains("DOCUMENT_REPLACED", documentActionTypes);
        Assert.Equal(HttpStatusCode.OK, currentTimeline.StatusCode);
        Assert.Contains(currentTimelineBody!.Items, item =>
            item.DocumentId == storedDocument.Id
            && item.EventType == "DOCUMENT_UPLOADED"
            && item.IsCurrentDocument);
        Assert.Contains(currentTimelineBody.Items, item =>
            item.DocumentId == storedDocument.Id
            && item.EventType == "DOCUMENT_REPLACED"
            && item.RelatedDocumentId == supersededDocument.Id
            && item.IsCurrentDocument);
        Assert.Equal(HttpStatusCode.OK, supersededTimeline.StatusCode);
        Assert.Contains(supersededTimelineBody!.Items, item =>
            item.DocumentId == supersededDocument.Id
            && item.EventType == "DOCUMENT_SUPERSEDED"
            && item.RelatedDocumentId == storedDocument.Id
            && !item.IsCurrentDocument
            && item.IsSuperseded);
        Assert.Equal(HttpStatusCode.OK, entityTimeline.StatusCode);
        Assert.Contains(entityTimelineBody!.Items, item =>
            item.DocumentId == storedDocument.Id
            && item.EventType == "DOCUMENT_REPLACED"
            && item.RelatedDocumentId == supersededDocument.Id);
        Assert.Contains(entityTimelineBody.Items, item =>
            item.DocumentId == supersededDocument.Id
            && item.EventType == "DOCUMENT_SUPERSEDED"
            && item.RelatedDocumentId == storedDocument.Id);
    }

    [Fact]
    public async Task Document_catalog_filters_by_integrity_state()
    {
        var documentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "evidencia-faltante.pdf",
            "application/pdf",
            "missing file regression",
            createPhysicalFile: false);
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var response = await SendGetAsync("/api/documents?integrityState=MISSING_FILE&take=20", token);
        var body = await response.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(body!.Items, item => item.Id == documentId && item.IntegrityState == "MISSING_FILE");
    }

    [Fact]
    public async Task Document_catalog_does_not_expose_unmapped_document_modules()
    {
        var documentId = await SeedStoredDocumentAsync(
            "PRIVATE",
            DocumentAreaCodes.MarketsTenantCertificates,
            "PRIVATE_DOCUMENT",
            "privado.pdf",
            "application/pdf",
            "private regression");
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var list = await SendGetAsync("/api/documents?take=200", token);
        var body = await list.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var detail = await SendGetAsync($"/api/documents/{documentId}", token);

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.DoesNotContain(body!.Items, item => item.Id == documentId);
        Assert.Equal(HttpStatusCode.Forbidden, detail.StatusCode);
    }

    [Fact]
    public async Task Admin_can_archive_restore_filter_and_download_archived_documents()
    {
        var documentId = await SeedStoredDocumentAsync(
            "FEDERATION",
            DocumentAreaCodes.FederationApplicationEvidences,
            "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            "evidencia-ciclo.pdf",
            "application/pdf",
            "document lifecycle regression");
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var archive = await SendPostAsync(
            $"/api/documents/{documentId}/archive",
            token,
            new { reason = "Lifecycle regression" });
        var archivedDetail = await archive.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();

        using var defaultList = await SendGetAsync("/api/documents?take=200", token);
        var defaultBody = await defaultList.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var archivedList = await SendGetAsync("/api/documents?statusCode=ARCHIVED&take=200", token);
        var archivedBody = await archivedList.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var includeArchivedList = await SendGetAsync("/api/documents?includeArchived=true&take=200", token);
        var includeArchivedBody = await includeArchivedList.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();
        using var archivedDownload = await SendGetAsync($"/api/documents/{documentId}/download", token);

        using var restore = await SendPostAsync($"/api/documents/{documentId}/restore", token, new { });
        var restoredDetail = await restore.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();
        using var activeList = await SendGetAsync("/api/documents?statusCode=ACTIVE&take=200", token);
        var activeBody = await activeList.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var actionTypes = dbContext.AuditEvents
            .Where(item => item.EntityId == documentId.ToString())
            .Select(item => item.ActionType)
            .ToArray();

        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
        Assert.Equal("ARCHIVED", archivedDetail!.StatusCode);
        Assert.NotNull(archivedDetail.ArchivedUtc);
        Assert.Equal("Lifecycle regression", archivedDetail.ArchiveReason);

        Assert.Equal(HttpStatusCode.OK, defaultList.StatusCode);
        Assert.DoesNotContain(defaultBody!.Items, item => item.Id == documentId);
        Assert.Contains(archivedBody!.Items, item => item.Id == documentId && item.StatusCode == "ARCHIVED");
        Assert.Contains(includeArchivedBody!.Items, item => item.Id == documentId && item.StatusCode == "ARCHIVED");

        Assert.Equal(HttpStatusCode.OK, archivedDownload.StatusCode);
        Assert.Equal("document lifecycle regression", await archivedDownload.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        Assert.Equal("ACTIVE", restoredDetail!.StatusCode);
        Assert.Null(restoredDetail.ArchivedUtc);
        Assert.Null(restoredDetail.ArchiveReason);
        Assert.Contains(activeBody!.Items, item => item.Id == documentId && item.StatusCode == "ACTIVE");
        Assert.Contains("DOCUMENT_ARCHIVED", actionTypes);
        Assert.Contains("DOCUMENT_RESTORED", actionTypes);
    }

    [Fact]
    public async Task Admin_can_update_metadata_and_primary_marker_demotes_previous_active_primary()
    {
        var entityId = Guid.NewGuid();
        var firstDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "cedula-principal-previa.pdf",
            "application/pdf",
            "previous primary regression",
            entityId: entityId,
            documentClassCode: DocumentClassCodes.Certificate,
            businessPurpose: "Acreditar documento previo.",
            isPrimaryDocument: true);
        var secondDocumentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "cedula-principal-nueva.pdf",
            "application/pdf",
            "new primary regression",
            entityId: entityId,
            documentClassCode: DocumentClassCodes.Other);
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.AdminUserName, AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var update = await SendPatchAsync(
            $"/api/documents/{secondDocumentId}/metadata",
            token,
            new
            {
                documentClassCode = DocumentClassCodes.SignedDocument,
                businessPurpose = "Actualizar metadata desde catalogo transversal.",
                isPrimaryDocument = true,
                classificationNotes = "Metadata regression"
            });
        var detail = await update.Content.ReadFromJsonAsync<DocumentCatalogDetailTestResponse>();
        using var filtered = await SendGetAsync($"/api/documents?documentClassCode={DocumentClassCodes.SignedDocument}&take=200", token);
        var filteredBody = await filtered.Content.ReadFromJsonAsync<DocumentCatalogListTestResponse>();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var firstDocument = dbContext.StoredDocuments.Single(item => item.Id == firstDocumentId);
        var secondDocument = dbContext.StoredDocuments.Single(item => item.Id == secondDocumentId);
        var actionTypes = dbContext.AuditEvents
            .Where(item => item.EntityId == secondDocumentId.ToString())
            .Select(item => item.ActionType)
            .ToArray();

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(DocumentClassCodes.SignedDocument, detail!.DocumentClassCode);
        Assert.Equal("Actualizar metadata desde catalogo transversal.", detail.BusinessPurpose);
        Assert.Equal("Metadata regression", detail.ClassificationNotes);
        Assert.True(detail.IsPrimaryDocument);
        Assert.False(firstDocument.IsPrimaryDocument);
        Assert.True(secondDocument.IsPrimaryDocument);
        Assert.Contains(filteredBody!.Items, item => item.Id == secondDocumentId && item.DocumentClassCode == DocumentClassCodes.SignedDocument);
        Assert.Contains("DOCUMENT_METADATA_UPDATED", actionTypes);
        Assert.Contains("DOCUMENT_PRIMARY_CHANGED", actionTypes);
    }

    [Fact]
    public async Task Non_admin_cannot_update_document_metadata()
    {
        var documentId = await SeedStoredDocumentAsync(
            "DONATARIAS",
            DocumentAreaCodes.DonationsApplicationEvidences,
            "DONATION_APPLICATION_EVIDENCE",
            "metadata-non-admin.pdf",
            "application/pdf",
            "metadata forbidden regression");
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.OperatorUserName, AuthorizationRegressionWebApplicationFactory.OperatorPassword);

        using var update = await SendPatchAsync(
            $"/api/documents/{documentId}/metadata",
            token,
            new
            {
                documentClassCode = DocumentClassCodes.SupportingDocument,
                businessPurpose = "Operator attempt",
                isPrimaryDocument = false,
                classificationNotes = "forbidden"
            });

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
    }

    [Fact]
    public async Task Non_admin_cannot_archive_or_restore_documents()
    {
        var documentId = await SeedStoredDocumentAsync(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            "cedula-non-admin.pdf",
            "application/pdf",
            "document lifecycle forbidden regression");
        var token = await LoginAsync(AuthorizationRegressionWebApplicationFactory.OperatorUserName, AuthorizationRegressionWebApplicationFactory.OperatorPassword);

        using var archive = await SendPostAsync(
            $"/api/documents/{documentId}/archive",
            token,
            new { reason = "operator attempt" });
        using var restore = await SendPostAsync($"/api/documents/{documentId}/restore", token, new { });

        Assert.Equal(HttpStatusCode.Forbidden, archive.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, restore.StatusCode);
    }

    private async Task<Guid> SeedStoredDocumentAsync(
        string moduleCode,
        string documentAreaCode,
        string entityType,
        string originalFileName,
        string contentType,
        string content,
        bool createPhysicalFile = true,
        Guid? entityId = null,
        string? documentClassCode = null,
        string? businessPurpose = null,
        bool isPrimaryDocument = false,
        string? retentionPolicyCode = null,
        DateTimeOffset? retentionUntilUtc = null)
    {
        var resolvedEntityId = entityId ?? Guid.NewGuid();
        var fileContent = System.Text.Encoding.UTF8.GetBytes(content);
        var relativePath = $"document-catalog/{Guid.NewGuid():N}{Path.GetExtension(originalFileName).ToLowerInvariant()}";
        if (createPhysicalFile)
        {
            var absolutePath = Path.Combine(
                ResolveStorageRoot(documentAreaCode),
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
            await File.WriteAllBytesAsync(absolutePath, fileContent);
        }

        var document = new StoredDocument(
            moduleCode,
            documentAreaCode,
            entityType,
            resolvedEntityId,
            originalFileName,
            relativePath,
            contentType,
            fileContent.Length,
            DateTimeOffset.UtcNow,
            Convert.ToHexString(SHA256.HashData(fileContent)),
            isLegacyBackfill: false,
            documentClassCode,
            businessPurpose,
            isPrimaryDocument,
            classificationNotes: null,
            retentionPolicyCode,
            retentionUntilUtc);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        dbContext.StoredDocuments.Add(document);
        await dbContext.SaveChangesAsync();

        return document.Id;
    }

    private async Task ArchiveStoredDocumentForTestAsync(Guid documentId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var document = dbContext.StoredDocuments.Single(item => item.Id == documentId);
        document.Archive("Summary regression archive", DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync();
    }

    private async Task SetAdministrativeHoldForTestAsync(Guid documentId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var document = dbContext.StoredDocuments.Single(item => item.Id == documentId);
        document.SetAdministrativeHold("Operational status precedence regression.", DateTimeOffset.UtcNow, "test");
        await dbContext.SaveChangesAsync();
    }

    private async Task MarkStoredDocumentSupersededForTestAsync(Guid documentId, Guid supersedingDocumentId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var document = dbContext.StoredDocuments.Single(item => item.Id == documentId);
        document.MarkSupersededBy(supersedingDocumentId, DateTimeOffset.UtcNow, "Operational status precedence regression.");
        await dbContext.SaveChangesAsync();
    }

    private async Task<MarketTenantSeedResult> SeedMarketTenantAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var status = new ModuleStatusCatalogEntry(
            "MARKETS",
            "Mercados",
            "MARKET",
            "Mercado",
            "ACTIVE",
            "Activo",
            description: null,
            sortOrder: 1,
            isClosed: false,
            alertsEnabledByDefault: true);
        dbContext.ModuleStatusCatalogEntries.Add(status);
        await dbContext.SaveChangesAsync();

        var market = new Market(
            "Mercado contextual",
            "Cuauhtemoc",
            status.Id,
            secretaryGeneralContactId: null,
            "Secretario contextual",
            notes: null);
        dbContext.Markets.Add(market);
        await dbContext.SaveChangesAsync();

        var tenant = new MarketTenant(
            market.Id,
            contactId: null,
            "Locatario contextual",
            "CTX-001",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            "Alimentos",
            mobilePhone: null,
            whatsAppPhone: null,
            email: null,
            notes: null,
            "cedula-contextual.pdf",
            "document-catalog/cedula-contextual.pdf",
            "application/pdf",
            42,
            DateTimeOffset.UtcNow);
        dbContext.MarketTenants.Add(tenant);
        await dbContext.SaveChangesAsync();

        return new MarketTenantSeedResult(market.Id, tenant.Id);
    }

    private async Task<DonationEvidenceSeedResult> SeedDonationApplicationEvidenceAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var status = new ModuleStatusCatalogEntry(
            "DONATARIAS",
            "Donatarias",
            "DONATION_APPLICATION",
            "Aplicacion de donacion",
            "ACTIVE",
            "Activo",
            description: null,
            sortOrder: 1,
            isClosed: false,
            alertsEnabledByDefault: true);
        var evidenceType = new EvidenceType("CTX_DON", "Contexto donatarias");
        dbContext.ModuleStatusCatalogEntries.Add(status);
        dbContext.EvidenceTypes.Add(evidenceType);
        await dbContext.SaveChangesAsync();

        var donation = new Donation(
            "Donante contextual",
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Especie",
            1000,
            "DON-CTX",
            notes: null,
            status.Id);
        dbContext.Donations.Add(donation);
        await dbContext.SaveChangesAsync();

        var application = new DonationApplication(
            donation.Id,
            "Beneficiario contextual",
            responsibleContactId: null,
            "Responsable contextual",
            DateOnly.FromDateTime(DateTime.UtcNow),
            250,
            status.Id,
            verificationDetails: null,
            closingDetails: null);
        dbContext.DonationApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var evidence = new DonationApplicationEvidence(
            application.Id,
            evidenceType.Id,
            "Evidencia contextual",
            "evidencia-donacion-contextual.pdf",
            "document-catalog/evidencia-donacion-contextual.pdf",
            "application/pdf",
            42,
            DateTimeOffset.UtcNow);
        dbContext.DonationApplicationEvidences.Add(evidence);
        await dbContext.SaveChangesAsync();

        return new DonationEvidenceSeedResult(application.Id, evidence.Id);
    }

    private async Task<FederationEvidenceSeedResult> SeedFederationApplicationEvidenceAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var status = new ModuleStatusCatalogEntry(
            "FEDERATION",
            "Federacion",
            "FEDERATION_DONATION_APPLICATION",
            "Aplicacion de Federacion",
            "ACTIVE",
            "Activo",
            description: null,
            sortOrder: 1,
            isClosed: false,
            alertsEnabledByDefault: true);
        var evidenceType = new EvidenceType("CTX_FED", "Contexto federacion");
        dbContext.ModuleStatusCatalogEntries.Add(status);
        dbContext.EvidenceTypes.Add(evidenceType);
        await dbContext.SaveChangesAsync();

        var donation = new FederationDonation(
            "Donante federacion contextual",
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Efectivo",
            1000,
            "FED-CTX",
            notes: null,
            status.Id);
        dbContext.FederationDonations.Add(donation);
        await dbContext.SaveChangesAsync();

        var application = new FederationDonationApplication(
            donation.Id,
            "Destino contextual",
            DateOnly.FromDateTime(DateTime.UtcNow),
            300,
            status.Id,
            verificationDetails: null,
            closingDetails: null);
        dbContext.FederationDonationApplications.Add(application);
        await dbContext.SaveChangesAsync();

        var evidence = new FederationDonationApplicationEvidence(
            application.Id,
            evidenceType.Id,
            "Evidencia federacion contextual",
            "evidencia-federacion-contextual.pdf",
            "document-catalog/evidencia-federacion-contextual.pdf",
            "application/pdf",
            42,
            DateTimeOffset.UtcNow);
        dbContext.FederationDonationApplicationEvidences.Add(evidence);
        await dbContext.SaveChangesAsync();

        return new FederationEvidenceSeedResult(application.Id, evidence.Id);
    }

    private async Task<Guid> SeedOpenMarketAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var status = new ModuleStatusCatalogEntry(
            "MARKETS",
            "Mercados",
            "MARKET",
            "Mercado",
            "ACTIVE",
            "Activo",
            description: null,
            sortOrder: 1,
            isClosed: false,
            alertsEnabledByDefault: true);
        dbContext.ModuleStatusCatalogEntries.Add(status);
        await dbContext.SaveChangesAsync();

        var market = new Market(
            "Mercado clasificacion",
            "Cuauhtemoc",
            status.Id,
            secretaryGeneralContactId: null,
            "Secretario general",
            notes: null);
        dbContext.Markets.Add(market);
        await dbContext.SaveChangesAsync();

        return market.Id;
    }

    private static MultipartFormDataContent BuildCertificateUploadContent(string fileName)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("%PDF-1.4\ncontextual remediation regression\n%%EOF\n"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "certificateFile", fileName);
        return content;
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

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(login?.AccessToken));

        return login.AccessToken;
    }

    private async Task<HttpResponseMessage> SendGetAsync(string path, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendPostAsync(string path, string accessToken, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendPatchAsync(string path, string accessToken, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendDeleteAsync(string path, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private static void AssertCsvDownloadHeaders(HttpResponseMessage response, string fileNamePrefix)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("utf-8", response.Content.Headers.ContentType?.CharSet ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(fileNamePrefix, response.Content.Headers.ContentDisposition?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("attachment", response.Content.Headers.ContentDisposition?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveStorageRoot(string documentAreaCode)
    {
        var root = documentAreaCode switch
        {
            DocumentAreaCodes.MarketsTenantCertificates => Path.Combine(Path.GetTempPath(), "fmcpa-authz-regression", "markets"),
            DocumentAreaCodes.DonationsApplicationEvidences => Path.Combine(Path.GetTempPath(), "fmcpa-authz-regression", "donations"),
            DocumentAreaCodes.FederationApplicationEvidences => Path.Combine(Path.GetTempPath(), "fmcpa-authz-regression", "federation"),
            _ => throw new ArgumentOutOfRangeException(nameof(documentAreaCode), documentAreaCode, "Unsupported document area.")
        };

        return Path.GetFullPath(root);
    }

    private sealed record LoginResponse(string AccessToken);

    private sealed record MarketTenantUploadTestResponse(Guid Id);

    private sealed record MarketTenantSeedResult(Guid MarketId, Guid TenantId);

    private sealed record DonationEvidenceSeedResult(Guid ApplicationId, Guid EvidenceId);

    private sealed record FederationEvidenceSeedResult(Guid ApplicationId, Guid EvidenceId);

    private sealed record DocumentCompletenessListTestResponse(
        int TotalCount,
        int ReturnedCount,
        int Skip,
        int Take,
        IReadOnlyList<DocumentCompletenessTestResponse> Items);

    private sealed record DocumentRuleListTestResponse(
        int TotalCount,
        IReadOnlyList<DocumentRuleTestResponse> Items);

    private sealed record DocumentRuleTestResponse(
        string RuleCode,
        string ModuleCode,
        string EntityType,
        IReadOnlyList<string> RequiredDocumentClassCodes,
        int MinimumRequiredCount);

    private sealed record DocumentRequirementTestResponse(
        string ModuleCode,
        string EntityType,
        Guid EntityId,
        DocumentOriginContextTestResponse OriginContext,
        bool AppliesRule,
        string? RuleCode,
        string StatusCode,
        bool? IsComplete,
        IReadOnlyList<string> RequiredDocumentClassCodes,
        int MinimumRequiredCount,
        int CurrentDocumentCount,
        string? MissingReasonCode,
        string? RemediationHint,
        IReadOnlyList<Guid> CoveringDocumentIds);

    private sealed record DocumentCompletenessTestResponse(
        string ModuleCode,
        string EntityType,
        Guid EntityId,
        DocumentOriginContextTestResponse OriginContext,
        string RuleCode,
        string StatusCode,
        bool IsComplete,
        string RequiredDocumentCode,
        IReadOnlyList<string> RequiredDocumentClassCodes,
        int MinimumRequiredCount,
        string? MissingReasonCode,
        string RemediationHint,
        int RelatedDocumentCount,
        IReadOnlyList<Guid> CoveringDocumentIds);

    private sealed record DocumentCatalogListTestResponse(
        int TotalCount,
        int ReturnedCount,
        int Skip,
        int Take,
        IReadOnlyList<DocumentCatalogItemTestResponse> Items);

    private sealed record DocumentWorkQueueTestResponse(
        int TotalCount,
        int ReturnedCount,
        int Skip,
        int Take,
        IReadOnlyList<DocumentWorkQueueItemTestResponse> Items);

    private sealed record DocumentWorkQueueItemTestResponse(
        string WorkItemKey,
        string WorkItemType,
        string SeverityCode,
        string ModuleCode,
        string ModuleName,
        string EntityType,
        Guid EntityId,
        DocumentOriginContextTestResponse OriginContext,
        Guid? DocumentId,
        string? DocumentOriginalFileName,
        string Title,
        string Summary,
        string ReasonCode,
        string CurrentStatusCode,
        string? DocumentOperationalStatusCode,
        string? DocumentOperationalSeverityCode,
        string RouteHint,
        string? DocumentDetailUrl,
        string? RemediationHint);

    private sealed record DocumentSummaryTestResponse(
        int TotalDocuments,
        int ActiveDocuments,
        int ArchivedDocuments,
        int IntegrityIssuesCount,
        int IncompleteEntitiesCount,
        int ReviewDueCount,
        int ExpiredRetentionCount,
        int AdministrativeHoldCount,
        IReadOnlyList<DocumentSummaryModuleTestResponse> Modules,
        IReadOnlyList<DocumentSummaryClassTestResponse> DocumentClasses,
        IReadOnlyList<DocumentSummaryOperationalStatusTestResponse> OperationalStatuses,
        IReadOnlyList<DocumentSummaryWorkQueueCategoryTestResponse> WorkQueueCategories);

    private sealed record DocumentSummaryModuleTestResponse(
        string ModuleCode,
        string ModuleName,
        int TotalDocuments,
        int ActiveDocuments,
        int ArchivedDocuments,
        int IntegrityIssuesCount,
        int IncompleteEntitiesCount,
        int ReviewDueCount,
        int ExpiredRetentionCount,
        int AdministrativeHoldCount);

    private sealed record DocumentSummaryClassTestResponse(
        string DocumentClassCode,
        int TotalDocuments,
        int ActiveDocuments,
        int ArchivedDocuments);

    private sealed record DocumentSummaryOperationalStatusTestResponse(
        string DocumentOperationalStatusCode,
        string DocumentOperationalSeverityCode,
        int TotalCount);

    private sealed record DocumentSummaryWorkQueueCategoryTestResponse(
        string WorkItemType,
        string ReasonCode,
        string SeverityCode,
        int TotalCount);

    private sealed record DocumentCatalogItemTestResponse(
        Guid Id,
        DocumentOriginContextTestResponse OriginContext,
        string DocumentClassCode,
        string RetentionPolicyCode,
        DateTimeOffset RetentionUntilUtc,
        string RetentionStatusCode,
        string RetentionBaselinePolicyCode,
        DateTimeOffset RetentionBaselineUntilUtc,
        string RetentionEffectivePolicyCode,
        DateTimeOffset RetentionEffectiveUntilUtc,
        bool HasRetentionOverride,
        string? RetentionOverridePolicyCode,
        DateTimeOffset? RetentionOverrideUntilUtc,
        string? RetentionOverrideReason,
        string RetentionReviewStatusCode,
        DateTimeOffset? LastRetentionReviewUtc,
        DateTimeOffset? NextRetentionReviewUtc,
        string? RetentionReviewNotes,
        bool IsAdministrativeHold,
        string? HoldReason,
        DateTimeOffset? HoldPlacedUtc,
        DateTimeOffset? HoldReleasedUtc,
        string? HoldPlacedBy,
        string DocumentOperationalStatusCode,
        string DocumentOperationalSeverityCode,
        bool IsPrimaryDocument,
        string StatusCode,
        bool IsSuperseded,
        Guid? SupersededByDocumentId,
        Guid? ReplacedDocumentId,
        string ReplacementGroupKey,
        string IntegrityState,
        string DownloadUrl);

    private sealed record DocumentCatalogDetailTestResponse(
        Guid Id,
        DocumentOriginContextTestResponse OriginContext,
        string DocumentClassCode,
        string? BusinessPurpose,
        bool IsPrimaryDocument,
        string? ClassificationNotes,
        string RetentionPolicyCode,
        DateTimeOffset RetentionUntilUtc,
        string RetentionStatusCode,
        string RetentionBaselinePolicyCode,
        DateTimeOffset RetentionBaselineUntilUtc,
        string RetentionEffectivePolicyCode,
        DateTimeOffset RetentionEffectiveUntilUtc,
        bool HasRetentionOverride,
        string? RetentionOverridePolicyCode,
        DateTimeOffset? RetentionOverrideUntilUtc,
        string? RetentionOverrideReason,
        string RetentionReviewStatusCode,
        DateTimeOffset? LastRetentionReviewUtc,
        DateTimeOffset? NextRetentionReviewUtc,
        string? RetentionReviewNotes,
        bool IsAdministrativeHold,
        string? HoldReason,
        DateTimeOffset? HoldPlacedUtc,
        DateTimeOffset? HoldReleasedUtc,
        string? HoldPlacedBy,
        string DocumentOperationalStatusCode,
        string DocumentOperationalSeverityCode,
        string StatusCode,
        DateTimeOffset? ArchivedUtc,
        string? ArchiveReason,
        bool IsSuperseded,
        Guid? SupersededByDocumentId,
        Guid? ReplacedDocumentId,
        string ReplacementGroupKey,
        string IntegrityState);

    private sealed record DocumentTimelineTestResponse(
        int TotalCount,
        IReadOnlyList<DocumentTimelineEventTestResponse> Items);

    private sealed record DocumentTimelineEventTestResponse(
        Guid? AuditEventId,
        Guid DocumentId,
        string DocumentOriginalFileName,
        string ModuleCode,
        string ModuleName,
        string DocumentAreaCode,
        string EntityType,
        Guid EntityId,
        DateTimeOffset OccurredUtc,
        string EventType,
        string Title,
        string Detail,
        Guid? RelatedDocumentId,
        string? RelatedDocumentOriginalFileName,
        string DocumentStatusCode,
        bool IsCurrentDocument,
        bool IsSuperseded);

    private sealed record DocumentOriginContextTestResponse(
        string ModuleCode,
        string ModuleName,
        string EntityType,
        Guid EntityId,
        string DisplayName,
        string? Summary,
        string? RouteHint);
}
