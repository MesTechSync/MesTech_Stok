using MesTech.Domain.Entities.Erp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// ErpSyncLog EF Core configuration — tablo adi, index'ler, alan kisitlamalari.
/// Dalga 11: ERP entegrasyonu icin eklendi.
/// </summary>
public sealed class ErpSyncLogConfiguration : IEntityTypeConfiguration<ErpSyncLog>
{
    public void Configure(EntityTypeBuilder<ErpSyncLog> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("erp_sync_logs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ErpRef).HasMaxLength(200);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(e => e.ErrorDetails).HasColumnType("text");
        builder.Property(e => e.TriggeredBy).HasMaxLength(50).HasDefaultValue("Manual");

        // Provider enum -> string donusumu (okunabilirlik)
        builder.Property(x => x.Provider)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Performance indexes
        builder.HasIndex(x => x.TenantId).HasDatabaseName("IX_ErpSyncLogs_TenantId");
        builder.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("IX_ErpSyncLogs_Entity");
        builder.HasIndex(x => x.NextRetryAt).HasDatabaseName("IX_ErpSyncLogs_NextRetryAt");
        builder.HasIndex(x => new { x.TenantId, x.Provider, x.Success })
            .HasDatabaseName("IX_ErpSyncLogs_Provider_Success");
    }
}
