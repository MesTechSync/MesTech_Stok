using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(10);
        builder.Property(p => p.TransactionId).HasMaxLength(200);

        builder.HasIndex(p => new { p.TenantId, p.OrderId })
            .HasDatabaseName("IX_PaymentTransactions_Tenant_Order");

        builder.HasIndex(p => new { p.TenantId, p.Status })
            .HasDatabaseName("IX_PaymentTransactions_Tenant_Status");

        builder.Property(p => p.RowVersion).IsRowVersion();
        builder.Property<uint>("xmin").HasColumnType("xid").IsConcurrencyToken();
    }
}
