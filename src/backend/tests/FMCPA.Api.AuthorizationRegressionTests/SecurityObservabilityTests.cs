using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace FMCPA.Api.AuthorizationRegressionTests;

public sealed class SecurityObservabilityTests : IClassFixture<AuthorizationRegressionWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityObservabilityTests(AuthorizationRegressionWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Admin_can_query_security_events_summary_locked_users_and_unlock()
    {
        var adminToken = await LoginAsync(
            AuthorizationRegressionWebApplicationFactory.AdminUserName,
            AuthorizationRegressionWebApplicationFactory.AdminPassword);
        var userName = $"lockout{Guid.NewGuid():N}"[..24];
        const string password = "LockoutUser123";

        using var createUser = await SendJsonAsync(
            HttpMethod.Post,
            "/api/admin/users/",
            adminToken,
            new
            {
                userName,
                displayName = "Lockout regression",
                roleCode = "READONLY",
                password
            });
        var createdUser = await createUser.Content.ReadFromJsonAsync<ApplicationUserAdminResponse>();

        Assert.Equal(HttpStatusCode.Created, createUser.StatusCode);
        Assert.NotNull(createdUser);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failedLogin = await _client.PostAsJsonAsync(
                "/api/auth/login",
                new
                {
                    userName,
                    password = "WrongPassword123"
                });

            Assert.True(
                failedLogin.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Locked,
                $"Expected failed login or lockout, but got {(int)failedLogin.StatusCode}.");
        }

        using var summaryResponse = await SendJsonAsync(
            HttpMethod.Get,
            "/api/admin/security/summary?hours=24",
            adminToken);
        using var eventsResponse = await SendJsonAsync(
            HttpMethod.Get,
            $"/api/admin/security/events?eventType=AUTH_LOGIN_FAILED&userName={userName}&take=10",
            adminToken);
        using var lockedUsersResponse = await SendJsonAsync(
            HttpMethod.Get,
            "/api/admin/security/locked-users",
            adminToken);
        using var unlockResponse = await SendJsonAsync(
            HttpMethod.Post,
            $"/api/admin/users/{createdUser!.Id}/unlock",
            adminToken,
            new { });
        using var lockedUsersAfterUnlockResponse = await SendJsonAsync(
            HttpMethod.Get,
            "/api/admin/security/locked-users",
            adminToken);

        var summary = await summaryResponse.Content.ReadFromJsonAsync<SecurityActivitySummaryResponse>();
        var events = await eventsResponse.Content.ReadFromJsonAsync<List<SecurityAuditEventResponse>>();
        var lockedUsers = await lockedUsersResponse.Content.ReadFromJsonAsync<List<LockedApplicationUserResponse>>();
        var lockedUsersAfterUnlock = await lockedUsersAfterUnlockResponse.Content.ReadFromJsonAsync<List<LockedApplicationUserResponse>>();

        Assert.Equal(HttpStatusCode.OK, summaryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, eventsResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, lockedUsersResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unlockResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, lockedUsersAfterUnlockResponse.StatusCode);
        Assert.True(summary?.LoginFailedCount >= 5);
        Assert.True(summary?.ActiveLockedUserCount >= 1);
        Assert.Contains(events ?? [], item => item.ActionType == "AUTH_LOGIN_FAILED" && item.Reference == userName);
        Assert.Contains(lockedUsers ?? [], item => item.Id == createdUser.Id && item.AccessFailedCount >= 5);
        Assert.DoesNotContain(lockedUsersAfterUnlock ?? [], item => item.Id == createdUser.Id);
    }

    [Theory]
    [InlineData("OPERATOR")]
    [InlineData("READONLY")]
    public async Task Non_admin_roles_cannot_query_security_observability(string roleCode)
    {
        var accessToken = roleCode switch
        {
            "OPERATOR" => await LoginAsync(
                AuthorizationRegressionWebApplicationFactory.OperatorUserName,
                AuthorizationRegressionWebApplicationFactory.OperatorPassword),
            "READONLY" => await LoginAsync(
                AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
                AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword),
            _ => throw new ArgumentOutOfRangeException(nameof(roleCode), roleCode, "Unsupported role code.")
        };

        using var response = await SendJsonAsync(
            HttpMethod.Get,
            "/api/admin/security/summary",
            accessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

    private async Task<HttpResponseMessage> SendJsonAsync(
        HttpMethod method,
        string path,
        string accessToken,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        if (!HttpMethod.Get.Equals(method))
        {
            request.Content = JsonContent.Create(body ?? new { });
        }

        return await _client.SendAsync(request);
    }

    private sealed record LoginResponse(string AccessToken);

    private sealed record ApplicationUserAdminResponse(Guid Id, string UserName);

    private sealed record SecurityActivitySummaryResponse(
        int LoginFailedCount,
        int ActiveLockedUserCount);

    private sealed record SecurityAuditEventResponse(string ActionType, string? Reference);

    private sealed record LockedApplicationUserResponse(Guid Id, int AccessFailedCount);
}
