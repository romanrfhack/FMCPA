using FMCPA.Domain.Entities.Donations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Donations;

public sealed class DonationConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.ToTable("Donations");

        builder.HasKey(donation => donation.Id);

        builder.Property(donation => donation.DonorEntityName)
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
            .HasMaxLength(1500);

        builder.Property(donation => donation.CreatedUtc)
            .IsRequired();

        builder.HasOne(donation => donation.StatusCatalogEntry)
            .WithMany()
            .HasForeignKey(donation => donation.StatusCatalogEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(donation => donation.DonationDate);
        builder.HasIndex(donation => donation.StatusCatalogEntryId);
        builder.HasIndex(donation => new { donation.DonationDate, donation.StatusCatalogEntryId });
    }
}
