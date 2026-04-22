namespace FMCPA.Domain.Entities.Shared;

public sealed class ContactParticipation
{
    private ContactParticipation()
    {
    }

    public ContactParticipation(
        Guid contactId,
        string moduleCode,
        string contextType,
        string contextKey,
        string? roleName,
        string? notes)
    {
        if (contactId == Guid.Empty)
        {
            throw new ArgumentException("The contact identifier is required.", nameof(contactId));
        }

        Id = Guid.NewGuid();
        ContactId = contactId;
        ModuleCode = NormalizeCode(moduleCode);
        ContextType = NormalizeRequired(contextType, nameof(contextType));
        ContextKey = NormalizeRequired(contextKey, nameof(contextKey));
        RoleName = NormalizeOptional(roleName);
        Notes = NormalizeOptional(notes);
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid ContactId { get; private set; }

    public string ModuleCode { get; private set; } = string.Empty;

    public string ContextType { get; private set; } = string.Empty;

    public string ContextKey { get; private set; } = string.Empty;

    public string? RoleName { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public Contact? Contact { get; private set; }

    private static string NormalizeCode(string value)
    {
        return NormalizeRequired(value, nameof(value)).ToUpperInvariant();
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required participation value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
