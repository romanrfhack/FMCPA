namespace FMCPA.Api.Contracts.AdminUsers;

public sealed record ApplicationUserAdminResponse(
    Guid Id,
    string UserName,
    string DisplayName,
    string RoleCode,
    bool IsActive,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc,
    DateTimeOffset? LastLoginUtc);

public sealed record CreateApplicationUserRequest(
    string UserName,
    string DisplayName,
    string RoleCode,
    string Password);

public sealed record ChangeApplicationUserRoleRequest(string RoleCode);

public sealed record SetApplicationUserActivationRequest(bool IsActive);

public sealed record ResetApplicationUserPasswordRequest(string NewPassword);
