using FMCPA.Api.Extensions;
using FMCPA.Api.Endpoints;
using FMCPA.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

const string corsPolicyName = "FrontendLocal";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
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

app.UseCors(corsPolicyName);

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

app.MapHealthChecks(
    "/health",
    new HealthCheckOptions
    {
        ResponseWriter = HealthCheckResponseWriter.WriteAsync
    });

app.MapContactsEndpoints();
app.MapSharedCatalogEndpoints();
app.MapMarketsEndpoints();
app.MapDonationsEndpoints();
app.MapFinancialsEndpoints();
app.MapFederationEndpoints();
app.MapCloseoutEndpoints();

app.Run();
