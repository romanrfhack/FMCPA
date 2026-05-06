namespace FMCPA.Api.Auth;

public sealed class CredentialSecuritySettings
{
    private CredentialSecuritySettings(PasswordPolicyRule passwordPolicy, LockoutRule lockout)
    {
        PasswordPolicy = passwordPolicy;
        Lockout = lockout;
    }

    public PasswordPolicyRule PasswordPolicy { get; }

    public LockoutRule Lockout { get; }

    public static CredentialSecuritySettings Resolve(IConfiguration configuration)
    {
        return new CredentialSecuritySettings(
            PasswordPolicyRule.Resolve(configuration),
            LockoutRule.Resolve(configuration));
    }
}

public sealed class PasswordPolicyRule
{
    private PasswordPolicyRule(
        int minimumLength,
        bool requireUppercase,
        bool requireLowercase,
        bool requireDigit,
        bool requireNonAlphanumeric)
    {
        MinimumLength = minimumLength;
        RequireUppercase = requireUppercase;
        RequireLowercase = requireLowercase;
        RequireDigit = requireDigit;
        RequireNonAlphanumeric = requireNonAlphanumeric;
    }

    public int MinimumLength { get; }

    public bool RequireUppercase { get; }

    public bool RequireLowercase { get; }

    public bool RequireDigit { get; }

    public bool RequireNonAlphanumeric { get; }

    public static PasswordPolicyRule Resolve(IConfiguration configuration)
    {
        const string sectionPath = "Security:Credentials:PasswordPolicy";
        var minimumLength = configuration.GetValue<int?>($"{sectionPath}:MinimumLength") ?? 12;
        if (minimumLength < 12)
        {
            throw new InvalidOperationException("Security:Credentials:PasswordPolicy:MinimumLength must be at least 12.");
        }

        return new PasswordPolicyRule(
            minimumLength,
            configuration.GetValue<bool?>($"{sectionPath}:RequireUppercase") ?? true,
            configuration.GetValue<bool?>($"{sectionPath}:RequireLowercase") ?? true,
            configuration.GetValue<bool?>($"{sectionPath}:RequireDigit") ?? true,
            configuration.GetValue<bool?>($"{sectionPath}:RequireNonAlphanumeric") ?? false);
    }
}

public sealed class LockoutRule
{
    private LockoutRule(int maxFailedAccessAttempts, int cooldownSeconds)
    {
        MaxFailedAccessAttempts = maxFailedAccessAttempts;
        CooldownSeconds = cooldownSeconds;
    }

    public int MaxFailedAccessAttempts { get; }

    public int CooldownSeconds { get; }

    public TimeSpan Cooldown => TimeSpan.FromSeconds(CooldownSeconds);

    public static LockoutRule Resolve(IConfiguration configuration)
    {
        const string sectionPath = "Security:Credentials:Lockout";
        var maxFailedAccessAttempts = configuration.GetValue<int?>($"{sectionPath}:MaxFailedAccessAttempts") ?? 5;
        var cooldownSeconds = configuration.GetValue<int?>($"{sectionPath}:CooldownSeconds") ?? 900;

        if (maxFailedAccessAttempts <= 0)
        {
            throw new InvalidOperationException("Security:Credentials:Lockout:MaxFailedAccessAttempts must be greater than zero.");
        }

        if (cooldownSeconds <= 0)
        {
            throw new InvalidOperationException("Security:Credentials:Lockout:CooldownSeconds must be greater than zero.");
        }

        return new LockoutRule(maxFailedAccessAttempts, cooldownSeconds);
    }
}
