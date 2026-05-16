namespace FMCPA.Domain.Entities.Shared;

public static class ContactInterventionHelpTypes
{
    public const string Information = "INFORMATION";
    public const string Facilitation = "FACILITATION";
    public const string Validation = "VALIDATION";
    public const string Escalation = "ESCALATION";
    public const string FollowUp = "FOLLOW_UP";
    public const string Unblocking = "UNBLOCKING";
    public const string Other = "OTHER";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Information,
        Facilitation,
        Validation,
        Escalation,
        FollowUp,
        Unblocking,
        Other
    };
}

public static class ContactInterventionOutcomes
{
    public const string Useful = "USEFUL";
    public const string Successful = "SUCCESSFUL";
    public const string Pending = "PENDING";
    public const string NoResponse = "NO_RESPONSE";
    public const string NotApplicable = "NOT_APPLICABLE";
    public const string Other = "OTHER";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Useful,
        Successful,
        Pending,
        NoResponse,
        NotApplicable,
        Other
    };
}
