using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FMCPA.Domain.Entities.Security;
using Microsoft.IdentityModel.Tokens;

namespace FMCPA.Api.Auth;

public sealed class JwtTokenIssuer
{
    private readonly JwtAuthenticationSettings _settings;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenIssuer(JwtAuthenticationSettings settings)
    {
        _settings = settings;
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public IssuedJwtToken Issue(ApplicationUser user)
    {
        var issuedAtUtc = DateTimeOffset.UtcNow;
        var expiresAtUtc = issuedAtUtc.Add(_settings.TokenLifetime);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(JwtRegisteredClaimNames.Name, user.DisplayName),
                new Claim(ClaimTypes.Role, user.RoleCode),
                new Claim("fmcpa_security_stamp", user.SecurityStamp),
                new Claim("fmcpa_expires_at", expiresAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ],
            notBefore: issuedAtUtc.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: _signingCredentials);

        return new IssuedJwtToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAtUtc);
    }
}

public sealed record IssuedJwtToken(string AccessToken, DateTimeOffset ExpiresAtUtc);
