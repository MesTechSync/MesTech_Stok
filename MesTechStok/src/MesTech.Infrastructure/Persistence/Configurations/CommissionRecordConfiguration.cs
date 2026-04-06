using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class CommissionRecordConfiguration : IEntityTypeConfiguration<CommissionRecord>
{
    public void Configure(EntityTypeBuilder<CommissionRecord> builder)
    {
        builder.Property(x => x.OrderId).HasMaxLength(100);
        builder.Property(x => x.Platform).HasMaxLength(50);
        builder.Property(x => x.Category).HasMaxLength(200);
        builder.Property(x => x.RateSource).HasMaxLength(100);
        builder.Property(x => x.GrossAmount).HasPrecision(18, 2);
        builder.Property(x => x.CommissionRate).HasPrecision(18, 4);
        builder.Property(x => x.CommissionAmount).HasPrecision(18, 2);
        builder.Property(x => x.ServiceFee).HasPrecision(18, 2);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Platform);
        builder.HasIndex(x => x.OrderId);

        builder.Property<uint>("xmin").HasColumnType("xid").IsConcurrencyToken();
    }
}
