using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// SupplierAccount entity EF Core Fluent API configuration.
/// </summary>
public class SupplierAccountConfiguration : IEntityTypeConfiguration<SupplierAccount>
{
    public void Configure(EntityTypeBuilder<SupplierAccount> builder)
    {
        // Indexes
        builder.HasIndex(sa => new { sa.TenantId, sa.AccountCode })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_SupplierAccounts_Tenant_AccountCode");

        builder.HasIndex(sa => new { sa.TenantId, sa.SupplierId })
            .HasDatabaseName("IX_SupplierAccounts_Tenant_Supplier");

        builder.HasIndex(sa => new { sa.TenantId, sa.SupplierEmail })
            .HasFilter("\"SupplierEmail\" IS NOT NULL AND \"IsDeleted\" = false")
            .HasDatabaseName("IX_SupplierAccounts_Tenant_Email");

        // String constraints
        builder.Property(sa => sa.AccountCode).HasMaxLength(50).IsRequired();
        builder.Property(sa => sa.SupplierName).HasMaxLength(200).IsRequired();
        builder.Property(sa => sa.SupplierTaxNumber).HasMaxLength(50);
        builder.Property(sa => sa.SupplierTaxOffice).HasMaxLength(200);
        builder.Property(sa => sa.SupplierAddress).HasMaxLength(500);
        builder.Property(sa => sa.SupplierEmail).HasMaxLength(256);
        builder.Property(sa => sa.SupplierPhone).HasMaxLength(20);
        builder.Property(sa => sa.Currency).HasMaxLength(10);

        // Relationships
        builder.HasOne(sa => sa.Supplier)
            .WithMany()
            .HasForeignKey(sa => sa.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(sa => sa.Transactions)
            .WithOne(at => at.SupplierAccount)
            .HasForeignKey(at => at.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
