using MesTech.Domain.Entities.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class FinanceExpenseConfiguration : IEntityTypeConfiguration<FinanceExpense>
{
    public void Configure(EntityTypeBuilder<FinanceExpense> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Title).HasMaxLength(300);
        builder.Property(e => e.Currency).HasMaxLength(10);
        builder.Property(e => e.Notes).HasMaxLength(2000);
        builder.Property(e => e.Amount).HasPrecision(18, 4);

        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_FinanceExpenses_Tenant_Status");

        builder.HasIndex(e => new { e.TenantId, e.ExpenseDate })
            .HasDatabaseName("IX_FinanceExpenses_Tenant_Date");
    }
}
