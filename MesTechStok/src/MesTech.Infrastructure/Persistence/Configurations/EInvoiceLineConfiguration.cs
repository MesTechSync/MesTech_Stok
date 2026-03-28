using MesTech.Domain.Entities.EInvoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class EInvoiceLineConfiguration : IEntityTypeConfiguration<EInvoiceLine>
{
    public void Configure(EntityTypeBuilder<EInvoiceLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("e_invoice_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(512);
        builder.Property(x => x.UnitCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.ProductCode).HasMaxLength(50);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.TenantId).HasDatabaseName("ix_einvoice_lines_tenant_id");
    }
}
