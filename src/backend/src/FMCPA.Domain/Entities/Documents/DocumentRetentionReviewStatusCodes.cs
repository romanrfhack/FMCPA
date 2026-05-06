namespace FMCPA.Domain.Entities.Documents;

public static class DocumentRetentionReviewStatusCodes
{
    public const string Pending = "REVIEW_PENDING";
    public const string Completed = "REVIEW_COMPLETED";
    public const string Deferred = "REVIEW_DEFERRED";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(
        [
            Pending,
            Completed,
            Deferred
        ],
        StringComparer.Ordinal);

    public static string NormalizeOrDefault(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Pending;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!Supported.Contains(normalized))
        {
            throw new ArgumentException("The retention review status code is not supported.", nameof(value));
        }

        return normalized;
    }
}
