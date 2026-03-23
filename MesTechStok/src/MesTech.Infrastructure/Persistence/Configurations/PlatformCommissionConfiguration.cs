using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class PlatformCommissionConfiguration : IEntityTypeConfiguration<PlatformCommission>
{
    public void Configure(EntityTypeBuilder<PlatformCommission> builder)
    {
        builder.Property(c => c.CategoryName).HasMaxLength(200);
        builder.Property(c => c.PlatformCategoryId).HasMaxLength(100);
        builder.Property(c => c.Rate).HasPrecision(18, 2);
        builder.Property(c => c.MinAmount).HasPrecision(18, 2);
        builder.Property(c => c.MaxAmount).HasPrecision(18, 2);
        builder.Property(c => c.Currency).HasMaxLength(10);
        builder.Property(c => c.Notes).HasMaxLength(1000);

        builder.HasIndex(c => new { c.TenantId, c.Platform, c.CategoryName })
            .HasDatabaseName("IX_PlatformCommissions_Tenant_Platform_Category");

        builder.HasIndex(c => new { c.TenantId, c.IsActive })
            .HasDatabaseName("IX_PlatformCommissions_Tenant_Active");
    }
}
