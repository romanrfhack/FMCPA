using FMCPA.Domain.Entities.Financials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Financials;

public sealed class FinancialCreditConfiguration : IEntityTypeConfiguration<FinancialCredit>
{
    public void Configure(EntityTypeBuilder<FinancialCredit> builder)
    {
        builder.ToTable("FinancialCredits");

        builder.HasKey(credit => credit.Id);

        builder.Property(credit => credit.PromoterName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(credit => credit.BeneficiaryName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(credit => credit.PhoneNumber)
            .HasMaxLength(30);

        builder.Property(credit => credit.WhatsAppPhone)
            .HasMaxLength(30);

        builder.Property(credit => credit.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(credit => credit.Notes)
            .HasMaxLength(1500);

        builder.Property(credit => credit.CreatedUtc)
            .IsRequired();

        builder.HasOne(credit => credit.FinancialPermit)
            .WithMany(permit => permit.Credits)
            .HasForeignKey(credit => credit.FinancialPermitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(credit => credit.PromoterContact)
            .WithMany()
            .HasForeignKey(credit => credit.PromoterContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(credit => credit.BeneficiaryContact)
            .WithMany()
            .HasForeignKey(credit => credit.BeneficiaryContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(credit => credit.FinancialPermitId);
        builder.HasIndex(credit => credit.AuthorizationDate);
        builder.HasIndex(credit => new { credit.FinancialPermitId, credit.AuthorizationDate });
    }
}
