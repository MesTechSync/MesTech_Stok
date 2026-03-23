using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.Property(l => l.ProductName).HasMaxLength(300);
        builder.Property(l => l.SKU).HasMaxLength(100);
        builder.Property(l => l.Barcode).HasMaxLength(200);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
        builder.Property(l => l.TaxRate).HasPrecision(18, 2);
        builder.Property(l => l.TaxAmount).HasPrecision(18, 2);
        builder.Property(l => l.LineTotal).HasPrecision(18, 2);
        builder.Property(l => l.DiscountAmount).HasPrecision(18, 2);

        builder.HasIndex(l => new { l.TenantId, l.InvoiceId })
            .HasDatabaseName("IX_InvoiceLines_Tenant_Invoice");
    }
}
