using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ProductSpecificationConfiguration : IEntityTypeConfiguration<ProductSpecification>
{
    public void Configure(EntityTypeBuilder<ProductSpecification> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.SpecGroup).HasMaxLength(200);
        builder.Property(s => s.SpecName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.SpecValue).HasMaxLength(2000);
        builder.Property(s => s.Unit).HasMaxLength(50);

        builder.HasIndex(s => s.TenantId).HasDatabaseName("IX_ProductSpec_TenantId");
        builder.HasIndex(s => new { s.ProductId, s.DisplayOrder })
            .HasDatabaseName("IX_ProductSpec_Product_Order");

        builder.HasOne(s => s.Product)
            .WithMany()
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
