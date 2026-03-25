using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// AccountTransaction entity EF Core Fluent API configuration.
/// </summary>
public sealed class AccountTransactionConfiguration : IEntityTypeConfiguration<AccountTransaction>
{
    public void Configure(EntityTypeBuilder<AccountTransaction> builder)
    {
        // Indexes
        builder.HasIndex(at => new { at.TenantId, at.AccountId, at.TransactionDate })
            .HasDatabaseName("IX_AccountTransactions_Tenant_Account_Date");

        builder.HasIndex(at => new { at.TenantId, at.Type })
            .HasDatabaseName("IX_AccountTransactions_Tenant_Type");

        builder.HasIndex(at => new { at.TenantId, at.TransactionDate })
            .HasDatabaseName("IX_AccountTransactions_Tenant_Date");

        builder.HasIndex(at => at.DocumentNumber)
            .HasFilter("\"DocumentNumber\" IS NOT NULL")
            .HasDatabaseName("IX_AccountTransactions_DocumentNumber");

        // String constraints
        builder.Property(at => at.Description).HasMaxLength(2000);
        builder.Property(at => at.DocumentNumber).HasMaxLength(50);
        builder.Property(at => at.Currency).HasMaxLength(10);

        // Decimal precision
        builder.Property(at => at.DebitAmount).HasPrecision(18, 2);
        builder.Property(at => at.CreditAmount).HasPrecision(18, 2);
    }
}
