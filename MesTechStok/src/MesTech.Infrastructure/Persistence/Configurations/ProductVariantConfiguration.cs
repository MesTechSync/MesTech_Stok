using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.VariantSKU).HasMaxLength(100);
        builder.Property(v => v.VariantBarcode).HasMaxLength(100);
        builder.Property(v => v.Color).HasMaxLength(50);
        builder.Property(v => v.Size).HasMaxLength(50);

        builder.Property(v => v.Price).HasPrecision(18, 4);
        builder.Property(v => v.PriceOverride).HasPrecision(18, 4);

        builder.HasIndex(v => v.VariantSKU).IsUnique().HasFilter("\"VariantSKU\" IS NOT NULL");

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => v.TenantId).HasDatabaseName("ix_product_variants_tenant_id");
    }
}
