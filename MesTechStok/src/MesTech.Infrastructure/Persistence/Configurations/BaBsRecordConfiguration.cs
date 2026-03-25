using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class BaBsRecordConfiguration : IEntityTypeConfiguration<BaBsRecord>
{
    public void Configure(EntityTypeBuilder<BaBsRecord> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.Year, e.Month, e.Type })
            .HasDatabaseName("IX_BaBsRecords_Tenant_Period_Type");

        builder.HasIndex(e => new { e.TenantId, e.CounterpartyVkn })
            .HasDatabaseName("IX_BaBsRecords_Tenant_Vkn");

        builder.Property(e => e.CounterpartyVkn).HasMaxLength(11).IsRequired();
        builder.Property(e => e.CounterpartyName).HasMaxLength(300).IsRequired();
        builder.Property(e => e.TotalAmount).HasPrecision(18, 2);
    }
}
