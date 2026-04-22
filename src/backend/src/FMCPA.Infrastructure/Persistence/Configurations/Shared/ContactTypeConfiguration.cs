using FMCPA.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Shared;

public sealed class ContactTypeConfiguration : IEntityTypeConfiguration<ContactType>
{
    public void Configure(EntityTypeBuilder<ContactType> builder)
    {
        builder.ToTable("ContactTypes");

        builder.HasKey(contactType => contactType.Id);

        builder.Property(contactType => contactType.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(contactType => contactType.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(contactType => contactType.Description)
            .HasMaxLength(250);

        builder.Property(contactType => contactType.SortOrder)
            .IsRequired();

        builder.Property(contactType => contactType.IsActive)
            .IsRequired();

        builder.HasIndex(contactType => contactType.Code)
            .IsUnique();

        builder.HasData(SharedCatalogSeedData.ContactTypes);
    }
}
