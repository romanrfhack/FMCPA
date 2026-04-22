using FMCPA.Domain.Entities.Markets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Markets;

public sealed class MarketIssueConfiguration : IEntityTypeConfiguration<MarketIssue>
{
    public void Configure(EntityTypeBuilder<MarketIssue> builder)
    {
        builder.ToTable("MarketIssues");

        builder.HasKey(issue => issue.Id);

        builder.Property(issue => issue.IssueType)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(issue => issue.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(issue => issue.AdvanceSummary)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(issue => issue.FollowUpOrResolution)
            .HasMaxLength(1500);

        builder.Property(issue => issue.FinalSatisfaction)
            .HasMaxLength(200);

        builder.Property(issue => issue.CreatedUtc)
            .IsRequired();

        builder.HasOne(issue => issue.StatusCatalogEntry)
            .WithMany()
            .HasForeignKey(issue => issue.StatusCatalogEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(issue => issue.MarketId);
        builder.HasIndex(issue => issue.IssueDate);
        builder.HasIndex(issue => issue.StatusCatalogEntryId);
    }
}
