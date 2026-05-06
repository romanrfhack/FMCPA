using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FMCPA.Api.Auth;
using FMCPA.Api.Contracts.AdminUsers;
using FMCPA.Api.Extensions;
using FMCPA.Domain.Entities.Audit;
using FMCPA.Domain.Entities.Security;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class UserManagementEndpoints
{
    private const string SecurityModuleCode = "SECURITY";
    private const string SecurityModuleName = "Seguridad";
    private const string ApplicationUserEntityType = "APPLICATION_USER";
    private const string UserAdministrationNavigationPath = "/admin/users";
    private const int MaximumUserNameLength = 64;
    private const int MaximumDisplayNameLength = 128;

    public static IEndpointRouteBuilder MapUserManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users")
            .WithTags("User Administration")
            .RequireUsersAdminAccess()
            .RequireRateLimiting(PlatformRateLimitingPolicies.SensitiveAdmin);

        group.MapGet(
            "/",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var users = await dbContext.ApplicationUsers
                    .AsNoTracking()
                    .OrderBy(item => item.UserName)
                    .ToListAsync(cancellationToken);

                var response = users
                    .Select(BuildResponse)
                    .ToList();

                return Results.Ok(response);
            });

        group.MapGet(
            "/{userId:guid}",
            async (Guid userId, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.ApplicationUsers
                    .AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

                return user is null
                    ? Results.NotFound()
                    : Results.Ok(BuildResponse(user));
            });

        group.MapPost(
            "/",
            async (CreateApplicationUserRequest request, ClaimsPrincipal principal, PlatformDbContext dbContext, PasswordHashingService passwordHashingService, PasswordPolicyService passwordPolicyService, CancellationToken cancellationToken) =>
            {
                var errors = ValidateCreateRequest(request, passwordPolicyService);
                var normalizedUserName = string.IsNullOrWhiteSpace(request.UserName)
                    ? null
                    : ApplicationUser.NormalizeUserName(request.UserName);

                if (normalizedUserName is not null)
                {
                    var duplicateExists = await dbContext.ApplicationUsers
                        .AsNoTracking()
                        .AnyAsync(item => item.NormalizedUserName == normalizedUserName, cancellationToken);

                    if (duplicateExists)
                    {
                        errors["userName"] = ["Ya existe un usuario con ese userName."];
                    }
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var user = new ApplicationUser(
                    request.UserName,
                    request.DisplayName,
                    passwordHashingService.HashPassword(request.Password),
                    request.RoleCode);

                dbContext.ApplicationUsers.Add(user);
                dbContext.AuditEvents.Add(
                    CreateUserAuditEvent(
                        principal,
                        user,
                        actionType: "USER_CREATED",
                        title: "Usuario creado",
                        detail: $"El usuario '{user.UserName}' fue creado con rol {user.RoleCode}.",
                        metadata: new
                        {
                            createdUserId = user.Id,
                            createdUserName = user.UserName,
                            createdRoleCode = user.RoleCode,
                            isActive = user.IsActive
                        }));

                await dbContext.SaveChangesAsync(cancellationToken);

                return Results.Created($"/api/admin/users/{user.Id}", BuildResponse(user));
            });

        group.MapPatch(
            "/{userId:guid}/role",
            async (Guid userId, ChangeApplicationUserRoleRequest request, ClaimsPrincipal principal, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var errors = ValidateRoleRequest(request);
                var user = await dbContext.ApplicationUsers
                    .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

                if (user is null)
                {
                    return Results.NotFound();
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var nextRoleCode = ApplicationRoleCodes.Normalize(request.RoleCode);
                if (await WouldLeaveSystemWithoutActiveAdminAsync(dbContext, user, nextRoleCode, user.IsActive, cancellationToken))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["roleCode"] = ["No se puede remover el último ADMIN activo del sistema."]
                        });
                }

                var previousRoleCode = user.RoleCode;
                user.ChangeRole(nextRoleCode);

                if (!string.Equals(previousRoleCode, user.RoleCode, StringComparison.Ordinal))
                {
                    dbContext.AuditEvents.Add(
                        CreateUserAuditEvent(
                            principal,
                            user,
                            actionType: "USER_ROLE_CHANGED",
                            title: "Rol de usuario actualizado",
                            detail: $"El usuario '{user.UserName}' cambió de rol {previousRoleCode} a {user.RoleCode}.",
                            metadata: new
                            {
                                targetUserId = user.Id,
                                targetUserName = user.UserName,
                                previousRoleCode,
                                newRoleCode = user.RoleCode
                            }));
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                return Results.Ok(BuildResponse(user));
            });

        group.MapPatch(
            "/{userId:guid}/activation",
            async (Guid userId, SetApplicationUserActivationRequest request, ClaimsPrincipal principal, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.ApplicationUsers
                    .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

                if (user is null)
                {
                    return Results.NotFound();
                }

                if (await WouldLeaveSystemWithoutActiveAdminAsync(dbContext, user, user.RoleCode, request.IsActive, cancellationToken))
                {
                    return Results.ValidationProblem(
                        new Dictionary<string, string[]>
                        {
                            ["isActive"] = ["No se puede desactivar el último ADMIN activo del sistema."]
                        });
                }

                var previousIsActive = user.IsActive;
                user.SetIsActive(request.IsActive);

                if (previousIsActive != user.IsActive)
                {
                    dbContext.AuditEvents.Add(
                        CreateUserAuditEvent(
                            principal,
                            user,
                            actionType: user.IsActive ? "USER_ACTIVATED" : "USER_DEACTIVATED",
                            title: user.IsActive ? "Usuario activado" : "Usuario desactivado",
                            detail: $"El usuario '{user.UserName}' fue {(user.IsActive ? "activado" : "desactivado")}.",
                            metadata: new
                            {
                                targetUserId = user.Id,
                                targetUserName = user.UserName,
                                isActive = user.IsActive
                            }));
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                return Results.Ok(BuildResponse(user));
            });

        group.MapPost(
            "/{userId:guid}/reset-password",
            async (Guid userId, ResetApplicationUserPasswordRequest request, ClaimsPrincipal principal, PlatformDbContext dbContext, PasswordHashingService passwordHashingService, PasswordPolicyService passwordPolicyService, CancellationToken cancellationToken) =>
            {
                var errors = ValidateResetPasswordRequest(request, passwordPolicyService);
                var user = await dbContext.ApplicationUsers
                    .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

                if (user is null)
                {
                    return Results.NotFound();
                }

                if (errors.Count > 0)
                {
                    return Results.ValidationProblem(errors);
                }

                var previousAccessFailedCount = user.AccessFailedCount;
                var previousLockoutEndUtc = user.LockoutEndUtc;
                user.ResetPassword(passwordHashingService.HashPassword(request.NewPassword));
                dbContext.AuditEvents.Add(
                    CreateUserAuditEvent(
                        principal,
                        user,
                        actionType: "USER_PASSWORD_RESET",
                        title: "Password restablecido por administrador",
                        detail: $"El password del usuario '{user.UserName}' fue restablecido por un administrador.",
                        metadata: new
                        {
                            targetUserId = user.Id,
                            targetUserName = user.UserName,
                            previousAccessFailedCount,
                            previousLockoutEndUtc,
                            lockoutCleared = previousAccessFailedCount > 0 || previousLockoutEndUtc is not null
                        }));

                await dbContext.SaveChangesAsync(cancellationToken);
                return Results.Ok(BuildResponse(user));
            });

        group.MapPost(
            "/{userId:guid}/unlock",
            async (Guid userId, ClaimsPrincipal principal, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var user = await dbContext.ApplicationUsers
                    .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

                if (user is null)
                {
                    return Results.NotFound();
                }

                var previousAccessFailedCount = user.AccessFailedCount;
                var previousLockoutEndUtc = user.LockoutEndUtc;
                var changed = user.ClearAccessLockout();

                if (changed)
                {
                    dbContext.AuditEvents.Add(
                        CreateUserAuditEvent(
                            principal,
                            user,
                            actionType: "USER_LOCKOUT_RESET",
                            title: "Lockout de usuario limpiado por administrador",
                            detail: $"El lockout del usuario '{user.UserName}' fue limpiado por un administrador.",
                            metadata: new
                            {
                                targetUserId = user.Id,
                                targetUserName = user.UserName,
                                previousAccessFailedCount,
                                previousLockoutEndUtc
                            }));
                }

                await dbContext.SaveChangesAsync(cancellationToken);
                return Results.Ok(BuildResponse(user));
            });

        return app;
    }

    private static ApplicationUserAdminResponse BuildResponse(ApplicationUser user)
    {
        var now = DateTimeOffset.UtcNow;

        return new ApplicationUserAdminResponse(
            user.Id,
            user.UserName,
            user.DisplayName,
            user.RoleCode,
            user.IsActive,
            user.CreatedUtc,
            user.UpdatedUtc,
            user.LastLoginUtc,
            user.AccessFailedCount,
            user.LockoutEndUtc,
            user.IsLockedOut(now));
    }

    private static Dictionary<string, string[]> ValidateCreateRequest(CreateApplicationUserRequest request, PasswordPolicyService passwordPolicyService)
    {
        var errors = ValidateCommonProfileFields(request.UserName, request.DisplayName, request.RoleCode);

        var passwordErrors = passwordPolicyService.Validate(request.Password, "Password");
        if (passwordErrors.Count > 0)
        {
            errors["password"] = passwordErrors.ToArray();
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateRoleRequest(ChangeApplicationUserRoleRequest request)
    {
        return ValidateRoleCode(request.RoleCode);
    }

    private static Dictionary<string, string[]> ValidateResetPasswordRequest(ResetApplicationUserPasswordRequest request, PasswordPolicyService passwordPolicyService)
    {
        var errors = new Dictionary<string, string[]>();
        var passwordErrors = passwordPolicyService.Validate(request.NewPassword, "NewPassword");
        if (passwordErrors.Count > 0)
        {
            errors["newPassword"] = passwordErrors.ToArray();
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateCommonProfileFields(string? userName, string? displayName, string? roleCode)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(userName))
        {
            errors["userName"] = ["UserName is required."];
        }
        else if (userName.Trim().Length > MaximumUserNameLength)
        {
            errors["userName"] = [$"UserName must contain at most {MaximumUserNameLength} characters."];
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            errors["displayName"] = ["DisplayName is required."];
        }
        else if (displayName.Trim().Length > MaximumDisplayNameLength)
        {
            errors["displayName"] = [$"DisplayName must contain at most {MaximumDisplayNameLength} characters."];
        }

        foreach (var pair in ValidateRoleCode(roleCode))
        {
            errors[pair.Key] = pair.Value;
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateRoleCode(string? roleCode)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(roleCode))
        {
            errors["roleCode"] = ["RoleCode is required."];
            return errors;
        }

        try
        {
            _ = ApplicationRoleCodes.Normalize(roleCode);
        }
        catch (ArgumentOutOfRangeException)
        {
            errors["roleCode"] = ["RoleCode is not supported."];
        }

        return errors;
    }

    private static async Task<bool> WouldLeaveSystemWithoutActiveAdminAsync(
        PlatformDbContext dbContext,
        ApplicationUser targetUser,
        string nextRoleCode,
        bool nextIsActive,
        CancellationToken cancellationToken)
    {
        var isCurrentlyActiveAdmin = targetUser.IsActive
            && string.Equals(targetUser.RoleCode, ApplicationRoleCodes.Admin, StringComparison.Ordinal);
        var remainsActiveAdmin = nextIsActive
            && string.Equals(nextRoleCode, ApplicationRoleCodes.Admin, StringComparison.Ordinal);

        if (!isCurrentlyActiveAdmin || remainsActiveAdmin)
        {
            return false;
        }

        var anotherActiveAdminExists = await dbContext.ApplicationUsers
            .AsNoTracking()
            .AnyAsync(
                item => item.Id != targetUser.Id
                    && item.IsActive
                    && item.RoleCode == ApplicationRoleCodes.Admin,
                cancellationToken);

        return !anotherActiveAdminExists;
    }

    private static AuditEvent CreateUserAuditEvent(
        ClaimsPrincipal principal,
        ApplicationUser targetUser,
        string actionType,
        string title,
        string detail,
        object metadata)
    {
        var actorUserIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var actorUserName = principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? "unknown";

        return AuditEventSupport.CreateAuditEvent(
            SecurityModuleCode,
            SecurityModuleName,
            ApplicationUserEntityType,
            targetUser.Id,
            actionType,
            title,
            $"{detail} Actor: {actorUserName}.",
            relatedStatusCode: null,
            reference: targetUser.UserName,
            UserAdministrationNavigationPath,
            metadata: new
            {
                actorUserId = actorUserIdClaim,
                actorUserName,
                targetUserId = targetUser.Id,
                targetUserName = targetUser.UserName,
                payload = metadata
            });
    }
}
