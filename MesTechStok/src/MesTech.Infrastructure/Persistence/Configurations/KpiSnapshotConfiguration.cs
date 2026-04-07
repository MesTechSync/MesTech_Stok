using MesTech.Domain.Entities.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class KpiSnapshotConfiguration : IEntityTypeConfiguration<KpiSnapshot>
{
    public void Configure(EntityTypeBuilder<KpiSnapshot> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Value).HasPrecision(18, 2);
        builder.Property(k => k.PreviousValue).HasPrecision(18, 2);
        builder.Property(k => k.PlatformCode).HasMaxLength(50);

        builder.HasIndex(k => k.TenantId).HasDatabaseName("IX_KpiSnapshots_TenantId");
        builder.HasIndex(k => new { k.TenantId, k.Type, k.SnapshotDate })
            .HasDatabaseName("IX_KpiSnapshots_Tenant_Type_Date");
    }
}
