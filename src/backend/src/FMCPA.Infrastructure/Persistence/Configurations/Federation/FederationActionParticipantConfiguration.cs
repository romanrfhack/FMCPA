using FMCPA.Domain.Entities.Federation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Federation;

public sealed class FederationActionParticipantConfiguration : IEntityTypeConfiguration<FederationActionParticipant>
{
    public void Configure(EntityTypeBuilder<FederationActionParticipant> builder)
    {
        builder.ToTable("FederationActionParticipants");

        builder.HasKey(participant => participant.Id);

        builder.Property(participant => participant.ParticipantSide)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(participant => participant.ParticipantName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(participant => participant.OrganizationOrDependency)
            .HasMaxLength(200);

        builder.Property(participant => participant.RoleTitle)
            .HasMaxLength(200);

        builder.Property(participant => participant.Notes)
            .HasMaxLength(1000);

        builder.Property(participant => participant.CreatedUtc)
            .IsRequired();

        builder.HasOne(participant => participant.FederationAction)
            .WithMany(action => action.Participants)
            .HasForeignKey(participant => participant.FederationActionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(participant => participant.Contact)
            .WithMany()
            .HasForeignKey(participant => participant.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(participant => participant.FederationActionId);
        builder.HasIndex(participant => participant.ContactId);
        builder.HasIndex(participant => participant.ParticipantSide);
    }
}
