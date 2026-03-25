using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class ProfitLossEntryConfiguration : IEntityTypeConfiguration<ProfitLossEntry>
{
    public void Configure(EntityTypeBuilder<ProfitLossEntry> builder)
    {
        builder.Property(x => x.Period).HasMaxLength(20);
        builder.Property(x => x.RevenueAmount).HasPrecision(18, 2);
        builder.Property(x => x.ExpenseAmount).HasPrecision(18, 2);
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.Ignore(x => x.NetProfit);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.Period })
            .HasDatabaseName("IX_ProfitLossEntries_Tenant_Period");
    }
}
