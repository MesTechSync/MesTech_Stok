using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class FixedExpenseConfiguration : IEntityTypeConfiguration<FixedExpense>
{
    public void Configure(EntityTypeBuilder<FixedExpense> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.MonthlyAmount).HasPrecision(18, 2);
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.SupplierName).HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.IsActive);
    }
}
