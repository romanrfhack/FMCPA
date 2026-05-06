using FMCPA.Domain.Entities.Security;
using Microsoft.AspNetCore.Authorization;

namespace FMCPA.Api.Auth;

public static class PlatformAuthorizationPolicies
{
    public const string ReadAccess = "Platform.ReadAccess";
    public const string WriteAccess = "Platform.WriteAccess";
    public const string AdminAccess = "Platform.AdminAccess";
    public const string DashboardReadAccess = "Platform.Dashboard.Read";
    public const string HistoryReadAccess = "Platform.History.Read";
    public const string ContactsReadAccess = "Platform.Contacts.Read";
    public const string ContactsWriteAccess = "Platform.Contacts.Write";
    public const string MarketsReadAccess = "Platform.Markets.Read";
    public const string MarketsWriteAccess = "Platform.Markets.Write";
    public const string MarketsFormalCloseAccess = "Platform.Markets.FormalClose";
    public const string DonationsReadAccess = "Platform.Donations.Read";
    public const string DonationsWriteAccess = "Platform.Donations.Write";
    public const string DonationsFormalCloseAccess = "Platform.Donations.FormalClose";
    public const string FinancialsReadAccess = "Platform.Financials.Read";
    public const string FinancialsWriteAccess = "Platform.Financials.Write";
    public const string FinancialsFormalCloseAccess = "Platform.Financials.FormalClose";
    public const string FederationReadAccess = "Platform.Federation.Read";
    public const string FederationWriteAccess = "Platform.Federation.Write";
    public const string FederationFormalCloseAccess = "Platform.Federation.FormalClose";
    public const string CatalogsReadAccess = "Platform.Catalogs.Read";
    public const string CatalogsAdminAccess = "Platform.Catalogs.Admin";
    public const string UsersAdminAccess = "Platform.Users.Admin";
    public const string FormalCloseAdminAccess = "Platform.FormalClose.Admin";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(
            ReadAccess,
            policy => policy.RequireAuthenticatedUser()
                .RequireRole(ApplicationRoleCodes.All));

        options.AddPolicy(
            WriteAccess,
            policy => policy.RequireAuthenticatedUser()
                .RequireRole(ApplicationRoleCodes.ReadWrite));

        options.AddPolicy(
            AdminAccess,
            policy => policy.RequireAuthenticatedUser()
                .RequireRole(ApplicationRoleCodes.Admin));

        AddPermissionPolicy(options, DashboardReadAccess, PlatformPermissionCodes.DashboardRead);
        AddPermissionPolicy(options, HistoryReadAccess, PlatformPermissionCodes.HistoryRead);
        AddPermissionPolicy(options, ContactsReadAccess, PlatformPermissionCodes.ContactsRead);
        AddPermissionPolicy(options, ContactsWriteAccess, PlatformPermissionCodes.ContactsWrite);
        AddPermissionPolicy(options, MarketsReadAccess, PlatformPermissionCodes.MarketsRead);
        AddPermissionPolicy(options, MarketsWriteAccess, PlatformPermissionCodes.MarketsWrite);
        AddPermissionPolicy(options, DonationsReadAccess, PlatformPermissionCodes.DonationsRead);
        AddPermissionPolicy(options, DonationsWriteAccess, PlatformPermissionCodes.DonationsWrite);
        AddPermissionPolicy(options, FinancialsReadAccess, PlatformPermissionCodes.FinancialsRead);
        AddPermissionPolicy(options, FinancialsWriteAccess, PlatformPermissionCodes.FinancialsWrite);
        AddPermissionPolicy(options, FederationReadAccess, PlatformPermissionCodes.FederationRead);
        AddPermissionPolicy(options, FederationWriteAccess, PlatformPermissionCodes.FederationWrite);
        AddPermissionPolicy(options, CatalogsReadAccess, PlatformPermissionCodes.CatalogsRead);
        AddPermissionPolicy(options, CatalogsAdminAccess, PlatformPermissionCodes.CatalogsAdmin);
        AddPermissionPolicy(options, UsersAdminAccess, PlatformPermissionCodes.UsersAdmin);
        AddPermissionPolicy(options, FormalCloseAdminAccess, PlatformPermissionCodes.FormalCloseAdmin);

        AddPermissionPolicy(
            options,
            MarketsFormalCloseAccess,
            PlatformPermissionCodes.MarketsWrite,
            PlatformPermissionCodes.FormalCloseAdmin);
        AddPermissionPolicy(
            options,
            DonationsFormalCloseAccess,
            PlatformPermissionCodes.DonationsWrite,
            PlatformPermissionCodes.FormalCloseAdmin);
        AddPermissionPolicy(
            options,
            FinancialsFormalCloseAccess,
            PlatformPermissionCodes.FinancialsWrite,
            PlatformPermissionCodes.FormalCloseAdmin);
        AddPermissionPolicy(
            options,
            FederationFormalCloseAccess,
            PlatformPermissionCodes.FederationWrite,
            PlatformPermissionCodes.FormalCloseAdmin);
    }

    private static void AddPermissionPolicy(AuthorizationOptions options, string policyName, params string[] permissionCodes)
    {
        options.AddPolicy(
            policyName,
            policy =>
            {
                policy.RequireAuthenticatedUser();

                foreach (var permissionCode in permissionCodes)
                {
                    policy.RequireClaim(PlatformPermissionCodes.ClaimType, permissionCode);
                }
            });
    }
}
