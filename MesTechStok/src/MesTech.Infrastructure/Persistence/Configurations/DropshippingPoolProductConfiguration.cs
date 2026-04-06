using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class DropshippingPoolProductConfiguration : IEntityTypeConfiguration<DropshippingPoolProduct>
{
    public void Configure(EntityTypeBuilder<DropshippingPoolProduct> builder)
    {
        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.PoolPrice)
            .HasPrecision(18, 4);

        builder.Property(pp => pp.ReliabilityScore)
            .HasPrecision(8, 4)
            .HasDefaultValue(0m);

        builder.Property(pp => pp.ReliabilityColor)
            .HasDefaultValue(0);

        // Tenant + Pool bileşik index — havuz ürün listesi sorgusu için
        builder.HasIndex(pp => new { pp.TenantId, pp.PoolId })
            .HasDatabaseName("IX_DropshippingPoolProducts_Tenant_Pool");

        // Tenant + Product — bir ürünün hangi havuzlarda olduğunu bulmak için
        builder.HasIndex(pp => new { pp.TenantId, pp.ProductId })
            .HasDatabaseName("IX_DropshippingPoolProducts_Tenant_Product");

        // Aynı ürün aynı havuzda yalnızca bir kez görünebilir
        builder.HasIndex(pp => new { pp.PoolId, pp.ProductId })
            .IsUnique()
            .HasDatabaseName("UX_DropshippingPoolProducts_Pool_Product");

        builder.HasIndex(pp => pp.TenantId)
            .HasDatabaseName("IX_DropshippingPoolProducts_TenantId");

        // Pool → PoolProducts (HasMany konfigürasyonu DropshippingPoolConfiguration'da tanımlandı)
        // Burada Product FK'yi konfigüre ediyoruz
        builder.HasOne(pp => pp.Product)
            .WithMany()
            .HasForeignKey(pp => pp.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // AddedFromFeed — opsiyonel referans, feed silinirse null olur
        builder.HasOne<SupplierFeed>()
            .WithMany()
            .HasForeignKey(pp => pp.AddedFromFeedId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
