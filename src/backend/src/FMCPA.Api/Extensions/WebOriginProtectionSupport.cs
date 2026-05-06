using FMCPA.Api.Auth;
using Microsoft.Net.Http.Headers;

namespace FMCPA.Api.Extensions;

internal static class WebOriginProtectionSupport
{
    public static IApplicationBuilder UseWebOriginProtection(this IApplicationBuilder app)
    {
        return app.Use(
            async (httpContext, next) =>
            {
                if (!IsSensitiveApiRequest(httpContext.Request))
                {
                    await next(httpContext);
                    return;
                }

                var settings = httpContext.RequestServices.GetRequiredService<HttpBoundarySecuritySettings>();
                var originValidation = ValidateOriginIfPresent(httpContext.Request, settings);
                if (!originValidation.IsAllowed)
                {
                    await WriteProblemAsync(
                        httpContext,
                        StatusCodes.Status403Forbidden,
                        "Origen web no permitido.",
                        "La solicitud no cumple la politica de origen del cliente web.");
                    return;
                }

                if (ShouldRequireClientHeader(httpContext.Request, settings.WebOriginProtection)
                    && !HasExpectedClientHeader(httpContext.Request, settings.WebOriginProtection))
                {
                    await WriteProblemAsync(
                        httpContext,
                        StatusCodes.Status400BadRequest,
                        "Solicitud web no permitida.",
                        "La solicitud no incluye la senal esperada del cliente web.");
                    return;
                }

                await next(httpContext);
            });
    }

    private static bool IsSensitiveApiRequest(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method)
            || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method))
        {
            return false;
        }

        return request.Path.Value?.StartsWith("/api", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static OriginValidationResult ValidateOriginIfPresent(
        HttpRequest request,
        HttpBoundarySecuritySettings settings)
    {
        var requestOrigin = ResolveOrigin(request);
        if (requestOrigin is null)
        {
            return OriginValidationResult.Allowed();
        }

        var isAllowed = settings.AllowedCorsOrigins.Contains(requestOrigin, StringComparer.OrdinalIgnoreCase);
        return isAllowed
            ? OriginValidationResult.Allowed()
            : OriginValidationResult.Denied();
    }

    private static string? ResolveOrigin(HttpRequest request)
    {
        var origin = request.Headers[HeaderNames.Origin].ToString();
        if (!string.IsNullOrWhiteSpace(origin))
        {
            return NormalizeOrigin(origin);
        }

        var referer = request.Headers[HeaderNames.Referer].ToString();
        if (!string.IsNullOrWhiteSpace(referer)
            && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
        {
            return BuildOrigin(refererUri);
        }

        return null;
    }

    private static string? NormalizeOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
        {
            return "invalid-origin";
        }

        return BuildOrigin(uri);
    }

    private static string BuildOrigin(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return "invalid-origin";
        }

        var builder = new UriBuilder(uri.Scheme, uri.Host, uri.Port);
        return builder.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    private static bool ShouldRequireClientHeader(HttpRequest request, WebOriginProtectionRule rule)
    {
        return HasBrowserRequestSignal(request)
            || request.Headers.ContainsKey(rule.ClientHeaderName);
    }

    private static bool HasBrowserRequestSignal(HttpRequest request)
    {
        return request.Headers.ContainsKey(HeaderNames.Origin)
            || request.Headers.ContainsKey(HeaderNames.Referer)
            || request.Headers.ContainsKey("Sec-Fetch-Site")
            || request.Headers.ContainsKey("Sec-Fetch-Mode")
            || request.Headers.ContainsKey("Sec-Fetch-Dest");
    }

    private static bool HasExpectedClientHeader(HttpRequest request, WebOriginProtectionRule rule)
    {
        var headerValue = request.Headers[rule.ClientHeaderName].ToString();
        return headerValue.Equals(rule.ClientHeaderValue, StringComparison.Ordinal);
    }

    private static async Task WriteProblemAsync(
        HttpContext httpContext,
        int statusCode,
        string title,
        string detail)
    {
        var response = httpContext.Response;
        if (response.HasStarted)
        {
            return;
        }

        response.StatusCode = statusCode;
        response.ContentType = "application/problem+json";

        await response.WriteAsJsonAsync(
            new
            {
                type = "https://fmcpa.local/security/web-origin-protection",
                title,
                status = statusCode,
                detail
            },
            httpContext.RequestAborted);
    }

    private readonly record struct OriginValidationResult(bool IsAllowed)
    {
        public static OriginValidationResult Allowed() => new(true);

        public static OriginValidationResult Denied() => new(false);
    }
}
