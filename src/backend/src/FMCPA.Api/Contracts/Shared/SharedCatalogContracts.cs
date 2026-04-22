namespace FMCPA.Api.Contracts.Shared;

public sealed record ContactTypeResponse(
    int Id,
    string Code,
    string Name,
    string? Description,
    int SortOrder);

public sealed record ContactResponse(
    Guid Id,
    string Name,
    int ContactTypeId,
    string ContactTypeCode,
    string ContactTypeName,
    string? OrganizationOrDependency,
    string? RoleTitle,
    string? MobilePhone,
    string? WhatsAppPhone,
    string? Email,
    string? Notes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? UpdatedUtc);

public sealed record CreateContactRequest(
    string Name,
    int ContactTypeId,
    string? OrganizationOrDependency,
    string? RoleTitle,
    string? MobilePhone,
    string? WhatsAppPhone,
    string? Email,
    string? Notes);

public sealed record CatalogItemResponse(
    int Id,
    string Code,
    string Name,
    string? Description,
    int SortOrder,
    bool IsActive);

public sealed record CreateCatalogItemRequest(
    string Code,
    string Name,
    string? Description,
    int SortOrder);

public sealed record ModuleStatusCatalogEntryResponse(
    int Id,
    string ModuleCode,
    string ModuleName,
    string ContextCode,
    string ContextName,
    string StatusCode,
    string StatusName,
    string? Description,
    int SortOrder,
    bool IsClosed,
    bool AlertsEnabledByDefault,
    bool IsActive);

public sealed record CreateModuleStatusCatalogEntryRequest(
    string ModuleCode,
    string ModuleName,
    string? ContextCode,
    string? ContextName,
    string StatusCode,
    string StatusName,
    string? Description,
    int SortOrder,
    bool IsClosed,
    bool AlertsEnabledByDefault);
