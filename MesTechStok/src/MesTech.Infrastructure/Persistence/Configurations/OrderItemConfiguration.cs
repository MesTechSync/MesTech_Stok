using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// OrderItem entity EF Core Fluent API configuration.
/// </summary>
public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        // Indexes
        builder.HasIndex(oi => new { oi.TenantId, oi.OrderId })
            .HasDatabaseName("IX_OrderItems_Tenant_Order");

        builder.HasIndex(oi => new { oi.TenantId, oi.ProductId })
            .HasDatabaseName("IX_OrderItems_Tenant_Product");

        builder.HasIndex(oi => oi.ProductSKU)
            .HasDatabaseName("IX_OrderItems_ProductSKU");

        // String constraints
        builder.Property(oi => oi.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(oi => oi.ProductSKU).HasMaxLength(50).IsRequired();

        // Decimal precision
        builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
        builder.Property(oi => oi.TotalPrice).HasPrecision(18, 2);
        builder.Property(oi => oi.TaxRate).HasPrecision(5, 2);
        builder.Property(oi => oi.TaxAmount).HasPrecision(18, 2);
    }
}
