using MesTech.Domain.Dropshipping.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// DropshipOrder entity EF Core Fluent API configuration.
/// </summary>
public class DropshipOrderConfiguration : IEntityTypeConfiguration<DropshipOrder>
{
    public void Configure(EntityTypeBuilder<DropshipOrder> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_DropshipOrders_Tenant_Status");

        builder.HasIndex(e => new { e.TenantId, e.OrderId })
            .HasDatabaseName("IX_DropshipOrders_Tenant_Order");

        builder.HasIndex(e => new { e.TenantId, e.DropshipSupplierId })
            .HasDatabaseName("IX_DropshipOrders_Tenant_Supplier");

        builder.Property(e => e.SupplierOrderRef).HasMaxLength(200);
        builder.Property(e => e.SupplierTrackingNumber).HasMaxLength(200);
        builder.Property(e => e.FailureReason).HasMaxLength(2000);
    }
}
