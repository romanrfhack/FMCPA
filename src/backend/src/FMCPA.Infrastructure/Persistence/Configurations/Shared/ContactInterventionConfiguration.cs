using FMCPA.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Shared;

public sealed class ContactInterventionConfiguration : IEntityTypeConfiguration<ContactIntervention>
{
    public void Configure(EntityTypeBuilder<ContactIntervention> builder)
    {
        builder.ToTable("ContactInterventions");

        builder.HasKey(intervention => intervention.Id);

        builder.Property(intervention => intervention.ModuleKey)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(intervention => intervention.OriginType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(intervention => intervention.OriginId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(intervention => intervention.OriginDisplayName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(intervention => intervention.Subject)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(intervention => intervention.HelpType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(intervention => intervention.Outcome)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(intervention => intervention.Notes)
            .HasMaxLength(2000);

        builder.Property(intervention => intervention.OccurredUtc)
            .IsRequired();

        builder.Property(intervention => intervention.CreatedUtc)
            .IsRequired();

        builder.HasOne(intervention => intervention.Contact)
            .WithMany()
            .HasForeignKey(intervention => intervention.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(intervention => intervention.CreatedByUser)
            .WithMany()
            .HasForeignKey(intervention => intervention.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(intervention => new { intervention.ContactId, intervention.OccurredUtc });
        builder.HasIndex(intervention => new
        {
            intervention.ModuleKey,
            intervention.OriginType,
            intervention.OriginId,
            intervention.OccurredUtc
        });
        builder.HasIndex(intervention => new { intervention.CreatedByUserId, intervention.CreatedUtc });
    }
}
