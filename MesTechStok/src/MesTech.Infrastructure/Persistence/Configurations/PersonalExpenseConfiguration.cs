using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class PersonalExpenseConfiguration : IEntityTypeConfiguration<PersonalExpense>
{
    public void Configure(EntityTypeBuilder<PersonalExpense> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.ApprovedBy).HasMaxLength(100);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.ExpenseDate);
    }
}
