using FMCPA.Domain.Entities.Security;

namespace FMCPA.Api.Auth;

public static class PlatformPermissionCodes
{
    public const string ClaimType = "fmcpa_permission";

    public const string DashboardRead = "DASHBOARD_READ";
    public const string HistoryRead = "HISTORY_READ";
    public const string ContactsRead = "CONTACTS_READ";
    public const string ContactsWrite = "CONTACTS_WRITE";
    public const string MarketsRead = "MARKETS_READ";
    public const string MarketsWrite = "MARKETS_WRITE";
    public const string DonationsRead = "DONATIONS_READ";
    public const string DonationsWrite = "DONATIONS_WRITE";
    public const string FinancialsRead = "FINANCIALS_READ";
    public const string FinancialsWrite = "FINANCIALS_WRITE";
    public const string FederationRead = "FEDERATION_READ";
    public const string FederationWrite = "FEDERATION_WRITE";
    public const string CatalogsRead = "CATALOGS_READ";
    public const string CatalogsAdmin = "CATALOGS_ADMIN";
    public const string UsersAdmin = "USERS_ADMIN";
    public const string FormalCloseAdmin = "FORMAL_CLOSE_ADMIN";

    public static readonly string[] ReadOnlyPermissions =
    [
        DashboardRead,
        HistoryRead,
        ContactsRead,
        MarketsRead,
        DonationsRead,
        FinancialsRead,
        FederationRead,
        CatalogsRead
    ];

    public static readonly string[] OperatorPermissions =
    [
        ..ReadOnlyPermissions,
        ContactsWrite,
        MarketsWrite,
        DonationsWrite,
        FinancialsWrite,
        FederationWrite
    ];

    public static readonly string[] AdminPermissions =
    [
        ..OperatorPermissions,
        CatalogsAdmin,
        UsersAdmin,
        FormalCloseAdmin
    ];

    public static IReadOnlyList<string> ForRole(string roleCode)
    {
        return ApplicationRoleCodes.Normalize(roleCode) switch
        {
            ApplicationRoleCodes.Admin => AdminPermissions,
            ApplicationRoleCodes.Operator => OperatorPermissions,
            ApplicationRoleCodes.ReadOnly => ReadOnlyPermissions,
            _ => throw new ArgumentOutOfRangeException(nameof(roleCode), "The application role code is not supported.")
        };
    }
}
