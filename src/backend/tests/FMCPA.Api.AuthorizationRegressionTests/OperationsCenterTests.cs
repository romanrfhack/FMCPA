using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Domain.Entities.Audit;
using FMCPA.Domain.Entities.Documents;
using FMCPA.Domain.Entities.Markets;
using FMCPA.Domain.Entities.Security;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FMCPA.Api.AuthorizationRegressionTests;

public sealed class OperationsCenterTests : IClassFixture<AuthorizationRegressionWebApplicationFactory>
{
    private readonly AuthorizationRegressionWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OperationsCenterTests(AuthorizationRegressionWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Operations_summary_and_work_queue_consolidate_business_documents_and_security_for_admin()
    {
        var signals = await SeedOperationsSignalsAsync();
        var adminToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.AdminUserName,
            AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var summaryResponse = await SendGetAsync("/api/operations/summary", adminToken);
        using var queueResponse = await SendGetAsync("/api/operations/work-queue?take=200", adminToken);
        var summary = await summaryResponse.Content.ReadFromJsonAsync<OperationsSummaryTestResponse>();
        var queue = await queueResponse.Content.ReadFromJsonAsync<OperationsWorkQueueTestResponse>();

        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        Assert.NotNull(summary);
        Assert.Contains(summary!.BusinessKpis, item => item.ModuleCode == "MARKETS" && item.KpiCode == "MARKETS_ACTIVE");
        Assert.NotNull(summary.Documents);
        Assert.True(summary.Documents!.IntegrityIssuesCount >= 1);
        Assert.Contains(summary.Documents.Modules, item => item.ModuleCode == "MARKETS");
        Assert.DoesNotContain(summary.Documents.Modules, item => item.ModuleCode == "PRIVATE");
        Assert.NotNull(summary.Security);
        Assert.True(summary.Security!.ActiveLockedUserCount >= 1);
        Assert.True(summary.Security.LoginFailedCount >= 1);

        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.NotNull(queue);
        Assert.Contains(queue!.SeverityConvention, item => item.SeverityCode == "HIGH" && item.SortOrder == 0);
        Assert.Contains(queue.Items, item =>
            item.CategoryCode == "BUSINESS"
            && item.ModuleCode == "MARKETS"
            && item.EntityType == "MARKET_TENANT"
            && item.EntityId == signals.MarketTenantId.ToString()
            && item.RouteHint == "/markets"
            && item.ActionKind == "REMEDIATE"
            && item.ActionLabel == "Ir a remediar");
        Assert.Contains(queue.Items, item =>
            item.CategoryCode == "DOCUMENTS"
            && item.ModuleCode == "MARKETS"
            && item.DocumentId == signals.MarketDocumentId
            && item.SeverityCode == "HIGH"
            && item.RouteHint == "/documents/work-queue"
            && item.ActionKind == "DOCUMENTS_QUEUE"
            && item.ActionLabel == "Abrir bandeja documental");
        var lockedUserItem = Assert.Single(queue.Items, item =>
            item.CategoryCode == "SECURITY"
            && item.TypeCode == "LOCKED_USER"
            && item.ModuleCode == "SECURITY"
            && item.EntityId == signals.LockedUserId.ToString());
        Assert.Equal("/admin/security", lockedUserItem.RouteHint);
        Assert.Equal("SECURITY_ADMIN", lockedUserItem.ActionKind);
        Assert.Equal("Abrir seguridad", lockedUserItem.ActionLabel);
        Assert.Equal("UNLOCK_USER", lockedUserItem.QuickActionCode);
        Assert.Equal("Desbloquear", lockedUserItem.QuickActionLabel);
        Assert.DoesNotContain(queue.Items, item => item.ModuleCode == "PRIVATE" || item.DocumentId == signals.PrivateDocumentId);

        using var unlockResponse = await SendPostAsync($"/api/admin/users/{signals.LockedUserId}/unlock", adminToken);
        using var securityQueueAfterUnlockResponse = await SendGetAsync("/api/operations/work-queue?categoryCode=SECURITY&take=200", adminToken);
        var securityQueueAfterUnlock = await securityQueueAfterUnlockResponse.Content.ReadFromJsonAsync<OperationsWorkQueueTestResponse>();

        Assert.Equal(HttpStatusCode.OK, unlockResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, securityQueueAfterUnlockResponse.StatusCode);
        Assert.NotNull(securityQueueAfterUnlock);
        Assert.DoesNotContain(securityQueueAfterUnlock!.Items, item => item.WorkItemKey == lockedUserItem.WorkItemKey);
    }

    [Fact]
    public async Task Operations_center_does_not_expose_security_surface_to_non_admin_roles()
    {
        await SeedOperationsSignalsAsync();
        var readOnlyToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var summaryResponse = await SendGetAsync("/api/operations/summary", readOnlyToken);
        using var queueResponse = await SendGetAsync("/api/operations/work-queue?categoryCode=SECURITY&take=20", readOnlyToken);
        var summary = await summaryResponse.Content.ReadFromJsonAsync<OperationsSummaryTestResponse>();
        var queue = await queueResponse.Content.ReadFromJsonAsync<OperationsWorkQueueTestResponse>();

        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        Assert.NotNull(summary);
        Assert.Null(summary!.Security);
        Assert.NotNull(summary.Documents);
        Assert.DoesNotContain(summary.Documents!.Modules, item => item.ModuleCode == "PRIVATE");

        Assert.Equal(HttpStatusCode.OK, queueResponse.StatusCode);
        Assert.NotNull(queue);
        Assert.Empty(queue!.Items);
    }

    [Fact]
    public async Task Operations_time_windows_filter_summary_and_work_queue_by_relevant_dates()
    {
        var signals = await SeedOperationsSignalsAsync();
        var adminToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.AdminUserName,
            AuthorizationRegressionWebApplicationFactory.AdminPassword);

        using var lastSevenDaysSummaryResponse = await SendGetAsync("/api/operations/summary?timeWindowCode=LAST_7_DAYS", adminToken);
        using var nextThirtyDaysSummaryResponse = await SendGetAsync("/api/operations/summary?timeWindowCode=NEXT_30_DAYS", adminToken);
        using var lastSevenDaysQueueResponse = await SendGetAsync("/api/operations/work-queue?timeWindowCode=LAST_7_DAYS&take=200", adminToken);
        using var nextThirtyDaysQueueResponse = await SendGetAsync("/api/operations/work-queue?timeWindowCode=NEXT_30_DAYS&take=200", adminToken);
        var lastSevenDaysSummary = await lastSevenDaysSummaryResponse.Content.ReadFromJsonAsync<OperationsSummaryTestResponse>();
        var nextThirtyDaysSummary = await nextThirtyDaysSummaryResponse.Content.ReadFromJsonAsync<OperationsSummaryTestResponse>();
        var lastSevenDaysQueue = await lastSevenDaysQueueResponse.Content.ReadFromJsonAsync<OperationsWorkQueueTestResponse>();
        var nextThirtyDaysQueue = await nextThirtyDaysQueueResponse.Content.ReadFromJsonAsync<OperationsWorkQueueTestResponse>();

        Assert.Equal(HttpStatusCode.OK, lastSevenDaysSummaryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, nextThirtyDaysSummaryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, lastSevenDaysQueueResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, nextThirtyDaysQueueResponse.StatusCode);
        Assert.NotNull(lastSevenDaysSummary);
        Assert.NotNull(nextThirtyDaysSummary);
        Assert.Equal("LAST_7_DAYS", lastSevenDaysSummary!.TimeWindow.TimeWindowCode);
        Assert.True(lastSevenDaysSummary.TimeWindow.IsApplied);
        Assert.Equal("NEXT_30_DAYS", nextThirtyDaysSummary!.TimeWindow.TimeWindowCode);
        Assert.True(nextThirtyDaysSummary.TimeWindow.IsApplied);

        Assert.NotNull(lastSevenDaysQueue);
        Assert.Contains(lastSevenDaysQueue!.Items, item =>
            item.CategoryCode == "BUSINESS"
            && item.EntityId == signals.MarketTenantId.ToString()
            && item.RelevantUtc is not null);
        Assert.DoesNotContain(lastSevenDaysQueue.Items, item => item.DocumentId == signals.RetentionReviewDocumentId);

        Assert.NotNull(nextThirtyDaysQueue);
        Assert.Contains(nextThirtyDaysQueue!.Items, item =>
            item.CategoryCode == "DOCUMENTS"
            && item.DocumentId == signals.RetentionReviewDocumentId
            && item.RelevantUtc is not null);
        Assert.DoesNotContain(nextThirtyDaysQueue.Items, item =>
            item.CategoryCode == "BUSINESS"
            && item.EntityId == signals.MarketTenantId.ToString());

        Assert.NotNull(nextThirtyDaysSummary.Documents);
        Assert.True(nextThirtyDaysSummary.Documents!.ReviewDueCount >= 1);
        Assert.Contains(nextThirtyDaysSummary.Documents.WorkQueueCategories, item =>
            item.WorkItemType == "RETENTION_REVIEW"
            && item.ReasonCode == DocumentRetentionPolicyCodes.ReviewDue);
    }

    [Fact]
    public async Task Operations_light_exports_cover_summary_and_work_queue_with_safe_csv_headers()
    {
        var signals = await SeedOperationsSignalsAsync();
        var adminToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.AdminUserName,
            AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var readOnlyToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
            AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword);

        using var summaryExport = await SendGetAsync("/api/operations/summary/export?timeWindowCode=NEXT_30_DAYS", adminToken);
        using var workQueueExport = await SendGetAsync("/api/operations/work-queue/export?timeWindowCode=NEXT_30_DAYS&categoryCode=DOCUMENTS&take=200", adminToken);
        using var readOnlySecurityExport = await SendGetAsync("/api/operations/work-queue/export?categoryCode=SECURITY&take=200", readOnlyToken);
        var summaryCsv = await summaryExport.Content.ReadAsStringAsync();
        var workQueueCsv = await workQueueExport.Content.ReadAsStringAsync();
        var readOnlySecurityCsv = await readOnlySecurityExport.Content.ReadAsStringAsync();

        AssertCsvDownloadHeaders(summaryExport, "operations-summary");
        Assert.Contains("timeWindowCode,sectionCode,metricCode,categoryCode", summaryCsv);
        Assert.Contains("NEXT_30_DAYS", summaryCsv);
        Assert.Contains("DOCUMENTS_REVIEW_DUE", summaryCsv);
        Assert.Contains("SECURITY_ACTIVE_LOCKED_USERS", summaryCsv);
        Assert.DoesNotContain(signals.PrivateDocumentId.ToString(), summaryCsv);

        AssertCsvDownloadHeaders(workQueueExport, "operations-work-queue");
        Assert.Contains("timeWindowCode,workItemKey,categoryCode,workItemType", workQueueCsv);
        Assert.Contains("NEXT_30_DAYS", workQueueCsv);
        Assert.Contains(signals.RetentionReviewDocumentId.ToString(), workQueueCsv);
        Assert.Contains("/documents/review", workQueueCsv);
        Assert.DoesNotContain(signals.PrivateDocumentId.ToString(), workQueueCsv);

        AssertCsvDownloadHeaders(readOnlySecurityExport, "operations-work-queue");
        Assert.DoesNotContain(signals.LockedUserId.ToString(), readOnlySecurityCsv);
        Assert.DoesNotContain("LOCKED_USER", readOnlySecurityCsv);
    }

    private async Task<OperationsSignalsSeedResult> SeedOperationsSignalsAsync()
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
            "Mercado operaciones",
            "Cuauhtemoc",
            status.Id,
            secretaryGeneralContactId: null,
            "Secretario operaciones",
            notes: null);
        dbContext.Markets.Add(market);
        await dbContext.SaveChangesAsync();

        var tenant = new MarketTenant(
            market.Id,
            contactId: null,
            "Locatario operaciones",
            "OPS-001",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            "Alimentos",
            mobilePhone: null,
            whatsAppPhone: null,
            email: null,
            notes: null,
            "cedula-operaciones.pdf",
            "operations/cedula-operaciones.pdf",
            "application/pdf",
            42,
            DateTimeOffset.UtcNow);
        dbContext.MarketTenants.Add(tenant);

        var marketDocument = new StoredDocument(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            tenant.Id,
            "operations-missing.pdf",
            $"operations/{Guid.NewGuid():N}.pdf",
            "application/pdf",
            24,
            DateTimeOffset.UtcNow.AddDays(-1),
            null,
            false,
            DocumentClassCodes.Certificate,
            "Operations center regression",
            true);
        var privateDocument = new StoredDocument(
            "PRIVATE",
            DocumentAreaCodes.MarketsTenantCertificates,
            "PRIVATE_DOCUMENT",
            Guid.NewGuid(),
            "operations-private.pdf",
            $"operations/{Guid.NewGuid():N}.pdf",
            "application/pdf",
            24,
            DateTimeOffset.UtcNow.AddDays(-1),
            null,
            false);
        var retentionReviewDocument = new StoredDocument(
            "FEDERATION",
            DocumentAreaCodes.FederationApplicationEvidences,
            "FEDERATION_DONATION_APPLICATION_EVIDENCE",
            Guid.NewGuid(),
            "operations-retention-review.pdf",
            $"operations/{Guid.NewGuid():N}.pdf",
            "application/pdf",
            24,
            DateTimeOffset.UtcNow.AddDays(-1),
            null,
            false,
            DocumentClassCodes.SupportingDocument,
            "Operations center time window regression",
            false,
            retentionUntilUtc: DateTimeOffset.UtcNow.AddDays(10));
        var lockedUser = new ApplicationUser(
            $"opslock{Guid.NewGuid():N}"[..24],
            "Operations locked",
            "test-hash",
            ApplicationRoleCodes.ReadOnly);
        lockedUser.RecordFailedLogin(DateTimeOffset.UtcNow, 1, TimeSpan.FromMinutes(30));

        dbContext.StoredDocuments.AddRange(marketDocument, privateDocument, retentionReviewDocument);
        dbContext.ApplicationUsers.Add(lockedUser);
        dbContext.AuditEvents.Add(new AuditEvent(
            "SECURITY",
            "Security",
            "APPLICATION_USER",
            lockedUser.Id.ToString(),
            "AUTH_LOGIN_FAILED",
            "Login failed",
            "Operations center regression security signal.",
            null,
            lockedUser.UserName,
            "/admin/security",
            false,
            null,
            DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync();

        return new OperationsSignalsSeedResult(market.Id, tenant.Id, marketDocument.Id, privateDocument.Id, retentionReviewDocument.Id, lockedUser.Id);
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

    private async Task<HttpResponseMessage> SendPostAsync(string path, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { });
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

    private sealed record OperationsSignalsSeedResult(
        Guid MarketId,
        Guid MarketTenantId,
        Guid MarketDocumentId,
        Guid PrivateDocumentId,
        Guid RetentionReviewDocumentId,
        Guid LockedUserId);

    private sealed record LoginResponse(string AccessToken);

    private sealed record OperationsSummaryTestResponse(
        OperationsTimeWindowTestResponse TimeWindow,
        IReadOnlyList<OperationsKpiTestResponse> BusinessKpis,
        DocumentSummaryTestResponse? Documents,
        SecuritySummaryTestResponse? Security);

    private sealed record OperationsTimeWindowTestResponse(
        string TimeWindowCode,
        bool IsApplied);

    private sealed record OperationsKpiTestResponse(
        string KpiCode,
        string? ModuleCode);

    private sealed record DocumentSummaryTestResponse(
        int IntegrityIssuesCount,
        int ReviewDueCount,
        IReadOnlyList<DocumentSummaryWorkQueueCategoryTestResponse> WorkQueueCategories,
        IReadOnlyList<DocumentSummaryModuleTestResponse> Modules);

    private sealed record DocumentSummaryModuleTestResponse(string ModuleCode);

    private sealed record DocumentSummaryWorkQueueCategoryTestResponse(
        string WorkItemType,
        string ReasonCode);

    private sealed record SecuritySummaryTestResponse(
        int ActiveLockedUserCount,
        int LoginFailedCount);

    private sealed record OperationsWorkQueueTestResponse(
        IReadOnlyList<OperationsSeverityConventionTestResponse> SeverityConvention,
        IReadOnlyList<OperationsWorkQueueItemTestResponse> Items);

    private sealed record OperationsSeverityConventionTestResponse(
        string SeverityCode,
        int SortOrder);

    private sealed record OperationsWorkQueueItemTestResponse(
        string WorkItemKey,
        string CategoryCode,
        string TypeCode,
        string SeverityCode,
        string ModuleCode,
        string RouteHint,
        string ActionKind,
        string ActionLabel,
        string? QuickActionCode,
        string? QuickActionLabel,
        string? EntityType,
        string? EntityId,
        Guid? DocumentId,
        DateTimeOffset? RelevantUtc);
}
