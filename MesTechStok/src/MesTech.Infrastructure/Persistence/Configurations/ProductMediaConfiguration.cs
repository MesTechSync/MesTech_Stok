using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ProductMediaConfiguration : IEntityTypeConfiguration<ProductMedia>
{
    public void Configure(EntityTypeBuilder<ProductMedia> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Url).HasMaxLength(2000).IsRequired();
        builder.Property(m => m.ThumbnailUrl).HasMaxLength(2000);
        builder.Property(m => m.AltText).HasMaxLength(500);

        builder.HasIndex(m => m.TenantId).HasDatabaseName("IX_ProductMedia_TenantId");
        builder.HasIndex(m => new { m.ProductId, m.SortOrder })
            .HasDatabaseName("IX_ProductMedia_Product_Sort");

        builder.HasOne(m => m.Product)
            .WithMany()
            .HasForeignKey(m => m.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Variant)
            .WithMany()
            .HasForeignKey(m => m.VariantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
