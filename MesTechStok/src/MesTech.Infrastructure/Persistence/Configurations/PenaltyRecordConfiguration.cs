using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class PenaltyRecordConfiguration : IEntityTypeConfiguration<PenaltyRecord>
{
    public void Configure(EntityTypeBuilder<PenaltyRecord> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.PenaltyDate })
            .HasDatabaseName("IX_PenaltyRecords_Tenant_Date");

        builder.HasIndex(e => new { e.TenantId, e.PaymentStatus })
            .HasDatabaseName("IX_PenaltyRecords_Tenant_PaymentStatus");

        builder.HasIndex(e => new { e.TenantId, e.Source })
            .HasDatabaseName("IX_PenaltyRecords_Tenant_Source");

        builder.Property(e => e.Description).HasMaxLength(2000).IsRequired();
        builder.Property(e => e.Currency).HasMaxLength(10).IsRequired();
        builder.Property(e => e.ReferenceNumber).HasMaxLength(100);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.Amount).HasPrecision(18, 2);

        builder.Property(e => e.RowVersion).IsRowVersion();
    }
}
