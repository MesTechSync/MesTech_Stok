using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class DropshippingPoolConfiguration : IEntityTypeConfiguration<DropshippingPool>
{
    public void Configure(EntityTypeBuilder<DropshippingPool> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.PricingStrategy)
            .HasConversion<int>();

        // Tenant bazlı sorgular için (Global Query Filter) composite index
        builder.HasIndex(p => p.TenantId)
            .HasDatabaseName("IX_DropshippingPools_TenantId");

        builder.HasIndex(p => new { p.TenantId, p.IsActive })
            .HasDatabaseName("IX_DropshippingPools_Tenant_Active");

        builder.HasIndex(p => new { p.TenantId, p.IsPublic })
            .HasDatabaseName("IX_DropshippingPools_Tenant_Public");

        // Products navigation — owned by Pool, cascade on delete
        builder.HasMany(p => p.Products)
            .WithOne(pp => pp.Pool)
            .HasForeignKey(pp => pp.PoolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
