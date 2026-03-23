using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class CiceksepetiCategoryConfiguration : IEntityTypeConfiguration<CiceksepetiCategory>
{
    public void Configure(EntityTypeBuilder<CiceksepetiCategory> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.CategoryName).HasMaxLength(300);

        builder.HasIndex(e => new { e.TenantId, e.CiceksepetiCategoryId })
            .IsUnique()
            .HasDatabaseName("IX_CiceksepetiCategories_Tenant_PlatformId");

        builder.HasIndex(e => new { e.TenantId, e.ParentCategoryId })
            .HasDatabaseName("IX_CiceksepetiCategories_Tenant_Parent");
    }
}
