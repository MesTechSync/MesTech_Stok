using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class FulfillmentShipmentConfiguration : IEntityTypeConfiguration<FulfillmentShipment>
{
    public void Configure(EntityTypeBuilder<FulfillmentShipment> builder)
    {
        builder.HasKey(f => f.Id);

        builder.HasIndex(f => new { f.TenantId, f.Center, f.Status })
            .HasDatabaseName("IX_FulfillmentShipments_Tenant_Center_Status");

        builder.HasIndex(f => new { f.TenantId, f.CreatedAt })
            .HasDatabaseName("IX_FulfillmentShipments_Tenant_Created")
            .IsDescending(false, true);

        builder.HasIndex(f => f.TrackingNumber)
            .HasDatabaseName("IX_FulfillmentShipments_Tracking");

        builder.Property(f => f.TrackingNumber).HasMaxLength(100).IsRequired();
        builder.Property(f => f.Center).HasMaxLength(100).IsRequired();
        builder.Property(f => f.Status).HasMaxLength(20).IsRequired();
        builder.Property(f => f.CarrierCode).HasMaxLength(50);

        builder.HasQueryFilter(f => !f.IsDeleted);
    }
}
