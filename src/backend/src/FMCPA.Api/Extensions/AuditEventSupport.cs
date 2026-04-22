using System.Text.Json;
using FMCPA.Domain.Entities.Audit;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Extensions;

internal static class AuditEventSupport
{
    public const string FormalCloseActionType = "CLOSED";
    public const string LegacyCloseNormalizedActionType = "LEGACY_CLOSE_NORMALIZED";

    public static AuditEvent CreateAuditEvent(
        string moduleCode,
        string moduleName,
        string entityType,
        Guid entityId,
        string actionType,
        string title,
        string detail,
        string? relatedStatusCode,
        string? reference,
        string navigationPath,
        bool isCloseEvent = false,
        object? metadata = null,
        DateTimeOffset? occurredUtc = null)
    {
        return new AuditEvent(
            moduleCode,
            moduleName,
            entityType,
            entityId.ToString(),
            actionType,
            title,
            detail,
            relatedStatusCode,
            reference,
            navigationPath,
            isCloseEvent,
            SerializeMetadata(metadata),
            occurredUtc);
    }

    public static async Task<ModuleStatusCatalogEntry?> ResolveContextStatusByCodeAsync(
        PlatformDbContext dbContext,
        string moduleCode,
        string contextCode,
        string statusCode,
        CancellationToken cancellationToken)
    {
        var normalizedModuleCode = moduleCode.Trim().ToUpperInvariant();
        var normalizedContextCode = contextCode.Trim().ToUpperInvariant();
        var normalizedStatusCode = statusCode.Trim().ToUpperInvariant();

        return await dbContext.ModuleStatusCatalogEntries
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.ModuleCode == normalizedModuleCode
                        && item.ContextCode == normalizedContextCode
                        && item.StatusCode == normalizedStatusCode,
                cancellationToken);
    }

    public static async Task<bool> HasCloseEventAsync(
        PlatformDbContext dbContext,
        string moduleCode,
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var normalizedModuleCode = moduleCode.Trim().ToUpperInvariant();
        var normalizedEntityType = entityType.Trim().ToUpperInvariant();
        var entityIdText = entityId.ToString();

        return await dbContext.AuditEvents
            .AsNoTracking()
            .AnyAsync(
                item => item.IsCloseEvent
                        && item.ModuleCode == normalizedModuleCode
                        && item.EntityType == normalizedEntityType
                        && item.EntityId == entityIdText,
                cancellationToken);
    }

    public static bool IsLegacyNormalizedCloseAction(string? actionType)
    {
        return string.Equals(actionType, LegacyCloseNormalizedActionType, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsFormalCloseAction(string? actionType)
    {
        return string.Equals(actionType, FormalCloseActionType, StringComparison.OrdinalIgnoreCase);
    }

    private static string? SerializeMetadata(object? metadata)
    {
        if (metadata is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(
            metadata,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
    }
}
