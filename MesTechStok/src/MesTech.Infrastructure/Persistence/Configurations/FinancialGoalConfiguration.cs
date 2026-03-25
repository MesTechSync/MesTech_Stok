using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class FinancialGoalConfiguration : IEntityTypeConfiguration<FinancialGoal>
{
    public void Configure(EntityTypeBuilder<FinancialGoal> builder)
    {
        builder.HasIndex(e => new { e.TenantId, e.IsAchieved })
            .HasDatabaseName("IX_FinancialGoals_Tenant_Achieved");

        builder.Property(e => e.Title).HasMaxLength(300).IsRequired();
        builder.Property(e => e.TargetAmount).HasPrecision(18, 2);
        builder.Property(e => e.CurrentAmount).HasPrecision(18, 2);
    }
}
