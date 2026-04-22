using FMCPA.Domain.Entities.Shared;

namespace FMCPA.Domain.Entities.Markets;

public sealed class MarketIssue
{
    private MarketIssue()
    {
    }

    public MarketIssue(
        Guid marketId,
        string issueType,
        string description,
        DateOnly issueDate,
        string advanceSummary,
        int statusCatalogEntryId,
        string? followUpOrResolution,
        string? finalSatisfaction)
    {
        if (marketId == Guid.Empty)
        {
            throw new ArgumentException("The market identifier is required.", nameof(marketId));
        }

        if (statusCatalogEntryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCatalogEntryId), "The issue status is required.");
        }

        Id = Guid.NewGuid();
        MarketId = marketId;
        IssueType = NormalizeRequired(issueType, nameof(issueType));
        Description = NormalizeRequired(description, nameof(description));
        IssueDate = issueDate;
        AdvanceSummary = NormalizeRequired(advanceSummary, nameof(advanceSummary));
        StatusCatalogEntryId = statusCatalogEntryId;
        FollowUpOrResolution = NormalizeOptional(followUpOrResolution);
        FinalSatisfaction = NormalizeOptional(finalSatisfaction);
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid MarketId { get; private set; }

    public string IssueType { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public DateOnly IssueDate { get; private set; }

    public string AdvanceSummary { get; private set; } = string.Empty;

    public int StatusCatalogEntryId { get; private set; }

    public string? FollowUpOrResolution { get; private set; }

    public string? FinalSatisfaction { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public Market? Market { get; private set; }

    public ModuleStatusCatalogEntry? StatusCatalogEntry { get; private set; }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A required market issue value is missing.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
