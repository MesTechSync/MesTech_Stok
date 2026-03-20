using MesTech.Domain.Entities.Erp;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class ErpConflictLogConfiguration : IEntityTypeConfiguration<ErpConflictLog>
{
    public void Configure(EntityTypeBuilder<ErpConflictLog> builder)
    {
        builder.ToTable("erp_conflict_logs");

        builder.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.EntityCode).HasMaxLength(100).IsRequired();
        builder.Property(e => e.MestechValue).HasMaxLength(500);
        builder.Property(e => e.ErpValue).HasMaxLength(500);
        builder.Property(e => e.Winner).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Resolution).HasMaxLength(20).HasDefaultValue("Auto");

        builder.HasIndex(e => e.TenantId).HasDatabaseName("IX_ErpConflictLogs_TenantId");
        builder.HasIndex(e => new { e.EntityType, e.EntityCode })
            .HasDatabaseName("IX_ErpConflictLogs_Entity");
        builder.HasIndex(e => e.Provider)
            .HasDatabaseName("IX_ErpConflictLogs_Provider");
    }
}
