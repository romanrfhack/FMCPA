using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FMCPA.Api.AuthorizationRegressionTests;

public sealed class AuthorizationSurfaceGuardrailTests : IClassFixture<AuthorizationRegressionWebApplicationFactory>
{
    private const string ManifestFileName = "security-authorization-surface-guardrails.json";

    private readonly AuthorizationRegressionWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AuthorizationSurfaceGuardrailTests(AuthorizationRegressionWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public void Public_endpoints_match_the_explicit_allowlist()
    {
        var manifest = LoadManifest();
        var endpoints = DiscoverEndpoints();

        var unexpectedPublicEndpoints = endpoints
            .Where(endpoint => endpoint.IsPublic)
            .Where(endpoint => FindBestRule(manifest.PublicEndpoints, endpoint) is null)
            .Select(Describe)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var missingPublicEndpoints = manifest.PublicEndpoints
            .Where(rule => !endpoints.Any(endpoint => endpoint.IsPublic && RuleMatchesEndpoint(rule, endpoint)))
            .Select(Describe)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(unexpectedPublicEndpoints);
        Assert.Empty(missingPublicEndpoints);
    }

    [Fact]
    public void Api_endpoints_are_either_protected_or_explicitly_public()
    {
        var manifest = LoadManifest();
        var endpoints = DiscoverEndpoints()
            .Where(endpoint => endpoint.RoutePattern.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var missingManifestCoverage = endpoints
            .Where(endpoint => FindBestRule(manifest.PublicEndpoints, endpoint) is null)
            .Where(endpoint => FindBestRule(manifest.ProtectedEndpointRules, endpoint) is null)
            .Select(Describe)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(missingManifestCoverage);
    }

    [Fact]
    public void Protected_manifest_rules_have_matching_real_endpoints()
    {
        var manifest = LoadManifest();
        var endpoints = DiscoverEndpoints();

        var staleProtectedRules = manifest.ProtectedEndpointRules
            .Where(rule => !endpoints.Any(endpoint => endpoint.RequiresAuthorization && RuleMatchesEndpoint(rule, endpoint)))
            .Select(Describe)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(staleProtectedRules);
    }

    [Fact]
    public void Protected_api_endpoints_declare_the_expected_authorization_metadata()
    {
        var manifest = LoadManifest();
        var endpoints = DiscoverEndpoints()
            .Where(endpoint => endpoint.RoutePattern.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var failures = new List<string>();

        foreach (var endpoint in endpoints)
        {
            var publicRule = FindBestRule(manifest.PublicEndpoints, endpoint);
            if (publicRule is not null)
            {
                if (!endpoint.IsPublic)
                {
                    failures.Add($"{Describe(endpoint)} is listed as public but declares authorization metadata.");
                }

                continue;
            }

            var protectedRule = FindBestRule(manifest.ProtectedEndpointRules, endpoint);
            if (protectedRule is null)
            {
                failures.Add($"{Describe(endpoint)} has no protected endpoint rule in {ManifestFileName}.");
                continue;
            }

            if (!endpoint.RequiresAuthorization)
            {
                failures.Add($"{Describe(endpoint)} is protected in the manifest but has no authorization metadata.");
                continue;
            }

            if (protectedRule.RequiresPolicy)
            {
                if (protectedRule.RequiredPolicies.Count == 0)
                {
                    failures.Add($"{Describe(protectedRule)} declares policy authorization without required policies.");
                    continue;
                }

                var missingPolicies = protectedRule.RequiredPolicies
                    .Where(policy => !endpoint.Policies.Contains(policy, StringComparer.Ordinal))
                    .ToArray();

                if (missingPolicies.Length > 0)
                {
                    failures.Add(
                        $"{Describe(endpoint)} is missing expected policy metadata: {string.Join(", ", missingPolicies)}.");
                }

                continue;
            }

            if (protectedRule.RequiresAuthenticatedUser && endpoint.Policies.Count > 0)
            {
                failures.Add(
                    $"{Describe(endpoint)} is listed as auth-only but declares policy metadata: {string.Join(", ", endpoint.Policies)}.");
            }
        }

        Assert.Empty(failures);
    }

    private IReadOnlyList<DiscoveredEndpoint> DiscoverEndpoints()
    {
        _ = _client;

        var dataSource = _factory.Services.GetRequiredService<EndpointDataSource>();
        return dataSource.Endpoints
            .OfType<RouteEndpoint>()
            .SelectMany(endpoint => GetMethods(endpoint).Select(method => BuildDiscoveredEndpoint(endpoint, method)))
            .OrderBy(endpoint => endpoint.RoutePattern, StringComparer.Ordinal)
            .ThenBy(endpoint => endpoint.Method, StringComparer.Ordinal)
            .ToArray();
    }

    private static DiscoveredEndpoint BuildDiscoveredEndpoint(RouteEndpoint endpoint, string method)
    {
        var authorizeData = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
        var policies = authorizeData
            .Select(item => item.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Select(policy => policy!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var allowsAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
        var routePattern = NormalizeRoutePattern(endpoint.RoutePattern.RawText ?? endpoint.RoutePattern.ToString() ?? string.Empty);

        return new DiscoveredEndpoint(
            method,
            routePattern,
            authorizeData.Count > 0 && !allowsAnonymous,
            allowsAnonymous || authorizeData.Count == 0,
            policies);
    }

    private static IReadOnlyList<string> GetMethods(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
        if (methods is null || methods.Count == 0)
        {
            return ["*"];
        }

        return methods
            .Select(method => method.ToUpperInvariant())
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static AuthorizationSurfaceGuardrailManifest LoadManifest()
    {
        var manifestPath = Path.Combine(AppContext.BaseDirectory, ManifestFileName);
        Assert.True(File.Exists(manifestPath), $"{ManifestFileName} must be copied to the test output.");

        var manifest = JsonSerializer.Deserialize<AuthorizationSurfaceGuardrailManifest>(
            File.ReadAllText(manifestPath),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(manifest);
        Assert.NotEmpty(manifest.PublicEndpoints);
        Assert.NotEmpty(manifest.ProtectedEndpointRules);

        return manifest;
    }

    private static AuthorizationEndpointRule? FindBestRule(
        IEnumerable<AuthorizationEndpointRule> rules,
        DiscoveredEndpoint endpoint)
    {
        return rules
            .Where(rule => RuleMatchesEndpoint(rule, endpoint))
            .OrderByDescending(rule => Specificity(rule.RoutePattern))
            .ThenByDescending(rule => MethodMatchesExactly(rule.Method, endpoint.Method) ? 1 : 0)
            .FirstOrDefault();
    }

    private static bool RuleMatchesEndpoint(AuthorizationEndpointRule rule, DiscoveredEndpoint endpoint)
    {
        return MethodMatches(rule.Method, endpoint.Method)
            && RouteMatches(rule.RoutePattern, endpoint.RoutePattern);
    }

    private static bool MethodMatches(string ruleMethod, string endpointMethod)
    {
        return ruleMethod.Equals("*", StringComparison.OrdinalIgnoreCase)
            || ruleMethod.Equals(endpointMethod, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MethodMatchesExactly(string ruleMethod, string endpointMethod)
    {
        return ruleMethod.Equals(endpointMethod, StringComparison.OrdinalIgnoreCase);
    }

    private static bool RouteMatches(string ruleRoutePattern, string endpointRoutePattern)
    {
        var normalizedRule = NormalizeRoutePattern(ruleRoutePattern);
        var normalizedEndpoint = NormalizeRoutePattern(endpointRoutePattern);

        if (!normalizedRule.Contains('*', StringComparison.Ordinal))
        {
            return normalizedRule.Equals(normalizedEndpoint, StringComparison.OrdinalIgnoreCase);
        }

        var regexPattern = "^" + Regex.Escape(normalizedRule).Replace("\\*", ".*", StringComparison.Ordinal) + "$";
        return Regex.IsMatch(normalizedEndpoint, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string NormalizeRoutePattern(string routePattern)
    {
        if (string.IsNullOrWhiteSpace(routePattern) || routePattern.Equals("/", StringComparison.Ordinal))
        {
            return "/";
        }

        return routePattern.StartsWith("/", StringComparison.Ordinal)
            ? routePattern
            : "/" + routePattern;
    }

    private static int Specificity(string routePattern)
    {
        return NormalizeRoutePattern(routePattern).Replace("*", string.Empty, StringComparison.Ordinal).Length;
    }

    private static string Describe(DiscoveredEndpoint endpoint)
    {
        var auth = endpoint.IsPublic
            ? "public"
            : endpoint.Policies.Count == 0
                ? "auth"
                : string.Join(", ", endpoint.Policies);

        return $"{endpoint.Method} {endpoint.RoutePattern} [{auth}]";
    }

    private static string Describe(AuthorizationEndpointRule rule)
    {
        return $"{rule.Method} {rule.RoutePattern}";
    }

    private sealed record DiscoveredEndpoint(
        string Method,
        string RoutePattern,
        bool RequiresAuthorization,
        bool IsPublic,
        IReadOnlyList<string> Policies);

    private sealed class AuthorizationSurfaceGuardrailManifest
    {
        public int Version { get; init; }
        public List<AuthorizationEndpointRule> PublicEndpoints { get; init; } = [];
        public List<AuthorizationEndpointRule> ProtectedEndpointRules { get; init; } = [];
    }

    private sealed class AuthorizationEndpointRule
    {
        public string Method { get; init; } = string.Empty;
        public string RoutePattern { get; init; } = string.Empty;
        public string Authorization { get; init; } = string.Empty;
        public List<string> RequiredPolicies { get; init; } = [];
        public string Reason { get; init; } = string.Empty;

        public bool RequiresPolicy => Authorization.Equals("policy", StringComparison.OrdinalIgnoreCase);
        public bool RequiresAuthenticatedUser => Authorization.Equals("authenticated", StringComparison.OrdinalIgnoreCase);
    }
}
