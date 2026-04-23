namespace FMCPA.Domain.Entities.Security;

public static class ApplicationRoleCodes
{
    public const string Admin = "ADMIN";
    public const string Operator = "OPERATOR";
    public const string ReadOnly = "READONLY";

    public static readonly string[] All = [Admin, Operator, ReadOnly];
    public static readonly string[] ReadWrite = [Admin, Operator];

    public static string Normalize(string value)
    {
        var normalizedValue = value?.Trim().ToUpperInvariant();

        return normalizedValue switch
        {
            Admin => Admin,
            Operator => Operator,
            ReadOnly => ReadOnly,
            _ => throw new ArgumentOutOfRangeException(nameof(value), "The application role code is not supported.")
        };
    }
}
