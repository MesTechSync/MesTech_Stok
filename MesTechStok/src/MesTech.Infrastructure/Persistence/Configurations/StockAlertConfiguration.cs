using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class StockAlertConfiguration : IEntityTypeConfiguration<StockAlert>
{
    public void Configure(EntityTypeBuilder<StockAlert> builder)
    {
        // Indexes
        builder.HasIndex(a => new { a.TenantId, a.ProductId, a.IsResolved })
            .HasDatabaseName("IX_StockAlerts_Tenant_Product_Resolved");

        builder.HasIndex(a => new { a.TenantId, a.AlertLevel })
            .HasDatabaseName("IX_StockAlerts_Tenant_Level");

        builder.HasIndex(a => new { a.TenantId, a.AlertDate })
            .HasDatabaseName("IX_StockAlerts_Tenant_Date");

        // String constraints
        builder.Property(a => a.Message).HasMaxLength(500);
        builder.Property(a => a.ResolvedBy).HasMaxLength(100);

        // Navigation
        builder.HasOne(a => a.Product)
            .WithMany()
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
