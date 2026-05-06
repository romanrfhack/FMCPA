using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace FMCPA.Api.Auth;

public static class PlatformRateLimitingPolicies
{
    public const string AuthLogin = "auth-login";
    public const string SensitiveAdmin = "sensitive-admin";

    public static void Configure(RateLimiterOptions options, HttpBoundarySecuritySettings settings)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = WriteRateLimitResponseAsync;

        options.AddPolicy(
            AuthLogin,
            httpContext => RateLimitPartition.GetFixedWindowLimiter(
                ResolveClientPartitionKey(httpContext),
                _ => CreateFixedWindowOptions(settings.LoginRateLimit)));

        options.AddPolicy(
            SensitiveAdmin,
            httpContext => RateLimitPartition.GetFixedWindowLimiter(
                ResolveUserAwarePartitionKey(httpContext),
                _ => CreateFixedWindowOptions(settings.SensitiveAdminRateLimit)));
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(RateLimitRule rule)
    {
        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = rule.PermitLimit,
            Window = rule.Window,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        };
    }

    private static async ValueTask WriteRateLimitResponseAsync(
        OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        var response = context.HttpContext.Response;
        if (response.HasStarted)
        {
            return;
        }

        var retryAfterSeconds = 60;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        }

        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.ContentType = "application/problem+json";

        await response.WriteAsJsonAsync(
            new
            {
                type = "https://tools.ietf.org/html/rfc6585#section-4",
                title = "Demasiados intentos.",
                status = StatusCodes.Status429TooManyRequests,
                detail = "Se excedio el limite temporal de solicitudes para este recurso. Intenta de nuevo mas tarde.",
                retryAfterSeconds
            },
            cancellationToken);
    }

    private static string ResolveClientPartitionKey(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-client";
    }

    private static string ResolveUserAwarePartitionKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return string.IsNullOrWhiteSpace(userId)
            ? ResolveClientPartitionKey(httpContext)
            : $"{ResolveClientPartitionKey(httpContext)}:{userId}";
    }
}
