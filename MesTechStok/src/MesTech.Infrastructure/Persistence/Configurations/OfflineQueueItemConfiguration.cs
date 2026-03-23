using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class OfflineQueueItemConfiguration : IEntityTypeConfiguration<OfflineQueueItem>
{
    public void Configure(EntityTypeBuilder<OfflineQueueItem> builder)
    {
        builder.Property(o => o.Channel).HasMaxLength(100);
        builder.Property(o => o.Direction).HasMaxLength(10);
        builder.Property(o => o.Status).HasMaxLength(50);
        builder.Property(o => o.LastError).HasMaxLength(2000);
        builder.Property(o => o.CorrelationId).HasMaxLength(100);

        builder.HasIndex(o => new { o.TenantId, o.Status })
            .HasDatabaseName("IX_OfflineQueueItems_Tenant_Status");

        builder.HasIndex(o => o.NextAttemptAt)
            .HasDatabaseName("IX_OfflineQueueItems_NextAttemptAt");
    }
}
