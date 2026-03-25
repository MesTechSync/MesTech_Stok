using MesTech.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("subscription_plans");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        builder.Property(x => x.MonthlyPrice).HasPrecision(18, 2);
        builder.Property(x => x.AnnualPrice).HasPrecision(18, 2);
        builder.Property(x => x.FeaturesJson).HasColumnType("jsonb");

        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.IsActive);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
