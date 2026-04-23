using FMCPA.Domain.Entities.Security;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Auth;

public static class AuthBootstrapper
{
    public static async Task EnsureDevelopmentBootstrapUsersAsync(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        var options = LocalBootstrapUserOptions.Resolve(app.Configuration);
        if (!options.Enabled)
        {
            app.Logger.LogInformation("Local bootstrap user provisioning is disabled.");
            return;
        }

        using var scope = app.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var passwordHashingService = scope.ServiceProvider.GetRequiredService<PasswordHashingService>();

        foreach (var userDefinition in options.Users)
        {
            if (!userDefinition.HasPassword)
            {
                if (userDefinition.IsPrimary)
                {
                    app.Logger.LogWarning(
                        "Primary local bootstrap user provisioning skipped because Auth:Bootstrap:Password is not configured. Configure a local admin password before validating privileged flows.");
                }
                else
                {
                    app.Logger.LogInformation(
                        "Optional local bootstrap user '{UserName}' ({RoleCode}) was not provisioned because no local password is configured.",
                        userDefinition.UserName,
                        userDefinition.RoleCode);
                }

                continue;
            }

            var normalizedUserName = ApplicationUser.NormalizeUserName(userDefinition.UserName);
            var existingUser = await dbContext.ApplicationUsers
                .SingleOrDefaultAsync(item => item.NormalizedUserName == normalizedUserName);

            if (existingUser is null)
            {
                dbContext.ApplicationUsers.Add(
                    new ApplicationUser(
                        userDefinition.UserName,
                        userDefinition.DisplayName,
                        passwordHashingService.HashPassword(userDefinition.Password!),
                        userDefinition.RoleCode));

                await dbContext.SaveChangesAsync();

                app.Logger.LogInformation(
                    "Local bootstrap user '{UserName}' ({RoleCode}) created for Development.",
                    userDefinition.UserName,
                    userDefinition.RoleCode);
                continue;
            }

            var needsPasswordUpdate = !passwordHashingService.VerifyPassword(userDefinition.Password!, existingUser.PasswordHash);
            existingUser.SyncBootstrapProfile(
                userDefinition.DisplayName,
                needsPasswordUpdate
                    ? passwordHashingService.HashPassword(userDefinition.Password!)
                    : existingUser.PasswordHash,
                userDefinition.RoleCode);

            await dbContext.SaveChangesAsync();

            app.Logger.LogInformation(
                "Local bootstrap user '{UserName}' ({RoleCode}) synchronized for Development.",
                userDefinition.UserName,
                userDefinition.RoleCode);
        }
    }
}
