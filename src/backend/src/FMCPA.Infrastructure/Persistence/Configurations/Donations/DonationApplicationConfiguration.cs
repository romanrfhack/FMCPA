using FMCPA.Domain.Entities.Donations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Donations;

public sealed class DonationApplicationConfiguration : IEntityTypeConfiguration<DonationApplication>
{
    public void Configure(EntityTypeBuilder<DonationApplication> builder)
    {
        builder.ToTable("DonationApplications");

        builder.HasKey(application => application.Id);

        builder.Property(application => application.BeneficiaryName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(application => application.ResponsibleName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(application => application.AppliedAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(application => application.VerificationDetails)
            .HasMaxLength(2000);

        builder.Property(application => application.ClosingDetails)
            .HasMaxLength(2000);

        builder.Property(application => application.CreatedUtc)
            .IsRequired();

        builder.HasOne(application => application.Donation)
            .WithMany(donation => donation.Applications)
            .HasForeignKey(application => application.DonationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(application => application.ResponsibleContact)
            .WithMany()
            .HasForeignKey(application => application.ResponsibleContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(application => application.StatusCatalogEntry)
            .WithMany()
            .HasForeignKey(application => application.StatusCatalogEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(application => application.DonationId);
        builder.HasIndex(application => application.ApplicationDate);
        builder.HasIndex(application => new { application.DonationId, application.ApplicationDate });
    }
}
