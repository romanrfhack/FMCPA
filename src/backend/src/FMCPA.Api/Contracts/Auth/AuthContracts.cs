namespace FMCPA.Api.Contracts.Auth;

public sealed record LoginRequest(string UserName, string Password);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);

public sealed record AuthenticatedUserResponse(
    Guid Id,
    string UserName,
    string DisplayName,
    string RoleCode,
    IReadOnlyList<string> Permissions);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAtUtc,
    AuthenticatedUserResponse User);

public sealed record CurrentSessionResponse(
    AuthenticatedUserResponse User,
    DateTimeOffset ExpiresAtUtc);

public sealed record ChangePasswordResponse(string Message);
