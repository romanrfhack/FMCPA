using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Auth;

public sealed class ApplicationUserTokenValidationService
{
    private const string SecurityStampClaimType = "fmcpa_security_stamp";

    private readonly PlatformDbContext _dbContext;

    public ApplicationUserTokenValidationService(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> ValidateAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userIdClaim = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return "The application user subject claim is invalid.";
        }

        var securityStamp = principal.FindFirstValue(SecurityStampClaimType);
        if (string.IsNullOrWhiteSpace(securityStamp))
        {
            return "The application user security stamp claim is missing.";
        }

        var user = await _dbContext.ApplicationUsers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return "The application user is no longer active.";
        }

        var roleCode = principal.FindFirstValue(ClaimTypes.Role);
        if (!string.Equals(user.RoleCode, roleCode, StringComparison.Ordinal))
        {
            return "The application user role claim is no longer valid.";
        }

        if (!string.Equals(user.SecurityStamp, securityStamp, StringComparison.Ordinal))
        {
            return "The application user session is no longer valid.";
        }

        var currentPermissionClaims = principal
            .FindAll(PlatformPermissionCodes.ClaimType)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.Ordinal);
        var expectedPermissions = PlatformPermissionCodes.ForRole(user.RoleCode);

        if (!currentPermissionClaims.SetEquals(expectedPermissions))
        {
            return "The application user permission claims are no longer valid.";
        }

        return null;
    }
}
