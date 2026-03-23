using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        builder.Property(l => l.Level).HasMaxLength(20);
        builder.Property(l => l.Category).HasMaxLength(100);
        builder.Property(l => l.Message).HasMaxLength(4000);
        builder.Property(l => l.Data).HasMaxLength(8000);
        builder.Property(l => l.UserId).HasMaxLength(100);
        builder.Property(l => l.Exception).HasMaxLength(4000);
        builder.Property(l => l.IpAddress).HasMaxLength(50);
        builder.Property(l => l.UserAgent).HasMaxLength(500);
        builder.Property(l => l.MachineName).HasMaxLength(100);

        builder.HasIndex(l => new { l.TenantId, l.Timestamp })
            .HasDatabaseName("IX_LogEntries_Tenant_Timestamp");

        builder.HasIndex(l => new { l.TenantId, l.Level })
            .HasDatabaseName("IX_LogEntries_Tenant_Level");
    }
}
