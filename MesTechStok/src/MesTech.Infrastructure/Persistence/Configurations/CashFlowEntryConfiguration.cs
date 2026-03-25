using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class CashFlowEntryConfiguration : IEntityTypeConfiguration<CashFlowEntry>
{
    public void Configure(EntityTypeBuilder<CashFlowEntry> builder)
    {
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasOne(x => x.Counterparty)
            .WithMany()
            .HasForeignKey(x => x.CounterpartyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.EntryDate);
        builder.HasIndex(x => x.Direction);
    }
}
