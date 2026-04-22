using FMCPA.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Audit;

public sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.ModuleCode)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(item => item.ModuleName)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.EntityType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.EntityId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.ActionType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(item => item.Title)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.Detail)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(item => item.RelatedStatusCode)
            .HasMaxLength(64);

        builder.Property(item => item.Reference)
            .HasMaxLength(256);

        builder.Property(item => item.NavigationPath)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(item => item.MetadataJson)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(item => item.OccurredUtc);
        builder.HasIndex(item => new { item.ModuleCode, item.EntityType, item.EntityId });
        builder.HasIndex(item => new { item.IsCloseEvent, item.ModuleCode, item.OccurredUtc });
    }
}
