using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class AccessLogConfiguration : IEntityTypeConfiguration<AccessLog>
{
    public void Configure(EntityTypeBuilder<AccessLog> builder)
    {
        builder.Property(a => a.Action).HasMaxLength(200);
        builder.Property(a => a.Resource).HasMaxLength(500);
        builder.Property(a => a.IpAddress).HasMaxLength(50);
        builder.Property(a => a.UserAgent).HasMaxLength(500);
        builder.Property(a => a.AdditionalInfo).HasMaxLength(2000);
        builder.Property(a => a.CorrelationId).HasMaxLength(100);

        builder.HasIndex(a => new { a.TenantId, a.AccessTime })
            .HasDatabaseName("IX_AccessLogs_Tenant_AccessTime");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AccessLogs_UserId");
    }
}
