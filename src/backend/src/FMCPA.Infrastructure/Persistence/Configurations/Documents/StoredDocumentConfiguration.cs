using FMCPA.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Documents;

public sealed class StoredDocumentConfiguration : IEntityTypeConfiguration<StoredDocument>
{
    public void Configure(EntityTypeBuilder<StoredDocument> builder)
    {
        builder.ToTable("StoredDocuments");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.ModuleCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.DocumentAreaCode)
            .HasMaxLength(96)
            .IsRequired();

        builder.Property(item => item.EntityType)
            .HasMaxLength(96)
            .IsRequired();

        builder.Property(item => item.OriginalFileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(item => item.StoredRelativePath)
            .HasMaxLength(520)
            .IsRequired();

        builder.Property(item => item.ContentType)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(item => item.SizeBytes)
            .IsRequired();

        builder.Property(item => item.CreatedUtc)
            .IsRequired();

        builder.Property(item => item.Sha256Hex)
            .HasMaxLength(128);

        builder.Property(item => item.IsLegacyBackfill)
            .IsRequired();

        builder.Property(item => item.DocumentClassCode)
            .HasMaxLength(64)
            .HasDefaultValue(DocumentClassCodes.Other)
            .IsRequired();

        builder.Property(item => item.BusinessPurpose)
            .HasMaxLength(500);

        builder.Property(item => item.IsPrimaryDocument)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(item => item.ClassificationNotes)
            .HasMaxLength(500);

        builder.Property(item => item.RetentionPolicyCode)
            .HasMaxLength(64)
            .HasDefaultValue(DocumentRetentionPolicyCodes.GenericReview)
            .IsRequired();

        builder.Property(item => item.RetentionUntilUtc)
            .IsRequired();

        builder.Property(item => item.RetentionOverridePolicyCode)
            .HasMaxLength(64);

        builder.Property(item => item.RetentionOverrideUntilUtc);

        builder.Property(item => item.RetentionOverrideReason)
            .HasMaxLength(500);

        builder.Property(item => item.RetentionReviewStatusCode)
            .HasMaxLength(32)
            .HasDefaultValue(DocumentRetentionReviewStatusCodes.Pending)
            .IsRequired();

        builder.Property(item => item.LastRetentionReviewUtc);

        builder.Property(item => item.NextRetentionReviewUtc);

        builder.Property(item => item.RetentionReviewNotes)
            .HasMaxLength(500);

        builder.Property(item => item.IsAdministrativeHold)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(item => item.HoldReason)
            .HasMaxLength(500);

        builder.Property(item => item.HoldPlacedUtc);

        builder.Property(item => item.HoldReleasedUtc);

        builder.Property(item => item.HoldPlacedBy)
            .HasMaxLength(256);

        builder.Property(item => item.StatusCode)
            .HasMaxLength(32)
            .HasDefaultValue(StoredDocument.ActiveStatusCode)
            .IsRequired();

        builder.Property(item => item.ArchivedUtc);

        builder.Property(item => item.ArchiveReason)
            .HasMaxLength(500);

        builder.Property(item => item.ReplacementGroupKey)
            .HasMaxLength(220)
            .IsRequired();

        builder.Property(item => item.ReplacedDocumentId);

        builder.Property(item => item.SupersededByDocumentId);

        builder.HasIndex(item => new { item.DocumentAreaCode, item.EntityType, item.EntityId, item.StatusCode });
        builder.HasIndex(item => item.ReplacementGroupKey);
        builder.HasIndex(item => item.ReplacedDocumentId);
        builder.HasIndex(item => item.SupersededByDocumentId);
        builder.HasIndex(item => new { item.ModuleCode, item.EntityType, item.EntityId });
        builder.HasIndex(item => new { item.DocumentClassCode, item.CreatedUtc });
        builder.HasIndex(item => new { item.RetentionPolicyCode, item.RetentionUntilUtc });
        builder.HasIndex(item => new { item.RetentionOverridePolicyCode, item.RetentionOverrideUntilUtc });
        builder.HasIndex(item => new { item.RetentionReviewStatusCode, item.NextRetentionReviewUtc });
        builder.HasIndex(item => new { item.IsAdministrativeHold, item.RetentionReviewStatusCode, item.NextRetentionReviewUtc });
        builder.HasIndex(item => new { item.StatusCode, item.CreatedUtc });
        builder.HasIndex(item => item.CreatedUtc);
    }
}
