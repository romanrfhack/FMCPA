using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace FMCPA.Api.AuthorizationRegressionTests;

public sealed class AuthorizationRegressionTests : IClassFixture<AuthorizationRegressionWebApplicationFactory>
{
    private static readonly Guid MissingEntityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private readonly HttpClient _client;

    public AuthorizationRegressionTests(AuthorizationRegressionWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    public static IEnumerable<object[]> ProtectedEndpointCases()
    {
        foreach (var endpoint in ProtectedEndpoints())
        {
            yield return [endpoint.Name, endpoint.Method, endpoint.Path];
        }
    }

    public static IEnumerable<object[]> RoleAccessCases()
    {
        foreach (var endpoint in ProtectedEndpoints())
        {
            foreach (var roleCode in endpoint.AllowedRoles)
            {
                yield return [endpoint.Name, endpoint.Method, endpoint.Path, roleCode, true];
            }

            foreach (var roleCode in endpoint.ForbiddenRoles)
            {
                yield return [endpoint.Name, endpoint.Method, endpoint.Path, roleCode, false];
            }
        }
    }

    [Fact]
    public async Task Public_endpoints_remain_public()
    {
        var root = await _client.GetAsync("/");
        var health = await _client.GetAsync("/health");
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                userName = AuthorizationRegressionWebApplicationFactory.AdminUserName,
                password = AuthorizationRegressionWebApplicationFactory.AdminPassword
            });

        Assert.Equal(HttpStatusCode.OK, root.StatusCode);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ProtectedEndpointCases))]
    public async Task Protected_endpoints_reject_anonymous_access(string name, string method, string path)
    {
        using var response = await SendAsync(method, path, accessToken: null);

        Assert.True(
            response.StatusCode == HttpStatusCode.Unauthorized,
            $"{name} should reject anonymous access with 401, but returned {(int)response.StatusCode}.");
    }

    [Theory]
    [MemberData(nameof(RoleAccessCases))]
    public async Task Protected_endpoints_enforce_expected_role_access(
        string name,
        string method,
        string path,
        string roleCode,
        bool shouldBeAllowed)
    {
        var accessToken = await LoginAsync(roleCode);

        using var response = await SendAsync(method, path, accessToken);

        if (shouldBeAllowed)
        {
            Assert.True(
                response.StatusCode is not HttpStatusCode.Unauthorized and not HttpStatusCode.Forbidden,
                $"{name} should allow {roleCode}, but returned {(int)response.StatusCode}.");
            return;
        }

        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden,
            $"{name} should deny {roleCode} with 403, but returned {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task Self_service_password_change_validates_current_password_and_invalidates_previous_token()
    {
        var adminToken = await LoginAsync("ADMIN");
        var userName = $"selfsvc{Guid.NewGuid():N}"[..24];
        const string originalPassword = "SelfService123";
        const string newPassword = "SelfService456";

        using var createUser = await SendAsync(
            HttpMethod.Post.Method,
            "/api/admin/users/",
            adminToken,
            new
            {
                userName,
                displayName = "Self-service regression",
                roleCode = "OPERATOR",
                password = originalPassword
            });

        Assert.Equal(HttpStatusCode.Created, createUser.StatusCode);

        var originalToken = await LoginWithCredentialsAsync(userName, originalPassword);

        using var wrongCurrentPassword = await SendAsync(
            HttpMethod.Post.Method,
            "/api/auth/change-password",
            originalToken,
            new
            {
                currentPassword = "WrongPassword123",
                newPassword,
                confirmNewPassword = newPassword
            });
        using var invalidNewPassword = await SendAsync(
            HttpMethod.Post.Method,
            "/api/auth/change-password",
            originalToken,
            new
            {
                currentPassword = originalPassword,
                newPassword = "short",
                confirmNewPassword = "short"
            });
        using var successfulChange = await SendAsync(
            HttpMethod.Post.Method,
            "/api/auth/change-password",
            originalToken,
            new
            {
                currentPassword = originalPassword,
                newPassword,
                confirmNewPassword = newPassword
            });
        using var oldTokenSession = await SendAsync(HttpMethod.Get.Method, "/api/auth/session", originalToken);
        using var oldPasswordLogin = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                userName,
                password = originalPassword
            });
        var newToken = await LoginWithCredentialsAsync(userName, newPassword);

        Assert.Equal(HttpStatusCode.BadRequest, wrongCurrentPassword.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidNewPassword.StatusCode);
        Assert.Equal(HttpStatusCode.OK, successfulChange.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldTokenSession.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(newToken));
    }

    private static IReadOnlyList<EndpointAuthorizationCase> ProtectedEndpoints()
    {
        var allRoles = new[] { "ADMIN", "OPERATOR", "READONLY" };
        var writeRoles = new[] { "ADMIN", "OPERATOR" };
        var adminOnly = new[] { "ADMIN" };
        var nonAdmins = new[] { "OPERATOR", "READONLY" };

        return
        [
            new("Auth session", HttpMethod.Get.Method, "/api/auth/session", allRoles, []),
            new("Auth self-service password change", HttpMethod.Post.Method, "/api/auth/change-password", allRoles, []),
            new("Dashboard read", HttpMethod.Get.Method, "/api/dashboard/summary", allRoles, []),
            new("Operations summary read", HttpMethod.Get.Method, "/api/operations/summary", allRoles, []),
            new("Operations summary export", HttpMethod.Get.Method, "/api/operations/summary/export", allRoles, []),
            new("Operations work queue read", HttpMethod.Get.Method, "/api/operations/work-queue?take=1", allRoles, []),
            new("Operations work queue export", HttpMethod.Get.Method, "/api/operations/work-queue/export?take=1", allRoles, []),
            new("History read", HttpMethod.Get.Method, "/api/bitacora?take=1", allRoles, []),
            new("Document catalog read", HttpMethod.Get.Method, "/api/documents?take=1", allRoles, []),
            new("Document catalog export", HttpMethod.Get.Method, "/api/documents/export?take=1", allRoles, []),
            new("Document summary read", HttpMethod.Get.Method, "/api/documents/summary", allRoles, []),
            new("Document by entity read", HttpMethod.Get.Method, $"/api/documents/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={MissingEntityId}&take=1", allRoles, []),
            new("Document entity timeline read", HttpMethod.Get.Method, $"/api/documents/timeline/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={MissingEntityId}&take=1", allRoles, []),
            new("Document rules read", HttpMethod.Get.Method, "/api/documents/rules", allRoles, []),
            new("Document requirements by entity read", HttpMethod.Get.Method, $"/api/documents/requirements/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={MissingEntityId}", allRoles, []),
            new("Document completeness by entity read", HttpMethod.Get.Method, $"/api/documents/completeness/by-entity?moduleCode=MARKETS&entityType=MARKET_TENANT&entityId={MissingEntityId}", allRoles, []),
            new("Document pending read", HttpMethod.Get.Method, "/api/documents/pending?take=1", allRoles, []),
            new("Document work queue read", HttpMethod.Get.Method, "/api/documents/work-queue?take=1", allRoles, []),
            new("Document work queue export", HttpMethod.Get.Method, "/api/documents/work-queue/export?take=1", allRoles, []),
            new("Document retention review queue", HttpMethod.Get.Method, "/api/documents/review-queue?take=1", adminOnly, nonAdmins),
            new("Document retention review queue export", HttpMethod.Get.Method, "/api/documents/review-queue/export?take=1", adminOnly, nonAdmins),
            new("Document timeline read", HttpMethod.Get.Method, $"/api/documents/{MissingEntityId}/timeline", allRoles, []),
            new("Document metadata update", HttpMethod.Patch.Method, $"/api/documents/{MissingEntityId}/metadata", adminOnly, nonAdmins),
            new("Document retention review update", HttpMethod.Patch.Method, $"/api/documents/{MissingEntityId}/retention-review", adminOnly, nonAdmins),
            new("Document retention override set", HttpMethod.Patch.Method, $"/api/documents/{MissingEntityId}/retention-override", adminOnly, nonAdmins),
            new("Document retention override clear", HttpMethod.Delete.Method, $"/api/documents/{MissingEntityId}/retention-override", adminOnly, nonAdmins),
            new("Document administrative hold set", HttpMethod.Post.Method, $"/api/documents/{MissingEntityId}/hold", adminOnly, nonAdmins),
            new("Document administrative hold clear", HttpMethod.Delete.Method, $"/api/documents/{MissingEntityId}/hold", adminOnly, nonAdmins),
            new("Document archive", HttpMethod.Post.Method, $"/api/documents/{MissingEntityId}/archive", adminOnly, nonAdmins),
            new("Document restore", HttpMethod.Post.Method, $"/api/documents/{MissingEntityId}/restore", adminOnly, nonAdmins),
            new("Markets read", HttpMethod.Get.Method, "/api/markets", allRoles, []),
            new("Markets write", HttpMethod.Post.Method, "/api/markets", writeRoles, ["READONLY"]),
            new("Markets formal close", HttpMethod.Post.Method, $"/api/markets/{MissingEntityId}/close", adminOnly, nonAdmins),
            new("Donations read", HttpMethod.Get.Method, "/api/donations", allRoles, []),
            new("Donations write", HttpMethod.Post.Method, "/api/donations", writeRoles, ["READONLY"]),
            new("Donations formal close", HttpMethod.Post.Method, $"/api/donations/{MissingEntityId}/close", adminOnly, nonAdmins),
            new("Financials read", HttpMethod.Get.Method, "/api/financials", allRoles, []),
            new("Financials current permit resolution read", HttpMethod.Get.Method, "/api/financials/current-permit?financialName=F&institutionOrDependency=D&placeOrStand=S", allRoles, []),
            new("Financials renewal chain read", HttpMethod.Get.Method, $"/api/financials/{MissingEntityId}/renewal-chain", allRoles, []),
            new("Financials renewal draft read", HttpMethod.Get.Method, $"/api/financials/{MissingEntityId}/renewal-draft", allRoles, []),
            new("Financials write", HttpMethod.Post.Method, "/api/financials", writeRoles, ["READONLY"]),
            new("Financials permit renewal", HttpMethod.Post.Method, $"/api/financials/{MissingEntityId}/renew", writeRoles, ["READONLY"]),
            new("Financials formal close", HttpMethod.Post.Method, $"/api/financials/{MissingEntityId}/close", adminOnly, nonAdmins),
            new("Federation read", HttpMethod.Get.Method, "/api/federation/actions", allRoles, []),
            new("Federation write", HttpMethod.Post.Method, "/api/federation/actions", writeRoles, ["READONLY"]),
            new("Federation formal close", HttpMethod.Post.Method, $"/api/federation/actions/{MissingEntityId}/close", adminOnly, nonAdmins),
            new("Shared catalogs read", HttpMethod.Get.Method, "/api/commission-types", allRoles, []),
            new("Shared catalogs admin", HttpMethod.Post.Method, "/api/commission-types", adminOnly, nonAdmins),
            new("User management admin", HttpMethod.Get.Method, "/api/admin/users/", adminOnly, nonAdmins),
            new("Security observability admin", HttpMethod.Get.Method, "/api/admin/security/summary", adminOnly, nonAdmins)
        ];
    }

    private async Task<string> LoginAsync(string roleCode)
    {
        return roleCode switch
        {
            "ADMIN" => await LoginWithCredentialsAsync(
                AuthorizationRegressionWebApplicationFactory.AdminUserName,
                AuthorizationRegressionWebApplicationFactory.AdminPassword),
            "OPERATOR" => await LoginWithCredentialsAsync(
                AuthorizationRegressionWebApplicationFactory.OperatorUserName,
                AuthorizationRegressionWebApplicationFactory.OperatorPassword),
            "READONLY" => await LoginWithCredentialsAsync(
                AuthorizationRegressionWebApplicationFactory.ReadOnlyUserName,
                AuthorizationRegressionWebApplicationFactory.ReadOnlyPassword),
            _ => throw new ArgumentOutOfRangeException(nameof(roleCode), roleCode, "Unsupported role code.")
        };
    }

    private async Task<string> LoginWithCredentialsAsync(string userName, string password)
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

    private async Task<HttpResponseMessage> SendAsync(
        string method,
        string path,
        string? accessToken,
        object? body = null)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        if (!HttpMethod.Get.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
        {
            request.Content = JsonContent.Create(body ?? BuildDefaultBody(path));
        }

        return await _client.SendAsync(request);
    }

    private static object BuildDefaultBody(string path)
    {
        if (path.Contains("/close", StringComparison.OrdinalIgnoreCase))
        {
            return new { reason = "authorization regression" };
        }

        if (path.Equals("/api/auth/change-password", StringComparison.OrdinalIgnoreCase))
        {
            return new
            {
                currentPassword = "WrongPassword123",
                newPassword = "Regression123",
                confirmNewPassword = "Regression123"
            };
        }

        if (path.Equals("/api/commission-types", StringComparison.OrdinalIgnoreCase))
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            return new
            {
                code = $"REG_{suffix}",
                name = $"Regression {suffix}",
                description = "Authorization regression",
                sortOrder = 999
            };
        }

        return new { };
    }

    private sealed record EndpointAuthorizationCase(
        string Name,
        string Method,
        string Path,
        IReadOnlyList<string> AllowedRoles,
        IReadOnlyList<string> ForbiddenRoles);

    private sealed record LoginResponse(string AccessToken);
}
