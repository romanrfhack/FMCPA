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

        builder.HasIndex(item => new { item.DocumentAreaCode, item.EntityType, item.EntityId })
            .IsUnique();

        builder.HasIndex(item => new { item.ModuleCode, item.EntityType, item.EntityId });
        builder.HasIndex(item => item.CreatedUtc);
    }
}
