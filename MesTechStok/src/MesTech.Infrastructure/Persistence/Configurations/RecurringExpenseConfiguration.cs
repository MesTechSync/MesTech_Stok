using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class RecurringExpenseConfiguration : IEntityTypeConfiguration<RecurringExpense>
{
    public void Configure(EntityTypeBuilder<RecurringExpense> builder)
    {
        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Frequency).HasMaxLength(20);
        builder.Property(x => x.Category).HasMaxLength(100);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.NextDueDate);
    }
}
