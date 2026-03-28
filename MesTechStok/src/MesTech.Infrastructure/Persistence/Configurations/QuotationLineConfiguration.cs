using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class QuotationLineConfiguration : IEntityTypeConfiguration<QuotationLine>
{
    public void Configure(EntityTypeBuilder<QuotationLine> builder)
    {
        builder.Property(l => l.ProductName).HasMaxLength(300);
        builder.Property(l => l.SKU).HasMaxLength(100);
        builder.Property(l => l.UnitPrice).HasPrecision(18, 2);
        builder.Property(l => l.TaxRate).HasPrecision(18, 2);
        builder.Property(l => l.Description).HasMaxLength(1000);

        // Computed properties — ignore from persistence
        builder.Ignore(l => l.LineTotal);
        builder.Ignore(l => l.TaxAmount);

        builder.HasIndex(l => l.QuotationId)
            .HasDatabaseName("IX_QuotationLines_QuotationId");

        builder.HasIndex(l => l.TenantId).HasDatabaseName("ix_quotation_lines_tenant_id");
    }
}
