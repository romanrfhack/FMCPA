using FMCPA.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FMCPA.Api.AuthorizationRegressionTests;

public sealed class AuthorizationRegressionWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AdminUserName = "admin";
    public const string AdminPassword = "AuthAdmin123";
    public const string OperatorUserName = "operator";
    public const string OperatorPassword = "AuthOperator123";
    public const string ReadOnlyUserName = "readonly";
    public const string ReadOnlyPassword = "AuthReadonly123";

    private readonly Dictionary<string, string?> _environmentOverrides;
    private readonly Dictionary<string, string?> _previousEnvironmentValues;
    private readonly string _databaseName = $"FMCPA.AuthorizationRegression.{Guid.NewGuid():N}";

    public AuthorizationRegressionWebApplicationFactory()
    {
        _environmentOverrides = new Dictionary<string, string?>
        {
            ["ConnectionStrings__PlatformDatabase"] = "Server=localhost;Database=FMCPA_AuthorizationRegression;User Id=sa;Password=Unused123;TrustServerCertificate=True;Encrypt=False",
            ["Cors__AllowedOrigins__0"] = "http://127.0.0.1:4200",
            ["Auth__Jwt__Issuer"] = "FMCPA.AuthorizationRegression",
            ["Auth__Jwt__Audience"] = "FMCPA.AuthorizationRegression.Client",
            ["Auth__Jwt__SigningKey"] = "AuthorizationRegressionSigningKey20260505LocalOnly1234567890",
            ["Auth__Jwt__TokenLifetimeMinutes"] = "60",
            ["Auth__Bootstrap__Enabled"] = "true",
            ["Auth__Bootstrap__UserName"] = AdminUserName,
            ["Auth__Bootstrap__DisplayName"] = "Admin regression",
            ["Auth__Bootstrap__Password"] = AdminPassword,
            ["Auth__Bootstrap__Operator__UserName"] = OperatorUserName,
            ["Auth__Bootstrap__Operator__DisplayName"] = "Operator regression",
            ["Auth__Bootstrap__Operator__Password"] = OperatorPassword,
            ["Auth__Bootstrap__ReadOnly__UserName"] = ReadOnlyUserName,
            ["Auth__Bootstrap__ReadOnly__DisplayName"] = "Readonly regression",
            ["Auth__Bootstrap__ReadOnly__Password"] = ReadOnlyPassword,
            ["Security__Credentials__PasswordPolicy__MinimumLength"] = "12",
            ["Security__Credentials__PasswordPolicy__RequireUppercase"] = "true",
            ["Security__Credentials__PasswordPolicy__RequireLowercase"] = "true",
            ["Security__Credentials__PasswordPolicy__RequireDigit"] = "true",
            ["Security__Credentials__PasswordPolicy__RequireNonAlphanumeric"] = "false",
            ["Security__Credentials__Lockout__MaxFailedAccessAttempts"] = "5",
            ["Security__Credentials__Lockout__CooldownSeconds"] = "900",
            ["Security__RateLimiting__Login__PermitLimit"] = "200",
            ["Security__RateLimiting__Login__WindowSeconds"] = "60",
            ["Security__RateLimiting__SensitiveAdmin__PermitLimit"] = "200",
            ["Security__RateLimiting__SensitiveAdmin__WindowSeconds"] = "60",
            ["Storage__Markets__MarketTenantCertificatesPath"] = Path.Combine(Path.GetTempPath(), "fmcpa-authz-regression", "markets"),
            ["Storage__Donations__ApplicationEvidencePath"] = Path.Combine(Path.GetTempPath(), "fmcpa-authz-regression", "donations"),
            ["Storage__Federation__ApplicationEvidencePath"] = Path.Combine(Path.GetTempPath(), "fmcpa-authz-regression", "federation")
        };

        _previousEnvironmentValues = _environmentOverrides.ToDictionary(
            pair => pair.Key,
            pair => Environment.GetEnvironmentVariable(pair.Key));

        foreach (var pair in _environmentOverrides)
        {
            Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(
            services =>
            {
                services.RemoveAll<IDbContextOptionsConfiguration<PlatformDbContext>>();
                services.RemoveAll<DbContextOptions<PlatformDbContext>>();
                services.RemoveAll<PlatformDbContext>();
                services.AddDbContext<PlatformDbContext>(options => options.UseInMemoryDatabase(_databaseName));
            });
    }

    protected override void Dispose(bool disposing)
    {
        foreach (var pair in _previousEnvironmentValues)
        {
            Environment.SetEnvironmentVariable(pair.Key, pair.Value);
        }

        base.Dispose(disposing);
    }
}
