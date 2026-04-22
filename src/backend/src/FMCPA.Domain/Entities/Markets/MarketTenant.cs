using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Domain.Entities.Markets;

public sealed class MarketTenant
{
    private MarketTenant()
    {
    }

    public MarketTenant(
        Guid marketId,
        Guid? contactId,
        string tenantName,
        string certificateNumber,
        DateOnly certificateValidityTo,
        string businessLine,
        string? mobilePhone,
        string? whatsAppPhone,
        string? email,
        string? notes,
        string certificateOriginalFileName,
        string certificateStoredRelativePath,
        string? certificateContentType,
        long certificateFileSizeBytes,
        DateTimeOffset certificateUploadedUtc,
        Guid? id = null)
    {
        if (marketId == Guid.Empty)
        {
            throw new ArgumentException("The market identifier is required.", nameof(marketId));
        }

        if (certificateFileSizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(certificateFileSizeBytes), "The certificate file size is required.");
        }

        Id = id is Guid explicitId && explicitId != Guid.Empty ? explicitId : Guid.NewGuid();
        MarketId = marketId;
        ContactId = contactId == Guid.Empty ? null : contactId;
        TenantName = NormalizeRequired(tenantName, nameof(tenantName));
        CertificateNumber = NormalizeRequired(certificateNumber, nameof(certificateNumber));
        CertificateValidityTo = certificateValidityTo;
        BusinessLine = NormalizeRequired(businessLine, nameof(businessLine));
        MobilePhone = NormalizeOptional(mobilePhone);
        WhatsAppPhone = NormalizeOptional(whatsAppPhone);
        Email = NormalizeEmail(email);
        Notes = NormalizeOptional(notes);
        CertificateOriginalFileName = NormalizeRequired(certificateOriginalFileName, nameof(certificateOriginalFileName));
        CertificateStoredRelativePath = NormalizeRequired(certificateStoredRelativePath, nameof(certificateStoredRelativePath));
        CertificateContentType = NormalizeOptional(certificateContentType);
        CertificateFileSizeBytes = certificateFileSizeBytes;
        CertificateUploadedUtc = certificateUploadedUtc;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid MarketId { get; private set; }

    public Guid? ContactId { get; private set; }

    public string TenantName { get; private set; } = string.Empty;

    public string CertificateNumber { get; private set; } = string.Empty;

    public DateOnly CertificateValidityTo { get; private set; }

    public string BusinessLine { get; private set; } = string.Empty;

    public string? MobilePhone { get; private set; }

    public string? WhatsAppPhone { get; private set; }

    public string? Email { get; private set; }

    public string? Notes { get; private set; }

    public string CertificateOriginalFileName { get; private set; } = string.Empty;

    public string CertificateStoredRelativePath { get; private set; } = string.Empty;

    public string? CertificateContentType { get; private set; }

    public long CertificateFileSizeBytes { get; private set; }

    public DateTimeOffset CertificateUploadedUtc { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public Market? Market { get; private set; }

    public Contact? Contact { get; private set; }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required market tenant value is missing.", paramName);
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
