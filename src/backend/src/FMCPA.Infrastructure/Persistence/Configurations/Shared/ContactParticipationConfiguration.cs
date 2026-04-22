using FMCPA.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Shared;

public sealed class ContactParticipationConfiguration : IEntityTypeConfiguration<ContactParticipation>
{
    public void Configure(EntityTypeBuilder<ContactParticipation> builder)
    {
        builder.ToTable("ContactParticipations");

        builder.HasKey(participation => participation.Id);

        builder.Property(participation => participation.ModuleCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(participation => participation.ContextType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(participation => participation.ContextKey)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(participation => participation.RoleName)
            .HasMaxLength(150);

        builder.Property(participation => participation.Notes)
            .HasMaxLength(1000);

        builder.Property(participation => participation.CreatedUtc)
            .IsRequired();

        builder.HasIndex(participation => new
        {
            participation.ContactId,
            participation.ModuleCode,
            participation.ContextType,
            participation.ContextKey
        });
    }
}
