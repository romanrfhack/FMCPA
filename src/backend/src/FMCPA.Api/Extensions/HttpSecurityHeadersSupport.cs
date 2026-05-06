namespace FMCPA.Api.Extensions;

internal static class HttpSecurityHeadersSupport
{
    public static IApplicationBuilder UseHttpSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(
            async (httpContext, next) =>
            {
                httpContext.Response.OnStarting(
                    () =>
                    {
                        ApplySecurityHeaders(httpContext);
                        return Task.CompletedTask;
                    });

                await next(httpContext);
            });
    }

    private static void ApplySecurityHeaders(HttpContext httpContext)
    {
        var headers = httpContext.Response.Headers;
        SetHeaderIfMissing(headers, "X-Content-Type-Options", "nosniff");
        SetHeaderIfMissing(headers, "Referrer-Policy", "no-referrer");
        SetHeaderIfMissing(headers, "X-Frame-Options", "DENY");
        SetHeaderIfMissing(headers, "Permissions-Policy", "camera=(), microphone=(), geolocation=()");

        if (IsApiRequest(httpContext.Request.Path))
        {
            headers.CacheControl = "no-store";
            headers.Pragma = "no-cache";
            headers.Expires = "0";
        }
    }

    private static bool IsApiRequest(PathString path)
    {
        return path.Value?.StartsWith("/api", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static void SetHeaderIfMissing(IHeaderDictionary headers, string name, string value)
    {
        if (!headers.ContainsKey(name))
        {
            headers[name] = value;
        }
    }
}
