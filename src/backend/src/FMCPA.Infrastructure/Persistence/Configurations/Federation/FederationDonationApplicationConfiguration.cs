using FMCPA.Domain.Entities.Federation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Federation;

public sealed class FederationDonationApplicationConfiguration : IEntityTypeConfiguration<FederationDonationApplication>
{
    public void Configure(EntityTypeBuilder<FederationDonationApplication> builder)
    {
        builder.ToTable("FederationDonationApplications");

        builder.HasKey(application => application.Id);

        builder.Property(application => application.BeneficiaryOrDestinationName)
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

        builder.HasOne(application => application.FederationDonation)
            .WithMany(donation => donation.Applications)
            .HasForeignKey(application => application.FederationDonationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(application => application.StatusCatalogEntry)
            .WithMany()
            .HasForeignKey(application => application.StatusCatalogEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(application => application.FederationDonationId);
        builder.HasIndex(application => application.ApplicationDate);
        builder.HasIndex(application => application.StatusCatalogEntryId);
    }
}
