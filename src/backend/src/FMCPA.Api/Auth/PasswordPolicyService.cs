namespace FMCPA.Api.Auth;

public sealed class PasswordPolicyService
{
    private static readonly HashSet<string> BlockedPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "admin123",
        "adminadmin",
        "changeme",
        "localadmin123",
        "password",
        "password1",
        "password123",
        "qwerty123",
        "temporal",
        "123456789012"
    };

    private readonly PasswordPolicyRule _rule;

    public PasswordPolicyService(CredentialSecuritySettings settings)
    {
        _rule = settings.PasswordPolicy;
    }

    public IReadOnlyList<string> Validate(string? password, string fieldName)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add($"{fieldName} is required.");
            return errors;
        }

        var normalizedPassword = password.Trim();
        if (!string.Equals(password, normalizedPassword, StringComparison.Ordinal))
        {
            errors.Add($"{fieldName} must not start or end with whitespace.");
        }

        if (normalizedPassword.Length < _rule.MinimumLength)
        {
            errors.Add($"{fieldName} must contain at least {_rule.MinimumLength} characters.");
        }

        if (_rule.RequireUppercase && !normalizedPassword.Any(char.IsUpper))
        {
            errors.Add($"{fieldName} must include at least one uppercase letter.");
        }

        if (_rule.RequireLowercase && !normalizedPassword.Any(char.IsLower))
        {
            errors.Add($"{fieldName} must include at least one lowercase letter.");
        }

        if (_rule.RequireDigit && !normalizedPassword.Any(char.IsDigit))
        {
            errors.Add($"{fieldName} must include at least one number.");
        }

        if (_rule.RequireNonAlphanumeric && !normalizedPassword.Any(character => !char.IsLetterOrDigit(character)))
        {
            errors.Add($"{fieldName} must include at least one symbol.");
        }

        if (BlockedPasswords.Contains(normalizedPassword) || HasSingleRepeatedCharacter(normalizedPassword))
        {
            errors.Add($"{fieldName} is too weak; use a non-obvious temporary password.");
        }

        return errors;
    }

    private static bool HasSingleRepeatedCharacter(string value)
    {
        return value.Length > 0 && value.All(character => character == value[0]);
    }
}
