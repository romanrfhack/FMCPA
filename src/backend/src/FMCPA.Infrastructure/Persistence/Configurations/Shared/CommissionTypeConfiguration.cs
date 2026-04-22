using FMCPA.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Shared;

public sealed class CommissionTypeConfiguration : IEntityTypeConfiguration<CommissionType>
{
    public void Configure(EntityTypeBuilder<CommissionType> builder)
    {
        builder.ToTable("CommissionTypes");

        builder.HasKey(commissionType => commissionType.Id);

        builder.Property(commissionType => commissionType.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(commissionType => commissionType.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(commissionType => commissionType.Description)
            .HasMaxLength(250);

        builder.Property(commissionType => commissionType.SortOrder)
            .IsRequired();

        builder.Property(commissionType => commissionType.IsActive)
            .IsRequired();

        builder.HasIndex(commissionType => commissionType.Code)
            .IsUnique();

        builder.HasData(SharedCatalogSeedData.CommissionTypes);
    }
}
