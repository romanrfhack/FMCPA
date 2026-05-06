using FMCPA.Domain.Entities.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Security;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUsers");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.UserName)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.NormalizedUserName)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.DisplayName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(item => item.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(item => item.RoleCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.SecurityStamp)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.IsActive)
            .IsRequired();

        builder.Property(item => item.CreatedUtc)
            .IsRequired();

        builder.Property(item => item.AccessFailedCount)
            .IsRequired();

        builder.Property(item => item.LockoutEndUtc)
            .HasColumnType("datetimeoffset");

        builder.HasIndex(item => item.NormalizedUserName)
            .IsUnique();
    }
}
