using FMCPA.Domain.Entities.Donations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Donations;

public sealed class DonationApplicationEvidenceConfiguration : IEntityTypeConfiguration<DonationApplicationEvidence>
{
    public void Configure(EntityTypeBuilder<DonationApplicationEvidence> builder)
    {
        builder.ToTable("DonationApplicationEvidences");

        builder.HasKey(evidence => evidence.Id);

        builder.Property(evidence => evidence.Description)
            .HasMaxLength(1000);

        builder.Property(evidence => evidence.OriginalFileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(evidence => evidence.StoredRelativePath)
            .HasMaxLength(400)
            .IsRequired();

        builder.Property(evidence => evidence.ContentType)
            .HasMaxLength(120);

        builder.Property(evidence => evidence.FileSizeBytes)
            .IsRequired();

        builder.Property(evidence => evidence.UploadedUtc)
            .IsRequired();

        builder.HasOne(evidence => evidence.DonationApplication)
            .WithMany(application => application.Evidences)
            .HasForeignKey(evidence => evidence.DonationApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(evidence => evidence.EvidenceType)
            .WithMany()
            .HasForeignKey(evidence => evidence.EvidenceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(evidence => evidence.DonationApplicationId);
        builder.HasIndex(evidence => evidence.EvidenceTypeId);
    }
}
