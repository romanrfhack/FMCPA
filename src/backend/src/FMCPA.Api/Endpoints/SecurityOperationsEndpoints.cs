using FMCPA.Api.Auth;
using FMCPA.Api.Contracts.AdminSecurity;
using FMCPA.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class SecurityOperationsEndpoints
{
    private const string SecurityModuleCode = "SECURITY";
    private const int DefaultEventTake = 50;
    private const int MaximumEventTake = 200;
    private const int DefaultSummaryHours = 24;
    private const int MaximumSummaryHours = 720;

    public static IEndpointRouteBuilder MapSecurityOperationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/security")
            .WithTags("Security Operations")
            .RequireUsersAdminAccess()
            .RequireRateLimiting(PlatformRateLimitingPolicies.SensitiveAdmin);

        group.MapGet(
            "/summary",
            async (int? hours, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await BuildSecurityActivitySummaryAsync(dbContext, hours, cancellationToken));
            });

        group.MapGet(
            "/events",
            async (Guid? userId, string? userName, string? eventType, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, int? take, PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await BuildSecurityAuditEventsAsync(
                    dbContext,
                    userId,
                    userName,
                    eventType,
                    fromUtc,
                    toUtc,
                    take,
                    cancellationToken));
            });

        group.MapGet(
            "/locked-users",
            async (PlatformDbContext dbContext, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await BuildLockedUsersAsync(dbContext, cancellationToken));
            });

        return app;
    }

    internal static async Task<SecurityActivitySummaryResponse> BuildSecurityActivitySummaryAsync(
        PlatformDbContext dbContext,
        int? hours,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var sinceUtc = now.AddHours(-NormalizeHours(hours));
        var recentEvents = dbContext.AuditEvents
            .AsNoTracking()
            .Where(item => item.ModuleCode == SecurityModuleCode && item.OccurredUtc >= sinceUtc);

        var activeLockedUserCount = await dbContext.ApplicationUsers
            .AsNoTracking()
            .CountAsync(item => item.LockoutEndUtc != null && item.LockoutEndUtc > now, cancellationToken);
        var usersWithFailedAttemptsCount = await dbContext.ApplicationUsers
            .AsNoTracking()
            .CountAsync(item => item.AccessFailedCount > 0, cancellationToken);

        return new SecurityActivitySummaryResponse(
            now,
            sinceUtc,
            await recentEvents.CountAsync(cancellationToken),
            await CountActionAsync(recentEvents, "AUTH_LOGIN_SUCCEEDED", cancellationToken),
            await CountActionAsync(recentEvents, "AUTH_LOGIN_FAILED", cancellationToken),
            await CountActionAsync(recentEvents, "AUTH_LOGIN_LOCKOUT_DENIED", cancellationToken),
            await CountActionAsync(recentEvents, "USER_TEMPORARILY_LOCKED", cancellationToken),
            await CountActionAsync(recentEvents, "USER_LOCKOUT_RESET", cancellationToken),
            await CountActionAsync(recentEvents, "USER_PASSWORD_RESET", cancellationToken),
            await CountActionAsync(recentEvents, "USER_ROLE_CHANGED", cancellationToken),
            await CountActionAsync(recentEvents, "USER_DEACTIVATED", cancellationToken),
            await CountActionAsync(recentEvents, "USER_ACTIVATED", cancellationToken),
            activeLockedUserCount,
            usersWithFailedAttemptsCount);
    }

    internal static async Task<IReadOnlyList<SecurityAuditEventResponse>> BuildSecurityAuditEventsAsync(
        PlatformDbContext dbContext,
        Guid? userId,
        string? userName,
        string? eventType,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        int? take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.AuditEvents
            .AsNoTracking()
            .Where(item => item.ModuleCode == SecurityModuleCode);

        if (userId is not null)
        {
            var userIdText = userId.Value.ToString();
            query = query.Where(item => item.EntityId == userIdText);
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            var normalizedUserName = userName.Trim().ToUpperInvariant();
            query = query.Where(item => item.Reference != null && item.Reference.ToUpper() == normalizedUserName);
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var normalizedEventType = eventType.Trim().ToUpperInvariant();
            query = query.Where(item => item.ActionType == normalizedEventType);
        }

        if (fromUtc is not null)
        {
            query = query.Where(item => item.OccurredUtc >= fromUtc.Value);
        }

        if (toUtc is not null)
        {
            query = query.Where(item => item.OccurredUtc <= toUtc.Value);
        }

        return await query
            .OrderByDescending(item => item.OccurredUtc)
            .Take(NormalizeTake(take))
            .Select(item => new SecurityAuditEventResponse(
                item.Id,
                item.OccurredUtc,
                item.ActionType,
                item.Title,
                item.Detail,
                item.EntityType,
                item.EntityId,
                item.Reference,
                item.NavigationPath,
                item.MetadataJson))
            .ToListAsync(cancellationToken);
    }

    internal static async Task<IReadOnlyList<LockedApplicationUserResponse>> BuildLockedUsersAsync(
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await dbContext.ApplicationUsers
            .AsNoTracking()
            .Where(item => item.LockoutEndUtc != null && item.LockoutEndUtc > now)
            .OrderByDescending(item => item.LockoutEndUtc)
            .ThenBy(item => item.UserName)
            .Select(item => new LockedApplicationUserResponse(
                item.Id,
                item.UserName,
                item.DisplayName,
                item.RoleCode,
                item.IsActive,
                item.AccessFailedCount,
                item.LockoutEndUtc,
                item.LastLoginUtc))
            .ToListAsync(cancellationToken);
    }

    private static async Task<int> CountActionAsync(
        IQueryable<FMCPA.Domain.Entities.Audit.AuditEvent> events,
        string actionType,
        CancellationToken cancellationToken)
    {
        return await events.CountAsync(item => item.ActionType == actionType, cancellationToken);
    }

    private static int NormalizeTake(int? take)
    {
        return Math.Clamp(take ?? DefaultEventTake, 1, MaximumEventTake);
    }

    private static int NormalizeHours(int? hours)
    {
        return Math.Clamp(hours ?? DefaultSummaryHours, 1, MaximumSummaryHours);
    }
}
