namespace FMCPA.Domain.Entities.Shared;

public sealed class Contact
{
    private Contact()
    {
    }

    public Contact(
        string name,
        int contactTypeId,
        string? organizationOrDependency,
        string? roleTitle,
        string? mobilePhone,
        string? whatsAppPhone,
        string? email,
        string? notes)
    {
        if (contactTypeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contactTypeId), "The contact type is required.");
        }

        Id = Guid.NewGuid();
        Name = NormalizeRequired(name, nameof(name));
        ContactTypeId = contactTypeId;
        OrganizationOrDependency = NormalizeOptional(organizationOrDependency);
        RoleTitle = NormalizeOptional(roleTitle);
        MobilePhone = NormalizeOptional(mobilePhone);
        WhatsAppPhone = NormalizeOptional(whatsAppPhone);
        Email = NormalizeEmail(email);
        Notes = NormalizeOptional(notes);
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public int ContactTypeId { get; private set; }

    public string? OrganizationOrDependency { get; private set; }

    public string? RoleTitle { get; private set; }

    public string? MobilePhone { get; private set; }

    public string? WhatsAppPhone { get; private set; }

    public string? Email { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset? UpdatedUtc { get; private set; }

    public ContactType? ContactType { get; private set; }

    public ICollection<ContactParticipation> Participations { get; } = new List<ContactParticipation>();

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required contact value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant();
    }
}
