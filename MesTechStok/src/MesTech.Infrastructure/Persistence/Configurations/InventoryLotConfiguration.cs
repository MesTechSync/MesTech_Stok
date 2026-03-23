using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class InventoryLotConfiguration : IEntityTypeConfiguration<InventoryLot>
{
    public void Configure(EntityTypeBuilder<InventoryLot> builder)
    {
        // Indexes
        builder.HasIndex(l => new { l.TenantId, l.ProductId, l.LotNumber })
            .IsUnique()
            .HasDatabaseName("IX_InventoryLots_Tenant_Product_Lot");

        builder.HasIndex(l => new { l.TenantId, l.Status })
            .HasDatabaseName("IX_InventoryLots_Tenant_Status");

        builder.HasIndex(l => l.ExpiryDate)
            .HasFilter("\"ExpiryDate\" IS NOT NULL")
            .HasDatabaseName("IX_InventoryLots_Expiry");

        // String constraints
        builder.Property(l => l.LotNumber).HasMaxLength(100).IsRequired();

        // Decimal precision
        builder.Property(l => l.ReceivedQty).HasPrecision(18, 2);
        builder.Property(l => l.RemainingQty).HasPrecision(18, 2);
    }
}
