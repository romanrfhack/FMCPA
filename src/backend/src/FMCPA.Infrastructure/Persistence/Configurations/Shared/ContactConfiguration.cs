using FMCPA.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Shared;

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");

        builder.HasKey(contact => contact.Id);

        builder.Property(contact => contact.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(contact => contact.OrganizationOrDependency)
            .HasMaxLength(200);

        builder.Property(contact => contact.RoleTitle)
            .HasMaxLength(150);

        builder.Property(contact => contact.MobilePhone)
            .HasMaxLength(30);

        builder.Property(contact => contact.WhatsAppPhone)
            .HasMaxLength(30);

        builder.Property(contact => contact.Email)
            .HasMaxLength(200);

        builder.Property(contact => contact.Notes)
            .HasMaxLength(1000);

        builder.Property(contact => contact.CreatedUtc)
            .IsRequired();

        builder.HasOne(contact => contact.ContactType)
            .WithMany(contactType => contactType.Contacts)
            .HasForeignKey(contact => contact.ContactTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(contact => contact.Participations)
            .WithOne(participation => participation.Contact)
            .HasForeignKey(participation => participation.ContactId);
    }
}
