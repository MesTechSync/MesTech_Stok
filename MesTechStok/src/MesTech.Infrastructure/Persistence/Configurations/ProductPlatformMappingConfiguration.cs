using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ProductPlatformMappingConfiguration : IEntityTypeConfiguration<ProductPlatformMapping>
{
    public void Configure(EntityTypeBuilder<ProductPlatformMapping> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ExternalProductId).HasMaxLength(200);
        builder.Property(m => m.ExternalCategoryId).HasMaxLength(200);
        builder.Property(m => m.ExternalUrl).HasMaxLength(1000);

        builder.HasIndex(m => new { m.ProductId, m.StoreId }).IsUnique()
            .HasFilter("\"ProductVariantId\" IS NULL");

        builder.HasOne(m => m.Product)
            .WithMany(p => p.PlatformMappings)
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Store)
            .WithMany(s => s.ProductMappings)
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.ProductVariant)
            .WithMany(v => v.PlatformMappings)
            .HasForeignKey(m => m.ProductVariantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => m.TenantId).HasDatabaseName("ix_product_platform_mappings_tenant_id");
    }
}
