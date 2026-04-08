using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ReconciliationMatchConfiguration : IEntityTypeConfiguration<ReconciliationMatch>
{
    public void Configure(EntityTypeBuilder<ReconciliationMatch> builder)
    {
        builder.Property(x => x.Confidence).HasPrecision(5, 4);
        builder.Property(x => x.ReviewedBy).HasMaxLength(100);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.SettlementBatchId);
        builder.HasIndex(x => x.BankTransactionId);
        builder.HasIndex(x => x.Status);

        // G132: Idempotency — aynı settlement+bankTx çifti ile çift match önleme
        // Nullable FK'lar PostgreSQL'de partial unique index gerektirir (NULL'lar unique sayılmaz)
        builder.HasIndex(x => new { x.TenantId, x.SettlementBatchId, x.BankTransactionId })
            .IsUnique()
            .HasFilter("\"SettlementBatchId\" IS NOT NULL AND \"BankTransactionId\" IS NOT NULL")
            .HasDatabaseName("IX_ReconciliationMatches_Idempotency");

        builder.Property<uint>("xmin").HasColumnType("xid").IsConcurrencyToken();
    }
}
