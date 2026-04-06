using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class AccountingDocumentConfiguration : IEntityTypeConfiguration<AccountingDocument>
{
    public void Configure(EntityTypeBuilder<AccountingDocument> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.DocumentType })
            .HasDatabaseName("IX_AccountingDocuments_Tenant_Type");

        builder.HasIndex(e => new { e.TenantId, e.CounterpartyId })
            .HasFilter("\"CounterpartyId\" IS NOT NULL")
            .HasDatabaseName("IX_AccountingDocuments_Tenant_Counterparty");

        builder.Property(e => e.FileName).HasMaxLength(500).IsRequired();
        builder.Property(e => e.MimeType).HasMaxLength(100).IsRequired();
        builder.Property(e => e.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(e => e.ExtractedData).HasMaxLength(4000);
        builder.Property(e => e.Amount).HasPrecision(18, 2);

        builder.Property<uint>("xmin").HasColumnType("xid").IsConcurrencyToken();
    }
}
