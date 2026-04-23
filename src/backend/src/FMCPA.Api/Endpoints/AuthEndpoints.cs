using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FMCPA.Api.Auth;
using FMCPA.Api.Contracts.Auth;
using FMCPA.Domain.Entities.Security;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost(
                "/login",
                async (LoginRequest request, PlatformDbContext dbContext, PasswordHashingService passwordHashingService, JwtTokenIssuer jwtTokenIssuer, CancellationToken cancellationToken) =>
                {
                    var errors = ValidateLoginRequest(request);
                    if (errors.Count > 0)
                    {
                        return Results.ValidationProblem(errors);
                    }

                    var normalizedUserName = ApplicationUser.NormalizeUserName(request.UserName);
                    var user = await dbContext.ApplicationUsers
                        .SingleOrDefaultAsync(item => item.NormalizedUserName == normalizedUserName, cancellationToken);

                    if (user is null
                        || !user.IsActive
                        || !passwordHashingService.VerifyPassword(request.Password, user.PasswordHash))
                    {
                        return Results.Json(
                            new { message = "Credenciales inválidas." },
                            statusCode: StatusCodes.Status401Unauthorized);
                    }

                    user.RecordSuccessfulLogin();
                    await dbContext.SaveChangesAsync(cancellationToken);

                    var issuedToken = jwtTokenIssuer.Issue(user);
                    return Results.Ok(
                        new LoginResponse(
                            issuedToken.AccessToken,
                            "Bearer",
                            issuedToken.ExpiresAtUtc,
                            BuildUserResponse(user)));
                });

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
            .RequireReadAccess();

        return app;
    }

    private static AuthenticatedUserResponse BuildUserResponse(ApplicationUser user)
    {
        return new AuthenticatedUserResponse(user.Id, user.UserName, user.DisplayName, user.RoleCode);
    }

    private static DateTimeOffset? ResolveExpiration(ClaimsPrincipal principal)
    {
        var expirationClaim = principal.FindFirstValue("fmcpa_expires_at");
        return long.TryParse(expirationClaim, out var expSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(expSeconds)
            : null;
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
