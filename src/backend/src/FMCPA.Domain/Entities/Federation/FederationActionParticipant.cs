using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Domain.Entities.Federation;

public sealed class FederationActionParticipant
{
    private FederationActionParticipant()
    {
    }

    public FederationActionParticipant(
        Guid federationActionId,
        Guid contactId,
        string participantSide,
        string participantName,
        string? organizationOrDependency,
        string? roleTitle,
        string? notes)
    {
        if (federationActionId == Guid.Empty)
        {
            throw new ArgumentException("The federation action identifier is required.", nameof(federationActionId));
        }

        if (contactId == Guid.Empty)
        {
            throw new ArgumentException("The contact identifier is required.", nameof(contactId));
        }

        Id = Guid.NewGuid();
        FederationActionId = federationActionId;
        ContactId = contactId;
        ParticipantSide = NormalizeCode(participantSide);
        ParticipantName = NormalizeRequired(participantName, nameof(participantName));
        OrganizationOrDependency = NormalizeOptional(organizationOrDependency);
        RoleTitle = NormalizeOptional(roleTitle);
        Notes = NormalizeOptional(notes);
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid FederationActionId { get; private set; }

    public Guid ContactId { get; private set; }

    public string ParticipantSide { get; private set; } = string.Empty;

    public string ParticipantName { get; private set; } = string.Empty;

    public string? OrganizationOrDependency { get; private set; }

    public string? RoleTitle { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public FederationAction? FederationAction { get; private set; }

    public Contact? Contact { get; private set; }

    private static string NormalizeCode(string value)
    {
        return NormalizeRequired(value, nameof(value)).ToUpperInvariant();
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required federation participant value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
