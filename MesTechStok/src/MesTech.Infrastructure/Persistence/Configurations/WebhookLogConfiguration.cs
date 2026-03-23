using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class WebhookLogConfiguration : IEntityTypeConfiguration<WebhookLog>
{
    public void Configure(EntityTypeBuilder<WebhookLog> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Platform).HasMaxLength(50);
        builder.Property(w => w.EventType).HasMaxLength(200);
        builder.Property(w => w.Signature).HasMaxLength(500);
        builder.Property(w => w.Error).HasMaxLength(2000);

        builder.HasIndex(w => new { w.TenantId, w.Platform, w.ReceivedAt })
            .HasDatabaseName("IX_WebhookLogs_Tenant_Platform_ReceivedAt");

        builder.HasIndex(w => new { w.IsValid, w.RetryCount })
            .HasDatabaseName("IX_WebhookLogs_Valid_Retry");
    }
}
