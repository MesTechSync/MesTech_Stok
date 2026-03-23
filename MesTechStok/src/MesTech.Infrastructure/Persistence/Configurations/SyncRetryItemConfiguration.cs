using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class SyncRetryItemConfiguration : IEntityTypeConfiguration<SyncRetryItem>
{
    public void Configure(EntityTypeBuilder<SyncRetryItem> builder)
    {
        builder.Property(s => s.SyncType).HasMaxLength(100);
        builder.Property(s => s.ItemId).HasMaxLength(200);
        builder.Property(s => s.ItemType).HasMaxLength(200);
        builder.Property(s => s.LastError).HasMaxLength(4000);
        builder.Property(s => s.ErrorCategory).HasMaxLength(100);
        builder.Property(s => s.CorrelationId).HasMaxLength(100);
        builder.Property(s => s.AdditionalInfo).HasMaxLength(2000);

        builder.HasIndex(s => new { s.TenantId, s.IsResolved, s.NextRetryUtc })
            .HasDatabaseName("IX_SyncRetryItems_Tenant_Resolved_NextRetry");
    }
}
