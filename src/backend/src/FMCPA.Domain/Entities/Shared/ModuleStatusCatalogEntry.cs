namespace FMCPA.Domain.Entities.Shared;

public sealed class ModuleStatusCatalogEntry
{
    private ModuleStatusCatalogEntry()
    {
    }

    public ModuleStatusCatalogEntry(
        string moduleCode,
        string moduleName,
        string? contextCode,
        string? contextName,
        string statusCode,
        string statusName,
        string? description,
        int sortOrder,
        bool isClosed,
        bool alertsEnabledByDefault)
    {
        ModuleCode = NormalizeCode(moduleCode);
        ModuleName = NormalizeRequired(moduleName, nameof(moduleName));
        ContextCode = NormalizeOptionalCode(contextCode) ?? "GENERAL";
        ContextName = NormalizeOptional(contextName) ?? "General";
        StatusCode = NormalizeCode(statusCode);
        StatusName = NormalizeRequired(statusName, nameof(statusName));
        Description = NormalizeOptional(description);
        SortOrder = sortOrder;
        IsClosed = isClosed;
        AlertsEnabledByDefault = alertsEnabledByDefault;
        IsActive = true;
    }

    public int Id { get; private set; }

    public string ModuleCode { get; private set; } = string.Empty;

    public string ModuleName { get; private set; } = string.Empty;

    public string ContextCode { get; private set; } = string.Empty;

    public string ContextName { get; private set; } = string.Empty;

    public string StatusCode { get; private set; } = string.Empty;

    public string StatusName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsClosed { get; private set; }

    public bool AlertsEnabledByDefault { get; private set; }

    public bool IsActive { get; private set; }

    private static string NormalizeCode(string value)
    {
        return NormalizeRequired(value, nameof(value)).ToUpperInvariant();
    }

    private static string? NormalizeOptionalCode(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required status catalog value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
