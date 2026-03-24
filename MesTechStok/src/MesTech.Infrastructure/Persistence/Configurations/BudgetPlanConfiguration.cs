using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class BudgetPlanConfiguration : IEntityTypeConfiguration<BudgetPlan>
{
    public void Configure(EntityTypeBuilder<BudgetPlan> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.Period })
            .HasDatabaseName("IX_BudgetPlans_Tenant_Period");

        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Period).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(50);
        builder.Property(e => e.PlannedRevenue).HasPrecision(18, 2);
        builder.Property(e => e.PlannedExpense).HasPrecision(18, 2);
        builder.Property(e => e.ActualRevenue).HasPrecision(18, 2);
        builder.Property(e => e.ActualExpense).HasPrecision(18, 2);
        builder.Property(e => e.Variance).HasPrecision(18, 2);
    }
}
