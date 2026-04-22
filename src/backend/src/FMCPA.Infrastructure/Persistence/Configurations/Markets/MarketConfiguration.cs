using FMCPA.Domain.Entities.Markets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Markets;

public sealed class MarketConfiguration : IEntityTypeConfiguration<Market>
{
    public void Configure(EntityTypeBuilder<Market> builder)
    {
        builder.ToTable("Markets");

        builder.HasKey(market => market.Id);

        builder.Property(market => market.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(market => market.Borough)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(market => market.SecretaryGeneralName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(market => market.Notes)
            .HasMaxLength(1500);

        builder.Property(market => market.CreatedUtc)
            .IsRequired();

        builder.HasOne(market => market.StatusCatalogEntry)
            .WithMany()
            .HasForeignKey(market => market.StatusCatalogEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(market => market.SecretaryGeneralContact)
            .WithMany()
            .HasForeignKey(market => market.SecretaryGeneralContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(market => market.Tenants)
            .WithOne(tenant => tenant.Market)
            .HasForeignKey(tenant => tenant.MarketId);

        builder.HasMany(market => market.Issues)
            .WithOne(issue => issue.Market)
            .HasForeignKey(issue => issue.MarketId);

        builder.HasIndex(market => market.Name);
        builder.HasIndex(market => market.StatusCatalogEntryId);
    }
}
