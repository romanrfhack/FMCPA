using Microsoft.Net.Http.Headers;

namespace FMCPA.Api.Extensions;

internal static class DocumentDownloadResponseSupport
{
    public static IResult File(
        HttpContext httpContext,
        Stream content,
        string? contentType,
        string originalFileName)
    {
        httpContext.Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        httpContext.Response.Headers[HeaderNames.CacheControl] = "no-store";
        httpContext.Response.Headers[HeaderNames.Pragma] = "no-cache";

        return Results.File(
            content,
            DocumentUploadSecurity.NormalizeDownloadContentType(contentType),
            DocumentUploadSecurity.SanitizeOriginalFileName(originalFileName),
            enableRangeProcessing: false);
    }
}
