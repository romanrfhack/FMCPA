using FMCPA.Domain.Entities.Federation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Federation;

public sealed class FederationDonationConfiguration : IEntityTypeConfiguration<FederationDonation>
{
    public void Configure(EntityTypeBuilder<FederationDonation> builder)
    {
        builder.ToTable("FederationDonations");

        builder.HasKey(donation => donation.Id);

        builder.Property(donation => donation.DonorName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(donation => donation.DonationType)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(donation => donation.BaseAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(donation => donation.Reference)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(donation => donation.Notes)
            .HasMaxLength(2000);

        builder.Property(donation => donation.CreatedUtc)
            .IsRequired();

        builder.HasOne(donation => donation.StatusCatalogEntry)
            .WithMany()
            .HasForeignKey(donation => donation.StatusCatalogEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(donation => donation.DonationDate);
        builder.HasIndex(donation => donation.Reference)
            .IsUnique();
        builder.HasIndex(donation => donation.StatusCatalogEntryId);
    }
}
