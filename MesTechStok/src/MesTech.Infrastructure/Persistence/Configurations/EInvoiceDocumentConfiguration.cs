using MesTech.Domain.Entities.EInvoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class EInvoiceDocumentConfiguration : IEntityTypeConfiguration<EInvoiceDocument>
{
    public void Configure(EntityTypeBuilder<EInvoiceDocument> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("e_invoice_documents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.GibUuid).IsRequired().HasMaxLength(36);
        builder.Property(x => x.EttnNo).IsRequired().HasMaxLength(50);
        builder.Property(x => x.SellerVkn).IsRequired().HasMaxLength(11);
        builder.Property(x => x.SellerTitle).IsRequired().HasMaxLength(255);
        builder.Property(x => x.BuyerVkn).HasMaxLength(11);
        builder.Property(x => x.BuyerTitle).IsRequired().HasMaxLength(255);
        builder.Property(x => x.BuyerEmail).HasMaxLength(255);
        builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        builder.Property(x => x.ProviderId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ProviderRef).HasMaxLength(100);
        builder.Property(x => x.PdfUrl).HasMaxLength(1000);
        builder.Property(x => x.HtmlUrl).HasMaxLength(1000);

        builder.Property(x => x.LineExtensionAmount).HasPrecision(18, 4);
        builder.Property(x => x.TaxExclusiveAmount).HasPrecision(18, 4);
        builder.Property(x => x.TaxInclusiveAmount).HasPrecision(18, 4);
        builder.Property(x => x.AllowanceTotalAmount).HasPrecision(18, 4);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 4);
        builder.Property(x => x.PayableAmount).HasPrecision(18, 4);

        builder.HasIndex(x => x.GibUuid).IsUnique();
        builder.HasIndex(x => x.EttnNo).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.IssueDate);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.EInvoiceDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.SendLogs)
            .WithOne()
            .HasForeignKey(s => s.EInvoiceDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_einvoice_documents_tenant_id");
        builder.HasIndex(x => new { x.TenantId, x.IsDeleted }).HasDatabaseName("ix_einvoice_documents_tenant_deleted");
    }
}
