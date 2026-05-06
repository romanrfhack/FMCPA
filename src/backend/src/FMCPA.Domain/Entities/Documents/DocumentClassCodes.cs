namespace FMCPA.Domain.Entities.Documents;

public static class DocumentClassCodes
{
    public const string Certificate = "CERTIFICATE";
    public const string SignedDocument = "SIGNED_DOCUMENT";
    public const string SupportingDocument = "SUPPORTING_DOCUMENT";
    public const string PhotoEvidence = "PHOTO_EVIDENCE";
    public const string VideoEvidence = "VIDEO_EVIDENCE";
    public const string Other = "OTHER";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(
        [
            Certificate,
            SignedDocument,
            SupportingDocument,
            PhotoEvidence,
            VideoEvidence,
            Other
        ],
        StringComparer.Ordinal);

    public static string NormalizeOrDefault(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Other;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!Supported.Contains(normalized))
        {
            throw new ArgumentException("The document class code is not supported.", nameof(value));
        }

        return normalized;
    }
}
