using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Domain.Entities.Financials;

public sealed class FinancialCredit
{
    private FinancialCredit()
    {
    }

    public FinancialCredit(
        Guid financialPermitId,
        Guid? promoterContactId,
        string promoterName,
        Guid? beneficiaryContactId,
        string beneficiaryName,
        string? phoneNumber,
        string? whatsAppPhone,
        DateOnly authorizationDate,
        decimal amount,
        string? notes)
    {
        if (financialPermitId == Guid.Empty)
        {
            throw new ArgumentException("The financial permit identifier is required.", nameof(financialPermitId));
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "The financial credit amount must be greater than zero.");
        }

        Id = Guid.NewGuid();
        FinancialPermitId = financialPermitId;
        PromoterContactId = promoterContactId == Guid.Empty ? null : promoterContactId;
        PromoterName = NormalizeRequired(promoterName, nameof(promoterName));
        BeneficiaryContactId = beneficiaryContactId == Guid.Empty ? null : beneficiaryContactId;
        BeneficiaryName = NormalizeRequired(beneficiaryName, nameof(beneficiaryName));
        PhoneNumber = NormalizeOptional(phoneNumber);
        WhatsAppPhone = NormalizeOptional(whatsAppPhone);
        AuthorizationDate = authorizationDate;
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Notes = NormalizeOptional(notes);
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid FinancialPermitId { get; private set; }

    public Guid? PromoterContactId { get; private set; }

    public string PromoterName { get; private set; } = string.Empty;

    public Guid? BeneficiaryContactId { get; private set; }

    public string BeneficiaryName { get; private set; } = string.Empty;

    public string? PhoneNumber { get; private set; }

    public string? WhatsAppPhone { get; private set; }

    public DateOnly AuthorizationDate { get; private set; }

    public decimal Amount { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public FinancialPermit? FinancialPermit { get; private set; }

    public Contact? PromoterContact { get; private set; }

    public Contact? BeneficiaryContact { get; private set; }

    public ICollection<FinancialCreditCommission> Commissions { get; } = new List<FinancialCreditCommission>();

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required financial credit value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
