namespace FMCPA.Api.Contracts.Auth;

public sealed record LoginRequest(string UserName, string Password);

public sealed record AuthenticatedUserResponse(Guid Id, string UserName, string DisplayName, string RoleCode);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    AuthenticatedUserResponse User);

public sealed record CurrentSessionResponse(
    AuthenticatedUserResponse User,
    DateTimeOffset ExpiresAtUtc);
