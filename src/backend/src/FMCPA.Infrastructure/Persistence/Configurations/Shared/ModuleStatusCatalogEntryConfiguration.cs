using FMCPA.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Shared;

public sealed class ModuleStatusCatalogEntryConfiguration : IEntityTypeConfiguration<ModuleStatusCatalogEntry>
{
    public void Configure(EntityTypeBuilder<ModuleStatusCatalogEntry> builder)
    {
        builder.ToTable("ModuleStatusCatalog");

        builder.HasKey(moduleStatus => moduleStatus.Id);

        builder.Property(moduleStatus => moduleStatus.ModuleCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(moduleStatus => moduleStatus.ModuleName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(moduleStatus => moduleStatus.ContextCode)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(moduleStatus => moduleStatus.ContextName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(moduleStatus => moduleStatus.StatusCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(moduleStatus => moduleStatus.StatusName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(moduleStatus => moduleStatus.Description)
            .HasMaxLength(250);

        builder.Property(moduleStatus => moduleStatus.SortOrder)
            .IsRequired();

        builder.Property(moduleStatus => moduleStatus.IsClosed)
            .IsRequired();

        builder.Property(moduleStatus => moduleStatus.AlertsEnabledByDefault)
            .IsRequired();

        builder.Property(moduleStatus => moduleStatus.IsActive)
            .IsRequired();

        builder.HasIndex(moduleStatus => new { moduleStatus.ModuleCode, moduleStatus.ContextCode, moduleStatus.StatusCode })
            .IsUnique();

        builder.HasData(SharedCatalogSeedData.ModuleStatuses);
    }
}
