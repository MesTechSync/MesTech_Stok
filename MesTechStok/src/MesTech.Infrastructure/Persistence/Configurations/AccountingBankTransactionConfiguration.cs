using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class AccountingBankTransactionConfiguration : IEntityTypeConfiguration<BankTransaction>
{
    public void Configure(EntityTypeBuilder<BankTransaction> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(100);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(64);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.BankAccountId);
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("IX_BankTransactions_IdempotencyKey");
    }
}
