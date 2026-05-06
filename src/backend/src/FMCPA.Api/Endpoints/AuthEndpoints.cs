using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Security.Claims;
using FMCPA.Api.Auth;
using FMCPA.Api.Contracts.Auth;
using FMCPA.Api.Extensions;
using FMCPA.Domain.Entities.Audit;
using FMCPA.Domain.Entities.Security;
using FMCPA.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class AuthEndpoints
{
    private const string SecurityModuleCode = "SECURITY";
    private const string SecurityModuleName = "Seguridad";
    private const string ApplicationUserEntityType = "APPLICATION_USER";
    private const string LoginNavigationPath = "/login";
    private const string AccountPasswordNavigationPath = "/account/password";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost(
                "/login",
                async (LoginRequest request, HttpContext httpContext, PlatformDbContext dbContext, PasswordHashingService passwordHashingService, CredentialSecuritySettings credentialSecuritySettings, JwtTokenIssuer jwtTokenIssuer, CancellationToken cancellationToken) =>
                {
                    var errors = ValidateLoginRequest(request);
                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    var normalizedUserName = ApplicationUser.NormalizeUserName(request.UserName);
                    var user = await dbContext.ApplicationUsers
                        .SingleOrDefaultAsync(item => item.NormalizedUserName == normalizedUserName, cancellationToken);

                    var now = DateTimeOffset.UtcNow;
                    if (user is null)
                    {
                        dbContext.AuditEvents.Add(
                            CreateAuthAuditEvent(
                                user: null,
                                requestedUserName: request.UserName,
                                httpContext,
                                actionType: "AUTH_LOGIN_FAILED",
                                title: "Login fallido",
                                detail: "Intento de login fallido para usuario no encontrado.",
                                metadata: new { reason = "UNKNOWN_USER" }));

                        await dbContext.SaveChangesAsync(cancellationToken);
                        return InvalidCredentialsResult();
                    }

                    var lockoutExpired = user.ReleaseExpiredLockout(now);
                    if (lockoutExpired)
                    {
                        dbContext.AuditEvents.Add(
                            CreateAuthAuditEvent(
                                user,
                                request.UserName,
                                httpContext,
                                actionType: "USER_LOCKOUT_EXPIRED",
                                title: "Lockout de usuario expirado",
                                detail: $"El bloqueo temporal del usuario '{user.UserName}' expiró automáticamente.",
                                metadata: new { unlockedAtUtc = now }));
                    }

                    if (!user.IsActive)
                    {
                        dbContext.AuditEvents.Add(
                            CreateAuthAuditEvent(
                                user,
                                request.UserName,
                                httpContext,
                                actionType: "AUTH_LOGIN_FAILED",
                                title: "Login fallido",
                                detail: $"Intento de login fallido para usuario inactivo '{user.UserName}'.",
                                metadata: new { reason = "INACTIVE_USER" }));

                        await dbContext.SaveChangesAsync(cancellationToken);
                        return InvalidCredentialsResult();
                    }

                    if (user.IsLockedOut(now))
                    {
                        dbContext.AuditEvents.Add(
                            CreateAuthAuditEvent(
                                user,
                                request.UserName,
                                httpContext,
                                actionType: "AUTH_LOGIN_LOCKOUT_DENIED",
                                title: "Login rechazado por lockout",
                                detail: $"Intento de login rechazado porque el usuario '{user.UserName}' tiene bloqueo temporal activo.",
                                metadata: new
                                {
                                    reason = "LOCKED_OUT",
                                    user.AccessFailedCount,
                                    user.LockoutEndUtc
                                }));

                        await dbContext.SaveChangesAsync(cancellationToken);
                        return LockedOutResult(httpContext, user.LockoutEndUtc, now);
                    }

                    if (!passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
                    {
                        var lockoutStarted = user.RecordFailedLogin(
                            now,
                            credentialSecuritySettings.Lockout.MaxFailedAccessAttempts,
                            credentialSecuritySettings.Lockout.Cooldown);

                        dbContext.AuditEvents.Add(
                            CreateAuthAuditEvent(
                                user,
                                request.UserName,
                                httpContext,
                                actionType: "AUTH_LOGIN_FAILED",
                                title: "Login fallido",
                                detail: $"Intento de login fallido para usuario '{user.UserName}'.",
                                metadata: new
                                {
                                    reason = "INVALID_PASSWORD",
                                    user.AccessFailedCount,
                                    user.LockoutEndUtc,
                                    lockoutStarted
                                }));

                        if (lockoutStarted)
                        {
                            dbContext.AuditEvents.Add(
                                CreateAuthAuditEvent(
                                    user,
                                    request.UserName,
                                    httpContext,
                                    actionType: "USER_TEMPORARILY_LOCKED",
                                    title: "Usuario bloqueado temporalmente",
                                    detail: $"El usuario '{user.UserName}' fue bloqueado temporalmente por intentos fallidos de login.",
                                    metadata: new
                                    {
                                        credentialSecuritySettings.Lockout.MaxFailedAccessAttempts,
                                        credentialSecuritySettings.Lockout.CooldownSeconds,
                                        user.LockoutEndUtc
                                    }));
                        }

                        await dbContext.SaveChangesAsync(cancellationToken);

                        if (lockoutStarted)
                        {
                            return LockedOutResult(httpContext, user.LockoutEndUtc, now);
                        }

                        return Results.Json(
                            new { message = "Credenciales inválidas." },
                            statusCode: StatusCodes.Status401Unauthorized);
                    }

                    user.RecordSuccessfulLogin();
                    dbContext.AuditEvents.Add(
                        CreateAuthAuditEvent(
                            user,
                            request.UserName,
                            httpContext,
                            actionType: "AUTH_LOGIN_SUCCEEDED",
                            title: "Login exitoso",
                            detail: $"Login exitoso para usuario '{user.UserName}'.",
                            metadata: new { user.RoleCode }));

                    await dbContext.SaveChangesAsync(cancellationToken);

                    var issuedToken = jwtTokenIssuer.Issue(user);
                    return Results.Ok(
                        new LoginResponse(
                            issuedToken.AccessToken,
                            "Bearer",
                            issuedToken.ExpiresAtUtc,
                            BuildUserResponse(user)));
                })
            .RequireRateLimiting(PlatformRateLimitingPolicies.AuthLogin);

        group.MapGet(
                "/session",
                async (ClaimsPrincipal principal, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
                {
                    var userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
                    if (!Guid.TryParse(userIdClaim, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    var user = await dbContext.ApplicationUsers
                        .AsNoTracking()
                        .SingleOrDefaultAsync(item => item.Id == userId && item.IsActive, cancellationToken);

                    if (user is null)
                    {
                        return Results.Unauthorized();
                    }

                    var expiresAtUtc = ResolveExpiration(principal);
                    if (expiresAtUtc is null)
                    {
                        return Results.Unauthorized();
                    }

                    return Results.Ok(new CurrentSessionResponse(BuildUserResponse(user), expiresAtUtc.Value));
                })
            .RequireAuthorization();

        group.MapPost(
                "/change-password",
                async (ChangePasswordRequest request, ClaimsPrincipal principal, HttpContext httpContext, PlatformDbContext dbContext, PasswordHashingService passwordHashingService, PasswordPolicyService passwordPolicyService, CancellationToken cancellationToken) =>
                {
                    var userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
                    if (!Guid.TryParse(userIdClaim, out var userId))
                    {
                        return Results.Unauthorized();
                    }

                    var user = await dbContext.ApplicationUsers
                        .SingleOrDefaultAsync(item => item.Id == userId && item.IsActive, cancellationToken);

                    if (user is null)
                    {
                        return Results.Unauthorized();
                    }

                    var errors = ValidateChangePasswordRequest(request, passwordPolicyService);
                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    if (!passwordHashingService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
                    {
                        dbContext.AuditEvents.Add(
                            CreateAuthAuditEvent(
                                user,
                                user.UserName,
                                httpContext,
                                actionType: "AUTH_PASSWORD_CHANGE_FAILED",
                                title: "Cambio de password fallido",
                                detail: $"Intento fallido de cambio de password self-service para usuario '{user.UserName}'.",
                                metadata: new { reason = "INVALID_CURRENT_PASSWORD" },
                                navigationPath: AccountPasswordNavigationPath));

                        await dbContext.SaveChangesAsync(cancellationToken);
                        return Results.ValidationProblem(
                            new Dictionary<string, string[]>
                            {
                                ["currentPassword"] = ["Current password is invalid."]
                            });
                    }

                    user.ResetPassword(passwordHashingService.HashPassword(request.NewPassword));
                    dbContext.AuditEvents.Add(
                        CreateAuthAuditEvent(
                            user,
                            user.UserName,
                            httpContext,
                            actionType: "AUTH_PASSWORD_CHANGED_SELF_SERVICE",
                            title: "Password actualizado por usuario",
                            detail: $"El usuario '{user.UserName}' actualizó su password desde self-service.",
                            metadata: new
                            {
                                targetUserId = user.Id,
                                targetUserName = user.UserName,
                                securityStampRotated = true
                            },
                            navigationPath: AccountPasswordNavigationPath));

                    await dbContext.SaveChangesAsync(cancellationToken);

                    return Results.Ok(new ChangePasswordResponse("Password actualizado. Inicia sesión nuevamente."));
                })
            .RequireAuthorization();

        return app;
    }

    private static IResult InvalidCredentialsResult()
    {
        return Results.Json(
            new { message = "Credenciales inválidas." },
            statusCode: StatusCodes.Status401Unauthorized);
    }

    private static IResult LockedOutResult(HttpContext httpContext, DateTimeOffset? lockoutEndUtc, DateTimeOffset now)
    {
        var retryAfterSeconds = ResolveRetryAfterSeconds(lockoutEndUtc, now);
        httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

        return Results.Json(
            new
            {
                title = "Cuenta temporalmente bloqueada.",
                message = "La cuenta está bloqueada temporalmente. Intenta de nuevo más tarde.",
                lockedUntilUtc = lockoutEndUtc,
                retryAfterSeconds
            },
            statusCode: StatusCodes.Status423Locked);
    }

    private static AuthenticatedUserResponse BuildUserResponse(ApplicationUser user)
    {
        return new AuthenticatedUserResponse(
            user.Id,
            user.UserName,
            user.DisplayName,
            user.RoleCode,
            PlatformPermissionCodes.ForRole(user.RoleCode));
    }

    private static DateTimeOffset? ResolveExpiration(ClaimsPrincipal principal)
    {
        var expirationClaim = principal.FindFirstValue("fmcpa_expires_at");
        return long.TryParse(expirationClaim, out var expSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(expSeconds)
            : null;
    }

    private static AuditEvent CreateAuthAuditEvent(
        ApplicationUser? user,
        string requestedUserName,
        HttpContext httpContext,
        string actionType,
        string title,
        string detail,
        object metadata,
        string navigationPath = LoginNavigationPath)
    {
        var requestedUserNameReference = NormalizeAuditText(requestedUserName, fallback: "unknown");
        return AuditEventSupport.CreateAuditEvent(
            SecurityModuleCode,
            SecurityModuleName,
            ApplicationUserEntityType,
            user?.Id ?? Guid.Empty,
            actionType,
            title,
            detail,
            relatedStatusCode: null,
            reference: user?.UserName ?? requestedUserNameReference,
            navigationPath,
            metadata: new
            {
                targetUserId = user?.Id,
                targetUserName = user?.UserName,
                requestedUserName = requestedUserNameReference,
                remoteIp = httpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent = Truncate(NormalizeAuditText(httpContext.Request.Headers.UserAgent.ToString(), fallback: "unknown"), 256),
                payload = metadata
            });
    }

    private static int ResolveRetryAfterSeconds(DateTimeOffset? lockoutEndUtc, DateTimeOffset now)
    {
        if (lockoutEndUtc is null || lockoutEndUtc <= now)
        {
            return 1;
        }

        return Math.Max(1, (int)Math.Ceiling((lockoutEndUtc.Value - now).TotalSeconds));
    }

    private static string NormalizeAuditText(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static Dictionary<string, string[]> ValidateChangePasswordRequest(ChangePasswordRequest request, PasswordPolicyService passwordPolicyService)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            errors["currentPassword"] = ["CurrentPassword is required."];
        }

        var passwordErrors = passwordPolicyService.Validate(request.NewPassword, "NewPassword");
        if (passwordErrors.Count > 0)
        {
            errors["newPassword"] = passwordErrors.ToArray();
        }

        if (string.IsNullOrWhiteSpace(request.ConfirmNewPassword))
        {
            errors["confirmNewPassword"] = ["ConfirmNewPassword is required."];
        }
        else if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            errors["confirmNewPassword"] = ["ConfirmNewPassword must match NewPassword."];
        }

        if (!string.IsNullOrWhiteSpace(request.CurrentPassword)
            && !string.IsNullOrWhiteSpace(request.NewPassword)
            && string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            errors["newPassword"] = ["NewPassword must be different from the current password."];
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateLoginRequest(LoginRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            errors["userName"] = ["UserName is required."];
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors["password"] = ["Password is required."];
        }

        return errors;
    }
}
