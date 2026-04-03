using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class TaxWithholdingConfiguration : IEntityTypeConfiguration<TaxWithholding>
{
    public void Configure(EntityTypeBuilder<TaxWithholding> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.InvoiceId })
            .HasFilter("\"InvoiceId\" IS NOT NULL")
            .HasDatabaseName("IX_TaxWithholdings_Tenant_Invoice");

        builder.Property(e => e.TaxType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.TaxExclusiveAmount).HasPrecision(18, 2);
        builder.Property(e => e.Rate).HasPrecision(5, 4);
        builder.Property(e => e.WithholdingAmount).HasPrecision(18, 2);

        builder.Property<uint>("xmin").HasColumnType("xid").IsConcurrencyToken();
    }
}
