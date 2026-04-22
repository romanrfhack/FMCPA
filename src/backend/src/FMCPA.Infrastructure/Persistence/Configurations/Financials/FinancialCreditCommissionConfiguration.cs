using FMCPA.Domain.Entities.Financials;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FMCPA.Infrastructure.Persistence.Configurations.Financials;

public sealed class FinancialCreditCommissionConfiguration : IEntityTypeConfiguration<FinancialCreditCommission>
{
    public void Configure(EntityTypeBuilder<FinancialCreditCommission> builder)
    {
        builder.ToTable("FinancialCreditCommissions");

        builder.HasKey(commission => commission.Id);

        builder.Property(commission => commission.RecipientCategory)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(commission => commission.RecipientName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(commission => commission.BaseAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(commission => commission.CommissionAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(commission => commission.Notes)
            .HasMaxLength(1500);

        builder.Property(commission => commission.CreatedUtc)
            .IsRequired();

        builder.HasOne(commission => commission.FinancialCredit)
            .WithMany(credit => credit.Commissions)
            .HasForeignKey(commission => commission.FinancialCreditId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(commission => commission.CommissionType)
            .WithMany()
            .HasForeignKey(commission => commission.CommissionTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(commission => commission.RecipientContact)
            .WithMany()
            .HasForeignKey(commission => commission.RecipientContactId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(commission => commission.FinancialCreditId);
        builder.HasIndex(commission => commission.CommissionTypeId);
        builder.HasIndex(commission => commission.RecipientCategory);
    }
}
