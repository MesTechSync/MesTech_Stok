using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        // Indexes
        builder.HasIndex(c => new { c.TenantId, c.Code })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Categories_Tenant_Code");

        builder.HasIndex(c => new { c.TenantId, c.ParentCategoryId })
            .HasDatabaseName("IX_Categories_Tenant_Parent");

        builder.HasIndex(c => new { c.TenantId, c.IsActive })
            .HasDatabaseName("IX_Categories_Tenant_Active");

        // String constraints
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Code).HasMaxLength(50).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.ImageUrl).HasMaxLength(500);
        builder.Property(c => c.Color).HasMaxLength(20);
        builder.Property(c => c.Icon).HasMaxLength(50);

        // Self-referencing hierarchy
        builder.HasMany(c => c.SubCategories)
            .WithOne()
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
