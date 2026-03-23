using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// NotificationLog entity EF Core Fluent API configuration.
/// </summary>
public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        // Indexes
        builder.HasIndex(nl => new { nl.TenantId, nl.Channel })
            .HasDatabaseName("IX_NotificationLogs_Tenant_Channel");

        builder.HasIndex(nl => new { nl.TenantId, nl.Status })
            .HasDatabaseName("IX_NotificationLogs_Tenant_Status");

        builder.HasIndex(nl => new { nl.TenantId, nl.CreatedAt })
            .HasDatabaseName("IX_NotificationLogs_Tenant_CreatedAt");

        // String constraints
        builder.Property(nl => nl.Recipient).HasMaxLength(256).IsRequired();
        builder.Property(nl => nl.TemplateName).HasMaxLength(200).IsRequired();
        builder.Property(nl => nl.Content).HasMaxLength(4000).IsRequired();
        builder.Property(nl => nl.ErrorMessage).HasMaxLength(2000);
    }
}
