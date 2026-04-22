using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Domain.Entities.Financials;

public sealed class FinancialCreditCommission
{
    private FinancialCreditCommission()
    {
    }

    public FinancialCreditCommission(
        Guid financialCreditId,
        int commissionTypeId,
        string recipientCategory,
        Guid? recipientContactId,
        string recipientName,
        decimal baseAmount,
        decimal commissionAmount,
        string? notes)
    {
        if (financialCreditId == Guid.Empty)
        {
            throw new ArgumentException("The financial credit identifier is required.", nameof(financialCreditId));
        }

        if (commissionTypeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(commissionTypeId), "The commission type is required.");
        }

        if (baseAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(baseAmount), "The commission base amount must be greater than zero.");
        }

        if (commissionAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(commissionAmount), "The commission amount must be greater than zero.");
        }

        Id = Guid.NewGuid();
        FinancialCreditId = financialCreditId;
        CommissionTypeId = commissionTypeId;
        RecipientCategory = NormalizeRequired(recipientCategory, nameof(recipientCategory)).ToUpperInvariant();
        RecipientContactId = recipientContactId == Guid.Empty ? null : recipientContactId;
        RecipientName = NormalizeRequired(recipientName, nameof(recipientName));
        BaseAmount = decimal.Round(baseAmount, 2, MidpointRounding.AwayFromZero);
        CommissionAmount = decimal.Round(commissionAmount, 2, MidpointRounding.AwayFromZero);
        Notes = NormalizeOptional(notes);
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid FinancialCreditId { get; private set; }

    public int CommissionTypeId { get; private set; }

    public string RecipientCategory { get; private set; } = string.Empty;

    public Guid? RecipientContactId { get; private set; }

    public string RecipientName { get; private set; } = string.Empty;

    public decimal BaseAmount { get; private set; }

    public decimal CommissionAmount { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public FinancialCredit? FinancialCredit { get; private set; }

    public CommissionType? CommissionType { get; private set; }

    public Contact? RecipientContact { get; private set; }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required financial credit commission value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
