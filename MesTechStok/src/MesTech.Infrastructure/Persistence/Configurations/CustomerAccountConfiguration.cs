using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// CustomerAccount entity EF Core Fluent API configuration.
/// </summary>
public sealed class CustomerAccountConfiguration : IEntityTypeConfiguration<CustomerAccount>
{
    public void Configure(EntityTypeBuilder<CustomerAccount> builder)
    {
        // Indexes
        builder.HasIndex(ca => new { ca.TenantId, ca.AccountCode })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_CustomerAccounts_Tenant_AccountCode");

        builder.HasIndex(ca => new { ca.TenantId, ca.CustomerId })
            .HasDatabaseName("IX_CustomerAccounts_Tenant_Customer");

        builder.HasIndex(ca => new { ca.TenantId, ca.CustomerEmail })
            .HasFilter("\"CustomerEmail\" IS NOT NULL AND \"IsDeleted\" = false")
            .HasDatabaseName("IX_CustomerAccounts_Tenant_Email");

        // String constraints
        builder.Property(ca => ca.AccountCode).HasMaxLength(50).IsRequired();
        builder.Property(ca => ca.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(ca => ca.CustomerTaxNumber).HasMaxLength(50);
        builder.Property(ca => ca.CustomerTaxOffice).HasMaxLength(200);
        builder.Property(ca => ca.CustomerAddress).HasMaxLength(500);
        builder.Property(ca => ca.CustomerEmail).HasMaxLength(256);
        builder.Property(ca => ca.CustomerPhone).HasMaxLength(20);
        builder.Property(ca => ca.Currency).HasMaxLength(10);

        // Decimal precision
        builder.Property(ca => ca.CreditLimit).HasPrecision(18, 2);

        // Relationships
        builder.HasOne(ca => ca.Customer)
            .WithMany()
            .HasForeignKey(ca => ca.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(ca => ca.Transactions)
            .WithOne(at => at.CustomerAccount)
            .HasForeignKey(at => at.AccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optimistic concurrency — PostgreSQL xmin pattern (SQL Server IsRowVersion yerine)
        builder.Property<uint>("xmin").HasColumnType("xid").IsConcurrencyToken();
        builder.Ignore(ca => ca.RowVersion);
    }
}
