namespace FMCPA.Domain.Entities.Shared;

public sealed class ContactType
{
    private ContactType()
    {
    }

    public ContactType(string code, string name, string? description = null, int sortOrder = 0)
    {
        Code = NormalizeCode(code);
        Name = NormalizeRequired(name, nameof(name));
        Description = NormalizeOptional(description);
        SortOrder = sortOrder;
        IsActive = true;
    }

    public int Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public ICollection<Contact> Contacts { get; } = new List<Contact>();

    private static string NormalizeCode(string value)
    {
        return NormalizeRequired(value, nameof(value)).ToUpperInvariant();
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required catalog value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
