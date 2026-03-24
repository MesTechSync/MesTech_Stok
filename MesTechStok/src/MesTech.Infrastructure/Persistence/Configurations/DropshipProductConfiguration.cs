using MesTech.Domain.Dropshipping.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// DropshipProduct entity EF Core Fluent API configuration.
/// </summary>
public class DropshipProductConfiguration : IEntityTypeConfiguration<DropshipProduct>
{
    public void Configure(EntityTypeBuilder<DropshipProduct> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.DropshipSupplierId })
            .HasDatabaseName("IX_DropshipProducts_Tenant_Supplier");

        builder.HasIndex(e => new { e.TenantId, e.ExternalProductId })
            .HasDatabaseName("IX_DropshipProducts_Tenant_ExternalId");

        builder.HasIndex(e => new { e.TenantId, e.ProductId })
            .HasFilter("\"ProductId\" IS NOT NULL")
            .HasDatabaseName("IX_DropshipProducts_Tenant_LinkedProduct");

        builder.Property(e => e.ExternalProductId).HasMaxLength(200).IsRequired();
        builder.Property(e => e.ExternalUrl).HasMaxLength(500);
        builder.Property(e => e.Title).HasMaxLength(500).IsRequired();
        builder.Property(e => e.OriginalPrice).HasPrecision(18, 2);
        builder.Property(e => e.SellingPrice).HasPrecision(18, 2);
    }
}
