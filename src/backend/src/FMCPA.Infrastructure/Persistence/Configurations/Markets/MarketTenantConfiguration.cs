using FMCPA.Domain.Entities.Markets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Markets;

public sealed class MarketTenantConfiguration : IEntityTypeConfiguration<MarketTenant>
{
    public void Configure(EntityTypeBuilder<MarketTenant> builder)
    {
        builder.ToTable("MarketTenants");

        builder.HasKey(tenant => tenant.Id);

        builder.Property(tenant => tenant.TenantName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(tenant => tenant.CertificateNumber)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(tenant => tenant.BusinessLine)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(tenant => tenant.MobilePhone)
            .HasMaxLength(30);

        builder.Property(tenant => tenant.WhatsAppPhone)
            .HasMaxLength(30);

        builder.Property(tenant => tenant.Email)
            .HasMaxLength(200);

        builder.Property(tenant => tenant.Notes)
            .HasMaxLength(1500);

        builder.Property(tenant => tenant.CertificateOriginalFileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(tenant => tenant.CertificateStoredRelativePath)
            .HasMaxLength(400)
            .IsRequired();

        builder.Property(tenant => tenant.CertificateContentType)
            .HasMaxLength(120);

        builder.Property(tenant => tenant.CertificateFileSizeBytes)
            .IsRequired();

        builder.Property(tenant => tenant.CertificateUploadedUtc)
            .IsRequired();

        builder.Property(tenant => tenant.CreatedUtc)
            .IsRequired();

        builder.HasOne(tenant => tenant.Contact)
            .WithMany()
            .HasForeignKey(tenant => tenant.ContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(tenant => tenant.MarketId);
        builder.HasIndex(tenant => tenant.CertificateValidityTo);
        builder.HasIndex(tenant => new { tenant.MarketId, tenant.CertificateValidityTo });
    }
}
