using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class SocialFeedConfigurationConfiguration : IEntityTypeConfiguration<SocialFeedConfiguration>
{
    public void Configure(EntityTypeBuilder<SocialFeedConfiguration> builder)
    {
        builder.Property(f => f.FeedUrl).HasMaxLength(1000);
        builder.Property(f => f.LastError).HasMaxLength(2000);
        builder.Property(f => f.CategoryFilter).HasMaxLength(1000);

        // Computed properties — ignore from persistence
        builder.Ignore(f => f.NextScheduledGeneration);
        builder.Ignore(f => f.NeedsRefresh);

        builder.HasIndex(f => new { f.TenantId, f.Platform })
            .IsUnique()
            .HasDatabaseName("IX_SocialFeedConfigurations_Tenant_Platform");
    }
}
