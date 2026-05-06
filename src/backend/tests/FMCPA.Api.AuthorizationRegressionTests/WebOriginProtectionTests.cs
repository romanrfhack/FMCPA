using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace FMCPA.Api.AuthorizationRegressionTests;

public sealed class WebOriginProtectionTests : IClassFixture<AuthorizationRegressionWebApplicationFactory>
{
    private const string AllowedOrigin = "http://127.0.0.1:4200";
    private const string DisallowedOrigin = "https://unexpected.example.test";
    private const string WebClientHeaderName = "X-FMCPA-Client";
    private const string WebClientHeaderValue = "FMCPA-Web";

    private readonly HttpClient _client;

    public WebOriginProtectionTests(AuthorizationRegressionWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Browser_login_with_expected_origin_and_client_header_is_allowed()
    {
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            "/api/auth/login",
            new
            {
                userName = AuthorizationRegressionWebApplicationFactory.AdminUserName,
                password = AuthorizationRegressionWebApplicationFactory.AdminPassword
            },
            origin: AllowedOrigin,
            includeWebClientHeader: true);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Browser_sensitive_request_without_client_header_is_rejected()
    {
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            "/api/auth/login",
            new
            {
                userName = AuthorizationRegressionWebApplicationFactory.AdminUserName,
                password = AuthorizationRegressionWebApplicationFactory.AdminPassword
            },
            origin: AllowedOrigin,
            includeWebClientHeader: false);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Browser_sensitive_request_with_disallowed_origin_is_rejected()
    {
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            "/api/auth/login",
            new
            {
                userName = AuthorizationRegressionWebApplicationFactory.AdminUserName,
                password = AuthorizationRegressionWebApplicationFactory.AdminPassword
            },
            origin: DisallowedOrigin,
            includeWebClientHeader: true);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Browser_admin_mutation_with_expected_origin_and_client_header_is_allowed()
    {
        var accessToken = await LoginAsync();
        var userName = $"origin{Guid.NewGuid():N}"[..24];
        using var request = CreateJsonRequest(
            HttpMethod.Post,
            "/api/admin/users/",
            new
            {
                userName,
                displayName = "Origin protection regression",
                roleCode = "READONLY",
                password = "OriginUser123"
            },
            origin: AllowedOrigin,
            includeWebClientHeader: true,
            accessToken);

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Non_browser_local_script_login_remains_supported_without_web_client_header()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                userName = AuthorizationRegressionWebApplicationFactory.AdminUserName,
                password = AuthorizationRegressionWebApplicationFactory.AdminPassword
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<string> LoginAsync()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                userName = AuthorizationRegressionWebApplicationFactory.AdminUserName,
                password = AuthorizationRegressionWebApplicationFactory.AdminPassword
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrWhiteSpace(login?.AccessToken));

        return login.AccessToken;
    }

    private static HttpRequestMessage CreateJsonRequest(
        HttpMethod method,
        string path,
        object body,
        string origin,
        bool includeWebClientHeader,
        string? accessToken = null)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.TryAddWithoutValidation("Origin", origin);

        if (includeWebClientHeader)
        {
            request.Headers.TryAddWithoutValidation(WebClientHeaderName, WebClientHeaderValue);
        }

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return request;
    }

    private sealed record LoginResponse(string AccessToken);
}
