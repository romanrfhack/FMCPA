using FMCPA.Domain.Entities.Security;
using Microsoft.AspNetCore.Authorization;

namespace FMCPA.Api.Auth;

public static class PlatformAuthorizationPolicies
{
    public const string ReadAccess = "Platform.ReadAccess";
    public const string WriteAccess = "Platform.WriteAccess";
    public const string AdminAccess = "Platform.AdminAccess";

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
    }
}
