using MesTech.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.AccountName).HasMaxLength(200);
        builder.Property(e => e.BankName).HasMaxLength(200);
        builder.Property(e => e.IBAN).HasMaxLength(34);
        builder.Property(e => e.AccountNumber).HasMaxLength(50);
        builder.Property(e => e.Currency).HasMaxLength(10);
        builder.Property(e => e.Balance).HasPrecision(18, 4);

        builder.HasIndex(e => new { e.TenantId, e.IsActive })
            .HasDatabaseName("IX_BankAccounts_Tenant_Active");

        builder.HasIndex(e => new { e.TenantId, e.IBAN })
            .IsUnique()
            .HasFilter("\"IBAN\" IS NOT NULL")
            .HasDatabaseName("IX_BankAccounts_Tenant_IBAN");
    }
}
