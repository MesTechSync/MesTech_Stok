using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class FixedAssetConfiguration : IEntityTypeConfiguration<FixedAsset>
{
    public void Configure(EntityTypeBuilder<FixedAsset> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.AssetCode })
            .HasDatabaseName("IX_FixedAssets_Tenant_Code");

        builder.HasIndex(e => new { e.TenantId, e.IsActive })
            .HasDatabaseName("IX_FixedAssets_Tenant_Active");

        builder.Property(e => e.Name).HasMaxLength(300).IsRequired();
        builder.Property(e => e.AssetCode).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.AcquisitionCost).HasPrecision(18, 2);
        builder.Property(e => e.AccumulatedDepreciation).HasPrecision(18, 2);
        builder.Property(e => e.NetBookValue).HasPrecision(18, 2);
    }
}
