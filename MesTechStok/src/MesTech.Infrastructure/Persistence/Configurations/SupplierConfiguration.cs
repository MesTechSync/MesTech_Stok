using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        // Indexes
        builder.HasIndex(s => new { s.TenantId, s.Code })
            .IsUnique()
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Suppliers_Tenant_Code");

        builder.HasIndex(s => new { s.TenantId, s.IsActive })
            .HasDatabaseName("IX_Suppliers_Tenant_Active");

        builder.HasIndex(s => new { s.TenantId, s.Name })
            .HasDatabaseName("IX_Suppliers_Tenant_Name");

        // String constraints
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Code).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.ContactPerson).HasMaxLength(200);
        builder.Property(s => s.Email).HasMaxLength(200);
        builder.Property(s => s.Phone).HasMaxLength(30);
        builder.Property(s => s.Mobile).HasMaxLength(30);
        builder.Property(s => s.Fax).HasMaxLength(30);
        builder.Property(s => s.Website).HasMaxLength(500);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.State).HasMaxLength(100);
        builder.Property(s => s.PostalCode).HasMaxLength(20);
        builder.Property(s => s.Country).HasMaxLength(100);
        builder.Property(s => s.TaxNumber).HasMaxLength(20);
        builder.Property(s => s.TaxOffice).HasMaxLength(100);
        builder.Property(s => s.VatNumber).HasMaxLength(30);
        builder.Property(s => s.TradeRegisterNumber).HasMaxLength(50);
        builder.Property(s => s.Currency).HasMaxLength(10);
        builder.Property(s => s.Notes).HasMaxLength(2000);
        builder.Property(s => s.DocumentUrls).HasMaxLength(4000);

        // Decimal precision
        builder.Property(s => s.CreditLimit).HasPrecision(18, 2);
        builder.Property(s => s.CurrentBalance).HasPrecision(18, 2);
        builder.Property(s => s.DiscountRate).HasPrecision(5, 2);
    }
}
