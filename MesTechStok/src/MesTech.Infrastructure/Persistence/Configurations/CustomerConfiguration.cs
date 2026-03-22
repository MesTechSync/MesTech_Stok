using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasIndex(c => new { c.TenantId, c.Email })
            .HasFilter("\"Email\" IS NOT NULL")
            .HasDatabaseName("IX_Customers_Tenant_Email");

        builder.HasIndex(c => new { c.TenantId, c.Phone })
            .HasFilter("\"Phone\" IS NOT NULL")
            .HasDatabaseName("IX_Customers_Tenant_Phone");

        builder.HasIndex(c => c.TaxNumber)
            .HasFilter("\"TaxNumber\" IS NOT NULL")
            .HasDatabaseName("IX_Customers_TaxNumber");

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(200);
        builder.Property(c => c.Phone).HasMaxLength(20);
        builder.Property(c => c.TaxNumber).HasMaxLength(20);
        builder.Property(c => c.TaxOffice).HasMaxLength(100);
        builder.Property(c => c.BillingAddress).HasMaxLength(500);
        builder.Property(c => c.City).HasMaxLength(100);
        builder.Property(c => c.Country).HasMaxLength(100);
    }
}
