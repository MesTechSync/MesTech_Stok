using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class SettlementLineConfiguration : IEntityTypeConfiguration<SettlementLine>
{
    public void Configure(EntityTypeBuilder<SettlementLine> builder)
    {
        builder.Property(x => x.OrderId).HasMaxLength(100);
        builder.Property(x => x.GrossAmount).HasPrecision(18, 2);
        builder.Property(x => x.CommissionAmount).HasPrecision(18, 2);
        builder.Property(x => x.ServiceFee).HasPrecision(18, 2);
        builder.Property(x => x.CargoDeduction).HasPrecision(18, 2);
        builder.Property(x => x.RefundDeduction).HasPrecision(18, 2);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);

        builder.HasIndex(x => x.SettlementBatchId);
        builder.HasIndex(x => x.OrderId);
    }
}
