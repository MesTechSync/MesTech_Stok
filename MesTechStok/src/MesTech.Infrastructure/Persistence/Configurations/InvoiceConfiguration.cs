using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber })
            .IsUnique()
            .HasDatabaseName("IX_Invoices_Tenant_Number");

        builder.HasIndex(i => new { i.TenantId, i.InvoiceDate })
            .HasDatabaseName("IX_Invoices_Tenant_Date");

        builder.HasIndex(i => i.GibInvoiceId)
            .HasFilter("\"GibInvoiceId\" IS NOT NULL")
            .HasDatabaseName("IX_Invoices_GibId");

        builder.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(i => i.CustomerName).HasMaxLength(200);
        builder.Property(i => i.CustomerTaxNumber).HasMaxLength(20);
        builder.Property(i => i.CustomerTaxOffice).HasMaxLength(100);
        builder.Property(i => i.CustomerAddress).HasMaxLength(500);
        builder.Property(i => i.CustomerEmail).HasMaxLength(200);
        builder.Property(i => i.GibInvoiceId).HasMaxLength(100);
        builder.Property(i => i.GibEnvelopeId).HasMaxLength(100);
        builder.Property(i => i.PdfUrl).HasMaxLength(500);

        builder.Property(i => i.SubTotal).HasPrecision(18, 2);
        builder.Property(i => i.TaxTotal).HasPrecision(18, 2);
        builder.Property(i => i.GrandTotal).HasPrecision(18, 2);

        builder.HasMany(i => i.Lines)
            .WithOne(l => l.Invoice)
            .HasForeignKey(l => l.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
