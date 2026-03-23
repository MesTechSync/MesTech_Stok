using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class SyncLogConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> builder)
    {
        builder.Property(s => s.PlatformCode).HasMaxLength(50);
        builder.Property(s => s.EntityType).HasMaxLength(200);
        builder.Property(s => s.EntityId).HasMaxLength(200);
        builder.Property(s => s.ErrorMessage).HasMaxLength(4000);
        builder.Property(s => s.CorrelationId).HasMaxLength(100);

        builder.HasIndex(s => new { s.TenantId, s.PlatformCode, s.StartedAt })
            .HasDatabaseName("IX_SyncLogs_Tenant_Platform_StartedAt");

        builder.HasIndex(s => new { s.PlatformCode, s.IsSuccess })
            .HasDatabaseName("IX_SyncLogs_Platform_Success");
    }
}
