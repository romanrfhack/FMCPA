namespace FMCPA.Domain.Entities.Documents;

public static class DocumentRetentionPolicyCodes
{
    public const string CertificateReview = "CERTIFICATE_REVIEW";
    public const string SignedLongTerm = "SIGNED_LONG_TERM";
    public const string EvidenceMediumTerm = "EVIDENCE_MEDIUM_TERM";
    public const string GenericReview = "GENERIC_REVIEW";

    public const string ActiveRetention = "ACTIVE_RETENTION";
    public const string ReviewDue = "REVIEW_DUE";
    public const string ExpiredRetention = "EXPIRED_RETENTION";

    public static readonly TimeSpan ReviewDueWindow = TimeSpan.FromDays(30);

    public static IReadOnlySet<string> SupportedPolicies { get; } = new HashSet<string>(
        [
            CertificateReview,
            SignedLongTerm,
            EvidenceMediumTerm,
            GenericReview
        ],
        StringComparer.Ordinal);

    public static IReadOnlySet<string> SupportedStatuses { get; } = new HashSet<string>(
        [
            ActiveRetention,
            ReviewDue,
            ExpiredRetention
        ],
        StringComparer.Ordinal);

    public static string ResolvePolicyForDocumentClass(string? documentClassCode)
    {
        var normalizedClass = DocumentClassCodes.NormalizeOrDefault(documentClassCode);
        return normalizedClass switch
        {
            DocumentClassCodes.Certificate => CertificateReview,
            DocumentClassCodes.SignedDocument => SignedLongTerm,
            DocumentClassCodes.SupportingDocument => EvidenceMediumTerm,
            DocumentClassCodes.PhotoEvidence => EvidenceMediumTerm,
            DocumentClassCodes.VideoEvidence => EvidenceMediumTerm,
            _ => GenericReview
        };
    }

    public static string NormalizeOrDefaultPolicy(string? policyCode)
    {
        if (string.IsNullOrWhiteSpace(policyCode))
        {
            return GenericReview;
        }

        var normalized = policyCode.Trim().ToUpperInvariant();
        if (!SupportedPolicies.Contains(normalized))
        {
            throw new ArgumentException("The document retention policy code is not supported.", nameof(policyCode));
        }

        return normalized;
    }

    public static DateTimeOffset CalculateRetentionUntilUtc(DateTimeOffset createdUtc, string? policyCode)
    {
        var normalizedPolicy = NormalizeOrDefaultPolicy(policyCode);
        return normalizedPolicy switch
        {
            SignedLongTerm => createdUtc.AddYears(7),
            EvidenceMediumTerm => createdUtc.AddYears(3),
            CertificateReview => createdUtc.AddYears(1),
            _ => createdUtc.AddYears(1)
        };
    }

    public static string ResolveRetentionStatusCode(DateTimeOffset retentionUntilUtc, DateTimeOffset referenceUtc)
    {
        if (retentionUntilUtc < referenceUtc)
        {
            return ExpiredRetention;
        }

        return retentionUntilUtc <= referenceUtc.Add(ReviewDueWindow)
            ? ReviewDue
            : ActiveRetention;
    }
}
