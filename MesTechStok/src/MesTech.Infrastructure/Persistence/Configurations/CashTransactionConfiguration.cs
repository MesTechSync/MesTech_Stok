using MesTech.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class CashTransactionConfiguration : IEntityTypeConfiguration<CashTransaction>
{
    public void Configure(EntityTypeBuilder<CashTransaction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("cash_transactions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Category).HasMaxLength(100);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.CashRegisterId);
        builder.HasIndex(x => x.TransactionDate);
        builder.HasIndex(x => x.Type);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
