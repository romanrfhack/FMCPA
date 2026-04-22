namespace FMCPA.Domain.Entities;

public sealed class SystemSetting
{
    private SystemSetting()
    {
    }

    public SystemSetting(string key, string value)
    {
        Id = Guid.NewGuid();
        Key = Normalize(key);
        Value = value?.Trim() ?? string.Empty;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Key { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset? UpdatedUtc { get; private set; }

    public void UpdateValue(string value)
    {
        Value = value?.Trim() ?? string.Empty;
        UpdatedUtc = DateTimeOffset.UtcNow;
    }

    private static string Normalize(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("The setting key is required.", nameof(key));
        }

        return key.Trim();
    }
}
