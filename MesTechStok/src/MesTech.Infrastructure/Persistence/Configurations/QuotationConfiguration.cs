using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.Property(q => q.QuotationNumber).HasMaxLength(50);
        builder.Property(q => q.CustomerName).HasMaxLength(300);
        builder.Property(q => q.CustomerTaxNumber).HasMaxLength(20);
        builder.Property(q => q.CustomerTaxOffice).HasMaxLength(200);
        builder.Property(q => q.CustomerAddress).HasMaxLength(500);
        builder.Property(q => q.CustomerEmail).HasMaxLength(200);
        builder.Property(q => q.SubTotal).HasPrecision(18, 2);
        builder.Property(q => q.TaxTotal).HasPrecision(18, 2);
        builder.Property(q => q.GrandTotal).HasPrecision(18, 2);
        builder.Property(q => q.Currency).HasMaxLength(10);
        builder.Property(q => q.Notes).HasMaxLength(2000);
        builder.Property(q => q.Terms).HasMaxLength(2000);

        builder.HasMany(q => q.Lines)
            .WithOne(l => l.Quotation)
            .HasForeignKey(l => l.QuotationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.Customer)
            .WithMany()
            .HasForeignKey(q => q.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(q => new { q.TenantId, q.QuotationNumber })
            .IsUnique()
            .HasDatabaseName("IX_Quotations_Tenant_Number");

        builder.HasIndex(q => new { q.TenantId, q.Status })
            .HasDatabaseName("IX_Quotations_Tenant_Status");
    }
}
