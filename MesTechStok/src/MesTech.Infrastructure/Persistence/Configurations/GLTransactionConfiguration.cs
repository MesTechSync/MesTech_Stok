using MesTech.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class GLTransactionConfiguration : IEntityTypeConfiguration<GLTransaction>
{
    public void Configure(EntityTypeBuilder<GLTransaction> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Currency).HasMaxLength(10);
        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.ReferenceNumber).HasMaxLength(100);
        builder.Property(e => e.Amount).HasPrecision(18, 4);
        builder.Property(e => e.ExchangeRate).HasPrecision(18, 6);

        builder.HasIndex(e => new { e.TenantId, e.TransactionDate })
            .HasDatabaseName("IX_GLTransactions_Tenant_Date");

        builder.HasIndex(e => new { e.TenantId, e.GLAccountId })
            .HasDatabaseName("IX_GLTransactions_Tenant_Account");

        builder.HasIndex(e => new { e.TenantId, e.OrderId })
            .HasDatabaseName("IX_GLTransactions_Tenant_Order");
    }
}
