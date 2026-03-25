using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        // Indexes
        builder.HasIndex(h => new { h.TenantId, h.ProductId, h.ChangedAt })
            .HasDatabaseName("IX_PriceHistory_Tenant_Product_Date");

        builder.HasIndex(h => new { h.TenantId, h.Platform })
            .HasFilter("\"Platform\" IS NOT NULL")
            .HasDatabaseName("IX_PriceHistory_Tenant_Platform");

        // Decimal precision
        builder.Property(h => h.OldPrice).HasPrecision(18, 2);
        builder.Property(h => h.NewPrice).HasPrecision(18, 2);
        builder.Property(h => h.OldListPrice).HasPrecision(18, 2);
        builder.Property(h => h.NewListPrice).HasPrecision(18, 2);

        // String constraints
        builder.Property(h => h.Currency).HasMaxLength(10);
        builder.Property(h => h.ChangedBy).HasMaxLength(100);
        builder.Property(h => h.ChangeReason).HasMaxLength(500);

        // Navigation
        builder.HasOne(h => h.Product)
            .WithMany()
            .HasForeignKey(h => h.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
