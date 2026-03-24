using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class AccountingSupplierAccountConfiguration : IEntityTypeConfiguration<AccountingSupplierAccount>
{
    public void Configure(EntityTypeBuilder<AccountingSupplierAccount> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.SupplierId })
            .HasDatabaseName("IX_AccountingSupplierAccounts_Tenant_Supplier");

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Currency).HasMaxLength(10);
        builder.Property(e => e.Balance).HasPrecision(18, 2);
    }
}
