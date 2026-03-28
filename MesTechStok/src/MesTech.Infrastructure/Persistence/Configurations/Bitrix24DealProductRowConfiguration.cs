using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class Bitrix24DealProductRowConfiguration : IEntityTypeConfiguration<Bitrix24DealProductRow>
{
    public void Configure(EntityTypeBuilder<Bitrix24DealProductRow> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.ProductName).HasMaxLength(500);
        builder.Property(r => r.UnitPrice).HasPrecision(18, 2);
        builder.Property(r => r.Discount).HasPrecision(18, 2);
        builder.Property(r => r.TaxRate).HasPrecision(5, 2);

        // Ignore computed properties — calculated at runtime, not stored
        builder.Ignore(r => r.LineTotal);
        builder.Ignore(r => r.TaxAmount);

        builder.HasIndex(r => r.Bitrix24DealId);

        builder.HasIndex(r => r.TenantId).HasDatabaseName("ix_bitrix24_deal_product_rows_tenant_id");
    }
}
