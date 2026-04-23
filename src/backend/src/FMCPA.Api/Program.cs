using FMCPA.Api.Endpoints;
using FMCPA.Api.Auth;
using FMCPA.Api.Extensions;
using FMCPA.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

const string corsPolicyName = "FrontendLocal";

var builder = WebApplication.CreateBuilder(args);
var jwtAuthenticationSettings = JwtAuthenticationSettings.Resolve(builder.Configuration, builder.Environment);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton(jwtAuthenticationSettings);
builder.Services.AddSingleton<PasswordHashingService>();
builder.Services.AddSingleton<JwtTokenIssuer>();
builder.Services.AddScoped<ApplicationUserTokenValidationService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtAuthenticationSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtAuthenticationSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAuthenticationSettings.SigningKey)),
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var tokenValidationService = context.HttpContext.RequestServices.GetRequiredService<ApplicationUserTokenValidationService>();
                var validationError = await tokenValidationService.ValidateAsync(
                    context.Principal!,
                    context.HttpContext.RequestAborted);

                if (validationError is not null)
                {
                    context.Fail(validationError);
                }
            }
        };
    });
builder.Services.AddAuthorization(PlatformAuthorizationPolicies.Configure);
builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:4200", "http://127.0.0.1:4200"];

    options.AddPolicy(
        corsPolicyName,
        policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("The foundation API is running."));

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenApi();
}

var app = builder.Build();

if (jwtAuthenticationSettings.UsesEphemeralSigningKey)
{
    app.Logger.LogWarning(
        "Auth:Jwt:SigningKey is not configured. Development is using an ephemeral signing key and active tokens will be invalidated on every backend restart.");
}

await AuthBootstrapper.EnsureDevelopmentBootstrapUsersAsync(app);

app.UseCors(corsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet(
    "/",
    (IHostEnvironment environment) => Results.Ok(new
    {
        service = "FMCPA Platform API",
        environment = environment.EnvironmentName,
        health = "/health",
        openApi = environment.IsDevelopment() ? "/openapi/v1.json" : null
    }));

app.MapAuthEndpoints();

app.MapHealthChecks(
    "/health",
    new HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    });

app.MapContactsEndpoints();
app.MapSharedCatalogEndpoints();
app.MapUserManagementEndpoints();
app.MapMarketsEndpoints();
app.MapDonationsEndpoints();
app.MapFinancialsEndpoints();
app.MapFederationEndpoints();
app.MapCloseoutEndpoints();

app.Run();
