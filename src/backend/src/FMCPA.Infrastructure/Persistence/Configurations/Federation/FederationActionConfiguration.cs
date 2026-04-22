using FMCPA.Domain.Entities.Federation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Federation;

public sealed class FederationActionConfiguration : IEntityTypeConfiguration<FederationAction>
{
    public void Configure(EntityTypeBuilder<FederationAction> builder)
    {
        builder.ToTable("FederationActions");

        builder.HasKey(action => action.Id);

        builder.Property(action => action.ActionTypeCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(action => action.CounterpartyOrInstitution)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(action => action.Objective)
            .HasMaxLength(1500)
            .IsRequired();

        builder.Property(action => action.Notes)
            .HasMaxLength(2000);

        builder.Property(action => action.CreatedUtc)
            .IsRequired();

        builder.HasOne(action => action.StatusCatalogEntry)
            .WithMany()
            .HasForeignKey(action => action.StatusCatalogEntryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(action => action.ActionDate);
        builder.HasIndex(action => action.ActionTypeCode);
        builder.HasIndex(action => action.StatusCatalogEntryId);
    }
}
