namespace FMCPA.Api.Auth;

public sealed class HttpBoundarySecuritySettings
{
    private static readonly string[] DevelopmentCorsOrigins =
    [
        "http://localhost:4200",
        "http://127.0.0.1:4200"
    ];

    private HttpBoundarySecuritySettings(
        IReadOnlyList<string> allowedCorsOrigins,
        WebOriginProtectionRule webOriginProtection,
        RateLimitRule loginRateLimit,
        RateLimitRule sensitiveAdminRateLimit)
    {
        AllowedCorsOrigins = allowedCorsOrigins;
        WebOriginProtection = webOriginProtection;
        LoginRateLimit = loginRateLimit;
        SensitiveAdminRateLimit = sensitiveAdminRateLimit;
    }

    public IReadOnlyList<string> AllowedCorsOrigins { get; }

    public WebOriginProtectionRule WebOriginProtection { get; }

    public RateLimitRule LoginRateLimit { get; }

    public RateLimitRule SensitiveAdminRateLimit { get; }

    public static HttpBoundarySecuritySettings Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        return new HttpBoundarySecuritySettings(
            ResolveAllowedCorsOrigins(configuration, environment),
            WebOriginProtectionRule.Resolve(configuration),
            RateLimitRule.Resolve(configuration, "Security:RateLimiting:Login", defaultPermitLimit: 10, defaultWindowSeconds: 60),
            RateLimitRule.Resolve(configuration, "Security:RateLimiting:SensitiveAdmin", defaultPermitLimit: 60, defaultWindowSeconds: 60));
    }

    private static IReadOnlyList<string> ResolveAllowedCorsOrigins(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (configuredOrigins is null || configuredOrigins.Length == 0)
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("Cors:AllowedOrigins must be configured outside Development.");
            }

            configuredOrigins = DevelopmentCorsOrigins;
        }

        var normalizedOrigins = configuredOrigins
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedOrigins.Length == 0)
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must contain at least one origin.");
        }

        foreach (var origin in normalizedOrigins)
        {
            if (origin == "*")
            {
                throw new InvalidOperationException("Cors:AllowedOrigins cannot use wildcard origins.");
            }

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                || string.IsNullOrWhiteSpace(uri.Host)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || !string.IsNullOrWhiteSpace(uri.PathAndQuery.Trim('/')))
            {
                throw new InvalidOperationException($"Cors:AllowedOrigins contains an invalid origin: '{origin}'.");
            }

            if (!environment.IsDevelopment() && IsLocalhostOrigin(uri))
            {
                throw new InvalidOperationException("Cors:AllowedOrigins cannot use localhost origins outside Development.");
            }
        }

        return normalizedOrigins;
    }

    private static bool IsLocalhostOrigin(Uri uri)
    {
        return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
               || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
               || string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record WebOriginProtectionRule(string ClientHeaderName, string ClientHeaderValue)
{
    private const string SectionName = "Security:WebOriginProtection";
    private const string DefaultClientHeaderName = "X-FMCPA-Client";
    private const string DefaultClientHeaderValue = "FMCPA-Web";

    public static WebOriginProtectionRule Resolve(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var clientHeaderName = section.GetValue<string?>("ClientHeaderName")?.Trim() ?? DefaultClientHeaderName;
        var clientHeaderValue = section.GetValue<string?>("ClientHeaderValue")?.Trim() ?? DefaultClientHeaderValue;

        if (string.IsNullOrWhiteSpace(clientHeaderName)
            || clientHeaderName.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException($"{SectionName}:ClientHeaderName must be a non-empty HTTP header name.");
        }

        if (string.IsNullOrWhiteSpace(clientHeaderValue))
        {
            throw new InvalidOperationException($"{SectionName}:ClientHeaderValue must be configured.");
        }

        return new WebOriginProtectionRule(clientHeaderName, clientHeaderValue);
    }
}

public sealed record RateLimitRule(int PermitLimit, int WindowSeconds)
{
    public TimeSpan Window => TimeSpan.FromSeconds(WindowSeconds);

    public static RateLimitRule Resolve(
        IConfiguration configuration,
        string sectionName,
        int defaultPermitLimit,
        int defaultWindowSeconds)
    {
        var section = configuration.GetSection(sectionName);
        var permitLimit = section.GetValue<int?>("PermitLimit") ?? defaultPermitLimit;
        var windowSeconds = section.GetValue<int?>("WindowSeconds") ?? defaultWindowSeconds;

        if (permitLimit <= 0)
        {
            throw new InvalidOperationException($"{sectionName}:PermitLimit must be greater than zero.");
        }

        if (windowSeconds <= 0)
        {
            throw new InvalidOperationException($"{sectionName}:WindowSeconds must be greater than zero.");
        }

        return new RateLimitRule(permitLimit, windowSeconds);
    }
}
