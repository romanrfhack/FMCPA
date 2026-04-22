using FMCPA.Domain.Entities.Federation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Federation;

public sealed class FederationDonationApplicationEvidenceConfiguration : IEntityTypeConfiguration<FederationDonationApplicationEvidence>
{
    public void Configure(EntityTypeBuilder<FederationDonationApplicationEvidence> builder)
    {
        builder.ToTable("FederationDonationApplicationEvidences");

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

        builder.HasOne(evidence => evidence.FederationDonationApplication)
            .WithMany(application => application.Evidences)
            .HasForeignKey(evidence => evidence.FederationDonationApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(evidence => evidence.EvidenceType)
            .WithMany()
            .HasForeignKey(evidence => evidence.EvidenceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(evidence => evidence.FederationDonationApplicationId);
        builder.HasIndex(evidence => evidence.EvidenceTypeId);
    }
}
