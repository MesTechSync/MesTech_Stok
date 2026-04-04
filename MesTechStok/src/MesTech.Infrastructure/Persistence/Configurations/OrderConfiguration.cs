using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// Order entity EF Core configuration — indexes, cascade, precision.
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // Indexes
        builder.HasIndex(o => new { o.TenantId, o.OrderNumber })
            .IsUnique()
            .HasDatabaseName("IX_Orders_Tenant_Number");

        builder.HasIndex(o => new { o.TenantId, o.CustomerId })
            .HasDatabaseName("IX_Orders_Tenant_Customer");

        builder.HasIndex(o => new { o.TenantId, o.OrderDate })
            .HasDatabaseName("IX_Orders_Tenant_Date");

        builder.HasIndex(o => o.ExternalOrderId)
            .HasFilter("\"ExternalOrderId\" IS NOT NULL")
            .HasDatabaseName("IX_Orders_ExternalId");

        // String constraints
        builder.Property(o => o.OrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(o => o.ExternalOrderId).HasMaxLength(100);
        builder.Property(o => o.PlatformOrderNumber).HasMaxLength(100);
        builder.Property(o => o.CustomerName).HasMaxLength(200);
        builder.Property(o => o.CustomerEmail).HasMaxLength(200);
        builder.Property(o => o.RecipientPhone).HasMaxLength(20);
        builder.Property(o => o.ShippingAddress).HasMaxLength(500);
        builder.Property(o => o.TrackingNumber).HasMaxLength(100);
        builder.Property(o => o.CargoBarcode).HasMaxLength(100);
        builder.Property(o => o.Notes).HasMaxLength(2000);

        // Decimal precision
        builder.Property(o => o.SubTotal).HasPrecision(18, 2);
        builder.Property(o => o.TaxAmount).HasPrecision(18, 2);
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);
        builder.Property(o => o.CommissionAmount).HasPrecision(18, 2);
        builder.Property(o => o.CommissionRate).HasPrecision(5, 4);
        builder.Property(o => o.CargoExpenseAmount).HasPrecision(18, 2);

        // Cascade delete — OrderItem'lar sipariş silindiğinde silinir
        builder.HasMany(o => o.OrderItems)
            .WithOne()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Optimistic concurrency — PostgreSQL xmin pattern (SQL Server IsRowVersion yerine)
        builder.Property<uint>("xmin").HasColumnType("xid").IsConcurrencyToken();
        builder.Ignore(o => o.RowVersion);
    }
}
