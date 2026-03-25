using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// Product entity EF Core Fluent API configuration.
/// Indexes, constraints, precision for the most queried entity.
/// </summary>
public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Indexes (sık sorgulanan alanlar)
        builder.HasIndex(p => new { p.TenantId, p.SKU })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Products_Tenant_SKU");

        builder.HasIndex(p => p.Barcode)
            .HasFilter("\"IsDeleted\" = false AND \"Barcode\" IS NOT NULL")
            .HasDatabaseName("IX_Products_Barcode");

        builder.HasIndex(p => new { p.TenantId, p.IsActive })
            .HasDatabaseName("IX_Products_Tenant_Active");

        builder.HasIndex(p => new { p.TenantId, p.CategoryId })
            .HasDatabaseName("IX_Products_Tenant_Category");

        // String constraints
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.SKU).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Barcode).HasMaxLength(50);
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.Property(p => p.Brand).HasMaxLength(100);
        builder.Property(p => p.Model).HasMaxLength(100);

        // Decimal precision (para alanları — 18,2)
        builder.Property(p => p.PurchasePrice).HasPrecision(18, 2);
        builder.Property(p => p.SalePrice).HasPrecision(18, 2);
        builder.Property(p => p.ListPrice).HasPrecision(18, 2);
        builder.Property(p => p.TaxRate).HasPrecision(5, 2);
        builder.Property(p => p.Weight).HasPrecision(18, 4);
    }
}
