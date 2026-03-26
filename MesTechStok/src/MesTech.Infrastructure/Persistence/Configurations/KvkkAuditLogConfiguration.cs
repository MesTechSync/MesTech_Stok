using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class KvkkAuditLogConfiguration : IEntityTypeConfiguration<KvkkAuditLog>
{
    public void Configure(EntityTypeBuilder<KvkkAuditLog> builder)
    {
        builder.ToTable("KvkkAuditLogs");

        builder.HasIndex(x => new { x.TenantId, x.OperationType })
            .HasDatabaseName("IX_KvkkAuditLogs_Tenant_OpType");

        builder.HasIndex(x => x.RequestedByUserId)
            .HasDatabaseName("IX_KvkkAuditLogs_User");

        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(4000);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
    }
}
