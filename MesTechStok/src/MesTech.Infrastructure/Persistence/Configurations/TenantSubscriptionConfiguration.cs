using MesTech.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class TenantSubscriptionConfiguration : IEntityTypeConfiguration<TenantSubscription>
{
    public void Configure(EntityTypeBuilder<TenantSubscription> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tenant_subscriptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CancellationReason).HasMaxLength(500);

        builder.HasOne(x => x.Plan)
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.NextBillingDate);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
