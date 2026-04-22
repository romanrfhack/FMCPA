using FMCPA.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");

        builder.HasKey(setting => setting.Id);

        builder.Property(setting => setting.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(setting => setting.Value)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(setting => setting.CreatedUtc)
            .IsRequired();

        builder.HasIndex(setting => setting.Key)
            .IsUnique();
    }
}
