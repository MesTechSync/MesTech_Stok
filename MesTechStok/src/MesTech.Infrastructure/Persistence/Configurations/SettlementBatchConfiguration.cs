using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class SettlementBatchConfiguration : IEntityTypeConfiguration<SettlementBatch>
{
    public void Configure(EntityTypeBuilder<SettlementBatch> builder)
    {
        builder.Property(x => x.Platform).HasMaxLength(50);
        builder.Property(x => x.TotalGross).HasPrecision(18, 2);
        builder.Property(x => x.TotalCommission).HasPrecision(18, 2);
        builder.Property(x => x.TotalNet).HasPrecision(18, 2);

        builder.HasMany(x => x.Lines)
            .WithOne(x => x.SettlementBatch)
            .HasForeignKey(x => x.SettlementBatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Platform);
        builder.HasIndex(x => new { x.PeriodStart, x.PeriodEnd });
    }
}
