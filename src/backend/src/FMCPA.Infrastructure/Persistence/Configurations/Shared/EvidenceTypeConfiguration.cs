using FMCPA.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Shared;

public sealed class EvidenceTypeConfiguration : IEntityTypeConfiguration<EvidenceType>
{
    public void Configure(EntityTypeBuilder<EvidenceType> builder)
    {
        builder.ToTable("EvidenceTypes");

        builder.HasKey(evidenceType => evidenceType.Id);

        builder.Property(evidenceType => evidenceType.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(evidenceType => evidenceType.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(evidenceType => evidenceType.Description)
            .HasMaxLength(250);

        builder.Property(evidenceType => evidenceType.SortOrder)
            .IsRequired();

        builder.Property(evidenceType => evidenceType.IsActive)
            .IsRequired();

        builder.HasIndex(evidenceType => evidenceType.Code)
            .IsUnique();

        builder.HasData(SharedCatalogSeedData.EvidenceTypes);
    }
}
