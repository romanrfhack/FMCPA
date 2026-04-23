using FMCPA.Domain.Entities.Security;

namespace FMCPA.Api.Auth;

public sealed class LocalBootstrapUserOptions
{
    private LocalBootstrapUserOptions(
        bool enabled,
        LocalBootstrapUserDefinition adminUser,
        LocalBootstrapUserDefinition operatorUser,
        LocalBootstrapUserDefinition readOnlyUser)
    {
        Enabled = enabled;
        AdminUser = adminUser;
        OperatorUser = operatorUser;
        ReadOnlyUser = readOnlyUser;
    }

    public bool Enabled { get; }

    public LocalBootstrapUserDefinition AdminUser { get; }

    public LocalBootstrapUserDefinition OperatorUser { get; }

    public LocalBootstrapUserDefinition ReadOnlyUser { get; }

    public IReadOnlyList<LocalBootstrapUserDefinition> Users => [AdminUser, OperatorUser, ReadOnlyUser];

    public static LocalBootstrapUserOptions Resolve(IConfiguration configuration)
    {
        return new LocalBootstrapUserOptions(
            configuration.GetValue("Auth:Bootstrap:Enabled", true),
            ResolveAdminUser(configuration),
            ResolveScopedUser(configuration, "Operator", "operator", "Operador local", ApplicationRoleCodes.Operator),
            ResolveScopedUser(configuration, "ReadOnly", "readonly", "Consulta local", ApplicationRoleCodes.ReadOnly));
    }

    private static LocalBootstrapUserDefinition ResolveAdminUser(IConfiguration configuration)
    {
        var userName = configuration["Auth:Bootstrap:UserName"]?.Trim();
        var displayName = configuration["Auth:Bootstrap:DisplayName"]?.Trim();
        var password = configuration["Auth:Bootstrap:Password"];

        return new LocalBootstrapUserDefinition(
            ApplicationRoleCodes.Admin,
            string.IsNullOrWhiteSpace(userName) ? "admin" : userName,
            string.IsNullOrWhiteSpace(displayName) ? "Administrador local" : displayName,
            string.IsNullOrWhiteSpace(password) ? null : password,
            IsPrimary: true);
    }

    private static LocalBootstrapUserDefinition ResolveScopedUser(
        IConfiguration configuration,
        string scopeName,
        string defaultUserName,
        string defaultDisplayName,
        string roleCode)
    {
        var userName = configuration[$"Auth:Bootstrap:{scopeName}:UserName"]?.Trim();
        var displayName = configuration[$"Auth:Bootstrap:{scopeName}:DisplayName"]?.Trim();
        var password = configuration[$"Auth:Bootstrap:{scopeName}:Password"];

        return new LocalBootstrapUserDefinition(
            roleCode,
            string.IsNullOrWhiteSpace(userName) ? defaultUserName : userName,
            string.IsNullOrWhiteSpace(displayName) ? defaultDisplayName : displayName,
            string.IsNullOrWhiteSpace(password) ? null : password,
            IsPrimary: false);
    }
}

public sealed record LocalBootstrapUserDefinition(
    string RoleCode,
    string UserName,
    string DisplayName,
    string? Password,
    bool IsPrimary)
{
    public bool HasPassword => !string.IsNullOrWhiteSpace(Password);
}
