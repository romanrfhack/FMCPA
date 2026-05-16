using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FMCPA.Api.Auth;
using FMCPA.Api.Contracts.Shared;
using FMCPA.Domain.Entities.Shared;
using FMCPA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FMCPA.Api.Endpoints;

public static class ContactInterventionsEndpoints
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 100;
    private const int MaxModuleKeyLength = 50;
    private const int MaxOriginTypeLength = 100;
    private const int MaxOriginIdLength = 100;
    private const int MaxOriginDisplayNameLength = 250;
    private const int MaxSubjectLength = 250;
    private const int MaxHelpTypeLength = 50;
    private const int MaxOutcomeLength = 50;
    private const int MaxNotesLength = 2000;

    private static readonly IReadOnlyDictionary<string, ContactInterventionModuleAccess> ModuleAccess =
        new Dictionary<string, ContactInterventionModuleAccess>(StringComparer.OrdinalIgnoreCase)
        {
            ["MARKETS"] = new("MARKETS", PlatformPermissionCodes.MarketsRead, PlatformPermissionCodes.MarketsWrite),
            ["DONATARIAS"] = new("DONATARIAS", PlatformPermissionCodes.DonationsRead, PlatformPermissionCodes.DonationsWrite),
            ["FINANCIALS"] = new("FINANCIALS", PlatformPermissionCodes.FinancialsRead, PlatformPermissionCodes.FinancialsWrite),
            ["FEDERATION"] = new("FEDERATION", PlatformPermissionCodes.FederationRead, PlatformPermissionCodes.FederationWrite),
            ["DOCUMENTS"] = new("DOCUMENTS", PlatformPermissionCodes.UsersAdmin, PlatformPermissionCodes.UsersAdmin),
            ["OTHER"] = new("OTHER", PlatformPermissionCodes.ContactsRead, PlatformPermissionCodes.ContactsWrite)
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>> AllowedOriginTypesByModule =
        new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["MARKETS"] = new HashSet<string>(StringComparer.Ordinal)
            {
                "MARKET",
                "MARKET_TENANT",
                "MARKET_ISSUE"
            },
            ["DONATARIAS"] = new HashSet<string>(StringComparer.Ordinal)
            {
                "DONATION",
                "DONATION_APPLICATION",
                "DONATION_APPLICATION_EVIDENCE"
            },
            ["FINANCIALS"] = new HashSet<string>(StringComparer.Ordinal)
            {
                "FINANCIAL_PERMIT",
                "FINANCIAL_CREDIT",
                "FINANCIAL_CREDIT_COMMISSION"
            },
            ["FEDERATION"] = new HashSet<string>(StringComparer.Ordinal)
            {
                "FEDERATION_ACTION",
                "FEDERATION_ACTION_PARTICIPANT",
                "FEDERATION_DONATION",
                "FEDERATION_DONATION_APPLICATION",
                "FEDERATION_DONATION_APPLICATION_EVIDENCE",
                "FEDERATION_DONATION_APPLICATION_COMMISSION"
            },
            ["DOCUMENTS"] = new HashSet<string>(StringComparer.Ordinal)
            {
                "STORED_DOCUMENT",
                "DOCUMENT_REVIEW"
            },
            ["OTHER"] = new HashSet<string>(StringComparer.Ordinal)
            {
                "OTHER"
            }
        };

    public static IEndpointRouteBuilder MapContactInterventionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Contact Interventions")
            .RequireContactsReadAccess();

        group.MapGet(
            "/contacts/{contactId:guid}/interventions",
            ListByContactAsync);

        group.MapPost(
            "/contacts/{contactId:guid}/interventions",
            CreateAsync);

        group.MapGet(
            "/contact-interventions",
            ListByOriginAsync);

        return app;
    }

    private static async Task<IResult> ListByContactAsync(
        Guid contactId,
        string? moduleKey,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? limit,
        ClaimsPrincipal principal,
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var take = ValidateLimit(limit, errors);
        var normalizedModuleKey = NormalizeOptionalCode(moduleKey);

        if (normalizedModuleKey is not null && !ModuleAccess.ContainsKey(normalizedModuleKey))
        {
            errors["moduleKey"] = ["ModuleKey is not supported."];
        }

        if (from is not null && to is not null && from.Value > to.Value)
        {
            errors["from"] = ["From must be earlier than or equal to To."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var contactExists = await dbContext.Contacts
            .AsNoTracking()
            .AnyAsync(contact => contact.Id == contactId, cancellationToken);

        if (!contactExists)
        {
            return Results.NotFound();
        }

        var readableModuleKeys = ResolveReadableModuleKeys(principal);
        if (normalizedModuleKey is not null)
        {
            if (!readableModuleKeys.Contains(normalizedModuleKey, StringComparer.Ordinal))
            {
                return Results.Ok(Array.Empty<ContactInterventionResponse>());
            }

            readableModuleKeys = [normalizedModuleKey];
        }

        var query = dbContext.ContactInterventions
            .AsNoTracking()
            .Where(intervention => intervention.ContactId == contactId)
            .Where(intervention => intervention.ArchivedUtc == null)
            .Where(intervention => readableModuleKeys.Contains(intervention.ModuleKey));

        if (from is not null)
        {
            var fromUtc = from.Value.ToUniversalTime();
            query = query.Where(intervention => intervention.OccurredUtc >= fromUtc);
        }

        if (to is not null)
        {
            var toUtc = to.Value.ToUniversalTime();
            query = query.Where(intervention => intervention.OccurredUtc <= toUtc);
        }

        var response = await query
            .OrderByDescending(intervention => intervention.OccurredUtc)
            .ThenByDescending(intervention => intervention.CreatedUtc)
            .Take(take)
            .Select(intervention => new ContactInterventionResponse(
                intervention.Id,
                intervention.ContactId,
                intervention.Contact!.Name,
                intervention.Contact.ContactType == null ? null : intervention.Contact.ContactType.Name,
                intervention.ModuleKey,
                intervention.OriginType,
                intervention.OriginId,
                intervention.OriginDisplayName,
                intervention.Subject,
                intervention.HelpType,
                intervention.Outcome,
                intervention.Notes,
                intervention.OccurredUtc,
                intervention.CreatedByUserId,
                intervention.CreatedByUser == null ? null : intervention.CreatedByUser.DisplayName,
                intervention.CreatedUtc,
                intervention.UpdatedUtc,
                intervention.ArchivedUtc))
            .ToListAsync(cancellationToken);

        return Results.Ok(response);
    }

    private static async Task<IResult> CreateAsync(
        Guid contactId,
        CreateContactInterventionRequest request,
        ClaimsPrincipal principal,
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = ValidateCreateRequest(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var moduleKey = NormalizeRequiredCode(request.ModuleKey!);
        var originType = NormalizeRequiredCode(request.OriginType!);
        if (!HasWriteAccess(principal, moduleKey))
        {
            return Results.Forbid();
        }

        if (!TryResolveUserId(principal, out var createdByUserId))
        {
            return Results.Unauthorized();
        }

        var contact = await dbContext.Contacts
            .AsNoTracking()
            .Include(item => item.ContactType)
            .SingleOrDefaultAsync(item => item.Id == contactId, cancellationToken);

        if (contact is null)
        {
            return Results.NotFound();
        }

        var occurredUtc = (request.OccurredUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var createdUtc = DateTimeOffset.UtcNow;
        var intervention = new ContactIntervention(
            contactId,
            moduleKey,
            originType,
            request.OriginId!,
            request.OriginDisplayName!,
            request.Subject!,
            request.HelpType!,
            request.Outcome!,
            request.Notes,
            occurredUtc,
            createdByUserId,
            createdUtc);

        dbContext.ContactInterventions.Add(intervention);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new ContactInterventionResponse(
            intervention.Id,
            intervention.ContactId,
            contact.Name,
            contact.ContactType?.Name,
            intervention.ModuleKey,
            intervention.OriginType,
            intervention.OriginId,
            intervention.OriginDisplayName,
            intervention.Subject,
            intervention.HelpType,
            intervention.Outcome,
            intervention.Notes,
            intervention.OccurredUtc,
            intervention.CreatedByUserId,
            ResolvePrincipalDisplayName(principal),
            intervention.CreatedUtc,
            intervention.UpdatedUtc,
            intervention.ArchivedUtc);

        return Results.Created($"/api/contact-interventions/{intervention.Id}", response);
    }

    private static async Task<IResult> ListByOriginAsync(
        string? moduleKey,
        string? originType,
        string? originId,
        int? limit,
        ClaimsPrincipal principal,
        PlatformDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var take = ValidateLimit(limit, errors);
        ValidateModuleOrigin(moduleKey, originType, errors);
        ValidateRequiredLength(originId, "originId", MaxOriginIdLength, errors);

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var normalizedModuleKey = NormalizeRequiredCode(moduleKey!);
        var normalizedOriginType = NormalizeRequiredCode(originType!);
        if (!HasReadAccess(principal, normalizedModuleKey))
        {
            return Results.Forbid();
        }

        var normalizedOriginId = originId!.Trim();
        var response = await dbContext.ContactInterventions
            .AsNoTracking()
            .Where(intervention => intervention.ArchivedUtc == null)
            .Where(intervention =>
                intervention.ModuleKey == normalizedModuleKey
                && intervention.OriginType == normalizedOriginType
                && intervention.OriginId == normalizedOriginId)
            .OrderByDescending(intervention => intervention.OccurredUtc)
            .ThenByDescending(intervention => intervention.CreatedUtc)
            .Take(take)
            .Select(intervention => new ContactInterventionResponse(
                intervention.Id,
                intervention.ContactId,
                intervention.Contact!.Name,
                intervention.Contact.ContactType == null ? null : intervention.Contact.ContactType.Name,
                intervention.ModuleKey,
                intervention.OriginType,
                intervention.OriginId,
                intervention.OriginDisplayName,
                intervention.Subject,
                intervention.HelpType,
                intervention.Outcome,
                intervention.Notes,
                intervention.OccurredUtc,
                intervention.CreatedByUserId,
                intervention.CreatedByUser == null ? null : intervention.CreatedByUser.DisplayName,
                intervention.CreatedUtc,
                intervention.UpdatedUtc,
                intervention.ArchivedUtc))
            .ToListAsync(cancellationToken);

        return Results.Ok(response);
    }

    private static Dictionary<string, string[]> ValidateCreateRequest(CreateContactInterventionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        ValidateModuleOrigin(request.ModuleKey, request.OriginType, errors);
        ValidateRequiredLength(request.OriginId, "originId", MaxOriginIdLength, errors);
        ValidateRequiredLength(request.OriginDisplayName, "originDisplayName", MaxOriginDisplayNameLength, errors);
        ValidateRequiredLength(request.Subject, "subject", MaxSubjectLength, errors);
        ValidateAllowedCode(
            request.HelpType,
            "helpType",
            MaxHelpTypeLength,
            ContactInterventionHelpTypes.All,
            errors);
        ValidateAllowedCode(
            request.Outcome,
            "outcome",
            MaxOutcomeLength,
            ContactInterventionOutcomes.All,
            errors);
        ValidateOptionalLength(request.Notes, "notes", MaxNotesLength, errors);

        return errors;
    }

    private static void ValidateModuleOrigin(string? moduleKey, string? originType, Dictionary<string, string[]> errors)
    {
        ValidateRequiredLength(moduleKey, "moduleKey", MaxModuleKeyLength, errors);
        ValidateRequiredLength(originType, "originType", MaxOriginTypeLength, errors);

        if (errors.ContainsKey("moduleKey") || errors.ContainsKey("originType"))
        {
            return;
        }

        var normalizedModuleKey = NormalizeRequiredCode(moduleKey!);
        var normalizedOriginType = NormalizeRequiredCode(originType!);
        if (!ModuleAccess.ContainsKey(normalizedModuleKey))
        {
            errors["moduleKey"] = ["ModuleKey is not supported."];
            return;
        }

        if (!AllowedOriginTypesByModule.TryGetValue(normalizedModuleKey, out var allowedOriginTypes)
            || !allowedOriginTypes.Contains(normalizedOriginType))
        {
            errors["originType"] = ["OriginType is not supported for the selected module."];
        }
    }

    private static void ValidateAllowedCode(
        string? value,
        string fieldName,
        int maxLength,
        IReadOnlySet<string> allowedValues,
        Dictionary<string, string[]> errors)
    {
        ValidateRequiredLength(value, fieldName, maxLength, errors);
        if (errors.ContainsKey(fieldName))
        {
            return;
        }

        var normalizedValue = NormalizeRequiredCode(value!);
        if (!allowedValues.Contains(normalizedValue))
        {
            errors[fieldName] = [$"{fieldName} is not supported."];
        }
    }

    private static void ValidateRequiredLength(
        string? value,
        string fieldName,
        int maxLength,
        Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[fieldName] = [$"{fieldName} is required."];
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            errors[fieldName] = [$"{fieldName} must be {maxLength} characters or fewer."];
        }
    }

    private static void ValidateOptionalLength(
        string? value,
        string fieldName,
        int maxLength,
        Dictionary<string, string[]> errors)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            errors[fieldName] = [$"{fieldName} must be {maxLength} characters or fewer."];
        }
    }

    private static int ValidateLimit(int? limit, Dictionary<string, string[]> errors)
    {
        if (limit is null)
        {
            return DefaultLimit;
        }

        if (limit <= 0 || limit > MaxLimit)
        {
            errors["limit"] = [$"limit must be between 1 and {MaxLimit}."];
            return DefaultLimit;
        }

        return limit.Value;
    }

    private static string? NormalizeOptionalCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    private static string NormalizeRequiredCode(string value)
    {
        return value.Trim().ToUpperInvariant();
    }

    private static IReadOnlyList<string> ResolveReadableModuleKeys(ClaimsPrincipal principal)
    {
        return ModuleAccess.Values
            .Where(access => HasPermission(principal, access.ReadPermission))
            .Select(access => access.ModuleKey)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool HasReadAccess(ClaimsPrincipal principal, string moduleKey)
    {
        return ModuleAccess.TryGetValue(moduleKey, out var access)
            && HasPermission(principal, access.ReadPermission);
    }

    private static bool HasWriteAccess(ClaimsPrincipal principal, string moduleKey)
    {
        return ModuleAccess.TryGetValue(moduleKey, out var access)
            && HasPermission(principal, access.WritePermission);
    }

    private static bool HasPermission(ClaimsPrincipal principal, string permissionCode)
    {
        return principal.HasClaim(PlatformPermissionCodes.ClaimType, permissionCode);
    }

    private static bool TryResolveUserId(ClaimsPrincipal principal, out Guid userId)
    {
        return Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out userId);
    }

    private static string? ResolvePrincipalDisplayName(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(JwtRegisteredClaimNames.Name)
            ?? principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName);
    }

    private sealed record ContactInterventionModuleAccess(
        string ModuleKey,
        string ReadPermission,
        string WritePermission);
}
