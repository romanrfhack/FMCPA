namespace FMCPA.Api.Contracts.AdminSecurity;

public sealed record SecurityActivitySummaryResponse(
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset SinceUtc,
    int RecentSecurityEventCount,
    int LoginSucceededCount,
    int LoginFailedCount,
    int LockoutDeniedCount,
    int TemporaryLockoutCount,
    int LockoutResetCount,
    int PasswordResetCount,
    int RoleChangedCount,
    int UserDeactivatedCount,
    int UserReactivatedCount,
    int ActiveLockedUserCount,
    int UsersWithFailedAttemptsCount);

public sealed record SecurityAuditEventResponse(
    Guid Id,
    DateTimeOffset OccurredUtc,
    string ActionType,
    string Title,
    string Detail,
    string EntityType,
    string EntityId,
    string? Reference,
    string NavigationPath,
    string? MetadataJson);

public sealed record LockedApplicationUserResponse(
    Guid Id,
    string UserName,
    string DisplayName,
    string RoleCode,
    bool IsActive,
    int AccessFailedCount,
    DateTimeOffset? LockoutEndUtc,
    DateTimeOffset? LastLoginUtc);
