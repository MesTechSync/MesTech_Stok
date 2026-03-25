using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        // Indexes
        builder.HasIndex(a => new { a.TenantId, a.EntityType, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_Tenant_Entity");

        builder.HasIndex(a => new { a.TenantId, a.Timestamp })
            .HasDatabaseName("IX_AuditLogs_Tenant_Timestamp");

        builder.HasIndex(a => new { a.TenantId, a.UserId })
            .HasDatabaseName("IX_AuditLogs_Tenant_User");

        // String constraints
        builder.Property(a => a.UserName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
        builder.Property(a => a.EntityType).HasMaxLength(200).IsRequired();
        builder.Property(a => a.OldValues).HasMaxLength(8000);
        builder.Property(a => a.NewValues).HasMaxLength(8000);
        builder.Property(a => a.IpAddress).HasMaxLength(50);
    }
}
