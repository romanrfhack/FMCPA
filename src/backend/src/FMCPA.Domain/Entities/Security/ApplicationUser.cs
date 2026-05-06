namespace FMCPA.Domain.Entities.Security;

public sealed class ApplicationUser
{
    private ApplicationUser()
    {
    }

    public ApplicationUser(string userName, string displayName, string passwordHash, string roleCode)
    {
        Id = Guid.NewGuid();
        UserName = NormalizeRequired(userName, nameof(userName));
        NormalizedUserName = NormalizeUserName(userName);
        DisplayName = NormalizeRequired(displayName, nameof(displayName));
        PasswordHash = NormalizeRequired(passwordHash, nameof(passwordHash));
        RoleCode = ApplicationRoleCodes.Normalize(roleCode);
        SecurityStamp = GenerateSecurityStamp();
        IsActive = true;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string UserName { get; private set; } = string.Empty;

    public string NormalizedUserName { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string RoleCode { get; private set; } = string.Empty;

    public string SecurityStamp { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset? UpdatedUtc { get; private set; }

    public DateTimeOffset? LastLoginUtc { get; private set; }

    public int AccessFailedCount { get; private set; }

    public DateTimeOffset? LockoutEndUtc { get; private set; }

    public void SyncBootstrapProfile(string displayName, string passwordHash, string roleCode)
    {
        var normalizedDisplayName = NormalizeRequired(displayName, nameof(displayName));
        var normalizedPasswordHash = NormalizeRequired(passwordHash, nameof(passwordHash));
        var normalizedRoleCode = ApplicationRoleCodes.Normalize(roleCode);

        var hasChanges = false;
        var requiresSecurityStampRotation = false;

        if (!string.Equals(DisplayName, normalizedDisplayName, StringComparison.Ordinal))
        {
            DisplayName = normalizedDisplayName;
            hasChanges = true;
        }

        if (!IsActive)
        {
            IsActive = true;
            hasChanges = true;
            requiresSecurityStampRotation = true;
        }

        if (!string.Equals(PasswordHash, normalizedPasswordHash, StringComparison.Ordinal))
        {
            PasswordHash = normalizedPasswordHash;
            ResetAccessFailureState();
            hasChanges = true;
            requiresSecurityStampRotation = true;
        }

        if (!string.Equals(RoleCode, normalizedRoleCode, StringComparison.Ordinal))
        {
            RoleCode = normalizedRoleCode;
            hasChanges = true;
            requiresSecurityStampRotation = true;
        }

        if (hasChanges)
        {
            Touch(requiresSecurityStampRotation);
        }
    }

    public void RecordSuccessfulLogin()
    {
        LastLoginUtc = DateTimeOffset.UtcNow;
        ResetAccessFailureState();
        Touch(rotateSecurityStamp: false);
    }

    public bool RecordFailedLogin(DateTimeOffset utcNow, int maxFailedAccessAttempts, TimeSpan cooldown)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFailedAccessAttempts);

        if (cooldown <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(cooldown), "Lockout cooldown must be greater than zero.");
        }

        AccessFailedCount++;

        if (AccessFailedCount >= maxFailedAccessAttempts)
        {
            LockoutEndUtc = utcNow.Add(cooldown);
            Touch(rotateSecurityStamp: false);
            return true;
        }

        Touch(rotateSecurityStamp: false);
        return false;
    }

    public bool IsLockedOut(DateTimeOffset utcNow)
    {
        return LockoutEndUtc is not null && LockoutEndUtc > utcNow;
    }

    public bool ReleaseExpiredLockout(DateTimeOffset utcNow)
    {
        if (LockoutEndUtc is null || LockoutEndUtc > utcNow)
        {
            return false;
        }

        ResetAccessFailureState();
        Touch(rotateSecurityStamp: false);
        return true;
    }

    public bool ClearAccessLockout()
    {
        if (AccessFailedCount == 0 && LockoutEndUtc is null)
        {
            return false;
        }

        ResetAccessFailureState();
        Touch(rotateSecurityStamp: false);
        return true;
    }

    public void ChangeRole(string roleCode)
    {
        var normalizedRoleCode = ApplicationRoleCodes.Normalize(roleCode);
        if (string.Equals(RoleCode, normalizedRoleCode, StringComparison.Ordinal))
        {
            return;
        }

        RoleCode = normalizedRoleCode;
        Touch(rotateSecurityStamp: true);
    }

    public void SetIsActive(bool isActive)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        Touch(rotateSecurityStamp: true);
    }

    public void ResetPassword(string passwordHash)
    {
        var normalizedPasswordHash = NormalizeRequired(passwordHash, nameof(passwordHash));
        PasswordHash = normalizedPasswordHash;
        ResetAccessFailureState();
        Touch(rotateSecurityStamp: true);
    }

    public static string NormalizeUserName(string value)
    {
        return NormalizeRequired(value, nameof(value)).ToUpperInvariant();
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("The application user value is required.", paramName);
        }

        return value.Trim();
    }

    private void Touch(bool rotateSecurityStamp)
    {
        UpdatedUtc = DateTimeOffset.UtcNow;

        if (rotateSecurityStamp)
        {
            SecurityStamp = GenerateSecurityStamp();
        }
    }

    private static string GenerateSecurityStamp()
    {
        return Guid.NewGuid().ToString("N");
    }

    private void ResetAccessFailureState()
    {
        AccessFailedCount = 0;
        LockoutEndUtc = null;
    }
}
