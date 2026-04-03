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

        builder.Property<uint>("xmin").HasColumnType("xid").IsConcurrencyToken();
    }
}
