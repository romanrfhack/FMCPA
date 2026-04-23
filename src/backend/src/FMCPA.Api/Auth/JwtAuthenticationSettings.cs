using System.Security.Cryptography;

namespace FMCPA.Api.Auth;

public sealed class JwtAuthenticationSettings
{
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

        return new JwtAuthenticationSettings(
            string.IsNullOrWhiteSpace(issuer) ? "FMCPA.Local" : issuer,
            string.IsNullOrWhiteSpace(audience) ? "FMCPA.Web.Local" : audience,
            signingKey,
            tokenLifetimeMinutes,
            usesEphemeralSigningKey);
    }
}
