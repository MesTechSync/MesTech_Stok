using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class ProductSetItemConfiguration : IEntityTypeConfiguration<ProductSetItem>
{
    public void Configure(EntityTypeBuilder<ProductSetItem> builder)
    {
        builder.HasIndex(i => new { i.ProductSetId, i.ProductId })
            .IsUnique()
            .HasDatabaseName("IX_ProductSetItems_Set_Product");
    }
}
