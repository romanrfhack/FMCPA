using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FMCPA.Application.Abstractions.Storage;
using FMCPA.Domain.Entities.Audit;
using FMCPA.Domain.Entities.Documents;
using FMCPA.Domain.Entities.Security;
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
            item.CategoryCode == "DOCUMENTS"
            && item.ModuleCode == "MARKETS"
            && item.DocumentId == signals.MarketDocumentId
            && item.SeverityCode == "HIGH");
        Assert.Contains(queue.Items, item =>
            item.CategoryCode == "SECURITY"
            && item.ModuleCode == "SECURITY"
            && item.EntityId == signals.LockedUserId.ToString());
        Assert.DoesNotContain(queue.Items, item => item.ModuleCode == "PRIVATE" || item.DocumentId == signals.PrivateDocumentId);
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

    private async Task<OperationsSignalsSeedResult> SeedOperationsSignalsAsync()
    {
        var marketDocument = new StoredDocument(
            "MARKETS",
            DocumentAreaCodes.MarketsTenantCertificates,
            "MARKET_TENANT",
            Guid.NewGuid(),
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
        var lockedUser = new ApplicationUser(
            $"opslock{Guid.NewGuid():N}"[..24],
            "Operations locked",
            "test-hash",
            ApplicationRoleCodes.ReadOnly);
        lockedUser.RecordFailedLogin(DateTimeOffset.UtcNow, 1, TimeSpan.FromMinutes(30));

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        dbContext.StoredDocuments.AddRange(marketDocument, privateDocument);
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

        return new OperationsSignalsSeedResult(marketDocument.Id, privateDocument.Id, lockedUser.Id);
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

    private sealed record OperationsSignalsSeedResult(
        Guid MarketDocumentId,
        Guid PrivateDocumentId,
        Guid LockedUserId);

    private sealed record LoginResponse(string AccessToken);

    private sealed record OperationsSummaryTestResponse(
        IReadOnlyList<OperationsKpiTestResponse> BusinessKpis,
        DocumentSummaryTestResponse? Documents,
        SecuritySummaryTestResponse? Security);

    private sealed record OperationsKpiTestResponse(
        string KpiCode,
        string? ModuleCode);

    private sealed record DocumentSummaryTestResponse(
        int IntegrityIssuesCount,
        IReadOnlyList<DocumentSummaryModuleTestResponse> Modules);

    private sealed record DocumentSummaryModuleTestResponse(string ModuleCode);

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
        string CategoryCode,
        string SeverityCode,
        string ModuleCode,
        string? EntityId,
        Guid? DocumentId);
}
