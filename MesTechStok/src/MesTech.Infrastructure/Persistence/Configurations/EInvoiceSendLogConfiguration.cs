using MesTech.Domain.Entities.EInvoice;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class EInvoiceSendLogConfiguration : IEntityTypeConfiguration<EInvoiceSendLog>
{
    public void Configure(EntityTypeBuilder<EInvoiceSendLog> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("e_invoice_send_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProviderId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Action).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ProviderRef).HasMaxLength(100);
        builder.HasIndex(x => x.EInvoiceDocumentId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
