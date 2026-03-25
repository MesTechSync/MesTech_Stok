using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class BarcodeScanLogConfiguration : IEntityTypeConfiguration<BarcodeScanLog>
{
    public void Configure(EntityTypeBuilder<BarcodeScanLog> builder)
    {
        builder.Property(b => b.Barcode).HasMaxLength(200);
        builder.Property(b => b.Format).HasMaxLength(50);
        builder.Property(b => b.Source).HasMaxLength(100);
        builder.Property(b => b.DeviceId).HasMaxLength(100);
        builder.Property(b => b.ValidationMessage).HasMaxLength(500);
        builder.Property(b => b.CorrelationId).HasMaxLength(100);

        builder.HasIndex(b => new { b.TenantId, b.TimestampUtc })
            .HasDatabaseName("IX_BarcodeScanLogs_Tenant_Timestamp");

        builder.HasIndex(b => b.Barcode)
            .HasDatabaseName("IX_BarcodeScanLogs_Barcode");
    }
}
