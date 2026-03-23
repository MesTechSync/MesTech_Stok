using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class ApiCallLogConfiguration : IEntityTypeConfiguration<ApiCallLog>
{
    public void Configure(EntityTypeBuilder<ApiCallLog> builder)
    {
        // Indexes
        builder.HasIndex(a => new { a.TenantId, a.TimestampUtc })
            .HasDatabaseName("IX_ApiCallLogs_Tenant_Timestamp");

        builder.HasIndex(a => new { a.TenantId, a.Endpoint, a.Success })
            .HasDatabaseName("IX_ApiCallLogs_Tenant_Endpoint_Success");

        builder.HasIndex(a => a.CorrelationId)
            .HasFilter("\"CorrelationId\" IS NOT NULL")
            .HasDatabaseName("IX_ApiCallLogs_CorrelationId");

        // String constraints
        builder.Property(a => a.Endpoint).HasMaxLength(500).IsRequired();
        builder.Property(a => a.Method).HasMaxLength(10).IsRequired();
        builder.Property(a => a.Category).HasMaxLength(100);
        builder.Property(a => a.CorrelationId).HasMaxLength(100);
    }
}
