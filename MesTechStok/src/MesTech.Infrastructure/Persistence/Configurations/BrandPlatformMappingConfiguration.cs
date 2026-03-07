using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class BrandPlatformMappingConfiguration : IEntityTypeConfiguration<BrandPlatformMapping>
{
    public void Configure(EntityTypeBuilder<BrandPlatformMapping> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasKey(m => m.Id);
        builder.Property(m => m.ExternalBrandId).HasMaxLength(200);
        builder.Property(m => m.ExternalBrandName).HasMaxLength(200);

        builder.HasIndex(m => new { m.BrandId, m.StoreId }).IsUnique();

        builder.HasOne(m => m.Brand)
            .WithMany(b => b.PlatformMappings)
            .HasForeignKey(m => m.BrandId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Store)
            .WithMany()
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
