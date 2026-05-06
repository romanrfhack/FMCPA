namespace FMCPA.Domain.Entities.Documents;

public static class DocumentOperationalStatusCodes
{
    public const string ActiveOk = "ACTIVE_OK";
    public const string IntegrityIssue = "INTEGRITY_ISSUE";
    public const string OnHold = "ON_HOLD";
    public const string ReviewDue = "REVIEW_DUE";
    public const string RetentionExpired = "RETENTION_EXPIRED";
    public const string Archived = "ARCHIVED";
    public const string Superseded = "SUPERSEDED";

    public const string SeverityHigh = "HIGH";
    public const string SeverityMedium = "MEDIUM";
    public const string SeverityLow = "LOW";
    public const string SeverityNone = "NONE";

    private const string ValidIntegrityState = "VALID";

    public static IReadOnlySet<string> SupportedStatuses { get; } = new HashSet<string>(
        [
            ActiveOk,
            IntegrityIssue,
            OnHold,
            ReviewDue,
            RetentionExpired,
            Archived,
            Superseded
        ],
        StringComparer.Ordinal);

    public static string Resolve(
        StoredDocument document,
        string? integrityState,
        DateTimeOffset referenceUtc)
    {
        if (!string.Equals(integrityState, ValidIntegrityState, StringComparison.Ordinal))
        {
            return IntegrityIssue;
        }

        if (document.IsAdministrativeHold)
        {
            return OnHold;
        }

        if (document.IsSuperseded)
        {
            return Superseded;
        }

        if (document.IsArchived)
        {
            return Archived;
        }

        var retentionStatusCode = document.ResolveRetentionStatusCode(referenceUtc);
        return retentionStatusCode switch
        {
            DocumentRetentionPolicyCodes.ExpiredRetention => RetentionExpired,
            DocumentRetentionPolicyCodes.ReviewDue => ReviewDue,
            _ => ActiveOk
        };
    }

    public static string ResolveSeverity(string operationalStatusCode)
    {
        return operationalStatusCode switch
        {
            IntegrityIssue => SeverityHigh,
            RetentionExpired => SeverityMedium,
            OnHold => SeverityLow,
            ReviewDue => SeverityLow,
            Archived => SeverityLow,
            Superseded => SeverityLow,
            _ => SeverityNone
        };
    }

    public static int ResolveSortOrder(string operationalStatusCode)
    {
        return operationalStatusCode switch
        {
            IntegrityIssue => 0,
            OnHold => 1,
            Superseded => 2,
            Archived => 3,
            RetentionExpired => 4,
            ReviewDue => 5,
            ActiveOk => 6,
            _ => 7
        };
    }
}
