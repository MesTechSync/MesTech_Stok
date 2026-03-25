using MesTech.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class DunningLogConfiguration : IEntityTypeConfiguration<DunningLog>
{
    public void Configure(EntityTypeBuilder<DunningLog> builder)
    {
        builder.Property(x => x.ErrorMessage).HasMaxLength(500);

        builder.HasOne(x => x.Subscription)
            .WithMany()
            .HasForeignKey(x => x.TenantSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => x.TenantSubscriptionId);
    }
}
