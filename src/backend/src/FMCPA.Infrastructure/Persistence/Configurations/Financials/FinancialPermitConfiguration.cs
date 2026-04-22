using FMCPA.Domain.Entities.Financials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Financials;

public sealed class FinancialPermitConfiguration : IEntityTypeConfiguration<FinancialPermit>
{
    public void Configure(EntityTypeBuilder<FinancialPermit> builder)
    {
        builder.ToTable("FinancialPermits");

        builder.HasKey(permit => permit.Id);

        builder.Property(permit => permit.FinancialName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(permit => permit.InstitutionOrDependency)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(permit => permit.PlaceOrStand)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(permit => permit.Schedule)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(permit => permit.NegotiatedTerms)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(permit => permit.Notes)
            .HasMaxLength(1500);

        builder.Property(permit => permit.CreatedUtc)
            .IsRequired();

        builder.HasOne(permit => permit.StatusCatalogEntry)
            .WithMany()
            .HasForeignKey(permit => permit.StatusCatalogEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(permit => permit.ValidTo);
        builder.HasIndex(permit => permit.StatusCatalogEntryId);
        builder.HasIndex(permit => new { permit.ValidTo, permit.StatusCatalogEntryId });
    }
}
