using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class ProductSetConfiguration : IEntityTypeConfiguration<ProductSet>
{
    public void Configure(EntityTypeBuilder<ProductSet> builder)
    {
        builder.Property(p => p.Name).HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Price).HasPrecision(18, 2);

        builder.HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey(i => i.ProductSetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("IX_ProductSets_TenantId");
    }
}
