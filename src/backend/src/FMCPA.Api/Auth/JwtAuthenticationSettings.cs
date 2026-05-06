using System.Security.Cryptography;
using System.Text;

namespace FMCPA.Api.Auth;

public sealed class JwtAuthenticationSettings
{
    private const int MinimumSigningKeyBytes = 32;
    private const string DefaultDevelopmentIssuer = "FMCPA.Local";
    private const string DefaultDevelopmentAudience = "FMCPA.Web.Local";

    private JwtAuthenticationSettings(
        string issuer,
        string audience,
        string signingKey,
        int tokenLifetimeMinutes,
        bool usesEphemeralSigningKey)
    {
        Issuer = issuer;
        Audience = audience;
        SigningKey = signingKey;
        TokenLifetimeMinutes = tokenLifetimeMinutes;
        UsesEphemeralSigningKey = usesEphemeralSigningKey;
    }

    public string Issuer { get; }

    public string Audience { get; }

    public string SigningKey { get; }

    public int TokenLifetimeMinutes { get; }

    public bool UsesEphemeralSigningKey { get; }

    public TimeSpan TokenLifetime => TimeSpan.FromMinutes(TokenLifetimeMinutes);

    public static JwtAuthenticationSettings Resolve(IConfiguration configuration, IHostEnvironment environment)
    {
        var issuer = configuration["Auth:Jwt:Issuer"]?.Trim();
        var audience = configuration["Auth:Jwt:Audience"]?.Trim();
        var configuredSigningKey = configuration["Auth:Jwt:SigningKey"]?.Trim();
        var tokenLifetimeMinutes = configuration.GetValue<int?>("Auth:Jwt:TokenLifetimeMinutes") ?? 480;

        if (tokenLifetimeMinutes <= 0)
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("Auth:Jwt:TokenLifetimeMinutes must be greater than zero outside Development.");
            }

            tokenLifetimeMinutes = 480;
        }

        var usesEphemeralSigningKey = false;
        var signingKey = configuredSigningKey;

        if (string.IsNullOrWhiteSpace(signingKey))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException("Auth:Jwt:SigningKey is required outside Development.");
            }

            signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            usesEphemeralSigningKey = true;
        }
        else if (Encoding.UTF8.GetByteCount(signingKey) < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException($"Auth:Jwt:SigningKey must be at least {MinimumSigningKeyBytes} bytes for HS256.");
        }

        if (!environment.IsDevelopment())
        {
            if (string.IsNullOrWhiteSpace(issuer))
            {
                throw new InvalidOperationException("Auth:Jwt:Issuer is required outside Development.");
            }

            if (string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException("Auth:Jwt:Audience is required outside Development.");
            }

            if (string.Equals(issuer, DefaultDevelopmentIssuer, StringComparison.Ordinal)
                || string.Equals(audience, DefaultDevelopmentAudience, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Auth:Jwt:Issuer and Auth:Jwt:Audience must be environment-specific outside Development.");
            }
        }

        return new JwtAuthenticationSettings(
            string.IsNullOrWhiteSpace(issuer) ? DefaultDevelopmentIssuer : issuer,
            string.IsNullOrWhiteSpace(audience) ? DefaultDevelopmentAudience : audience,
            signingKey,
            tokenLifetimeMinutes,
            usesEphemeralSigningKey);
    }
}
