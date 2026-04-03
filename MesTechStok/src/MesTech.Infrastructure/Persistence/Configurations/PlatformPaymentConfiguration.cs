using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class PlatformPaymentConfiguration : IEntityTypeConfiguration<PlatformPayment>
{
    public void Configure(EntityTypeBuilder<PlatformPayment> builder)
    {
        builder.Property(p => p.GrossSales).HasPrecision(18, 2);
        builder.Property(p => p.TotalCommission).HasPrecision(18, 2);
        builder.Property(p => p.TotalShippingCost).HasPrecision(18, 2);
        builder.Property(p => p.TotalReturnDeduction).HasPrecision(18, 2);
        builder.Property(p => p.OtherDeductions).HasPrecision(18, 2);
        builder.Property(p => p.NetAmount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(10);
        builder.Property(p => p.BankReference).HasMaxLength(200);
        builder.Property(p => p.PlatformPaymentId).HasMaxLength(200);
        builder.Property(p => p.Notes).HasMaxLength(2000);

        builder.HasIndex(p => new { p.TenantId, p.Platform, p.PeriodStart })
            .HasDatabaseName("IX_PlatformPayments_Tenant_Platform_Period");

        builder.HasIndex(p => new { p.TenantId, p.Status })
            .HasDatabaseName("IX_PlatformPayments_Tenant_Status");

        builder.HasOne(p => p.Store)
            .WithMany()
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(p => new { p.TenantId, p.StoreId })
            .HasDatabaseName("IX_PlatformPayments_Tenant_Store");
    }
}
