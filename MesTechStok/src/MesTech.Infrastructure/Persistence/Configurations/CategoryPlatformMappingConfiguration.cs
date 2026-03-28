using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class CategoryPlatformMappingConfiguration : IEntityTypeConfiguration<CategoryPlatformMapping>
{
    public void Configure(EntityTypeBuilder<CategoryPlatformMapping> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.ExternalCategoryId).HasMaxLength(200);
        builder.Property(m => m.ExternalCategoryName).HasMaxLength(200);

        builder.HasIndex(m => new { m.CategoryId, m.StoreId }).IsUnique();

        builder.HasOne(m => m.Category)
            .WithMany()
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Store)
            .WithMany()
            .HasForeignKey(m => m.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.TenantId).HasDatabaseName("ix_category_platform_mappings_tenant_id");
    }
}
