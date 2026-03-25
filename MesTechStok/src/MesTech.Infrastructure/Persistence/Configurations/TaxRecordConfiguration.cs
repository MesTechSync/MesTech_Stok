using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class TaxRecordConfiguration : IEntityTypeConfiguration<TaxRecord>
{
    public void Configure(EntityTypeBuilder<TaxRecord> builder)
    {
        builder.Property(x => x.Period).HasMaxLength(20);
        builder.Property(x => x.TaxType).HasMaxLength(50);
        builder.Property(x => x.TaxableAmount).HasPrecision(18, 2);
        builder.Property(x => x.TaxRate).HasPrecision(18, 4);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 2);
        builder.Property(x => x.PenaltyAmount).HasPrecision(18, 2);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.Period, x.TaxType })
            .HasDatabaseName("IX_TaxRecords_Tenant_Period_Type");
    }
}
