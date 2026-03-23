using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

/// <summary>
/// NotificationSetting entity EF Core Fluent API configuration.
/// </summary>
public class NotificationSettingConfiguration : IEntityTypeConfiguration<NotificationSetting>
{
    public void Configure(EntityTypeBuilder<NotificationSetting> builder)
    {
        // Indexes — unique per user+channel per tenant
        builder.HasIndex(ns => new { ns.TenantId, ns.UserId, ns.Channel })
            .IsUnique()
            .HasDatabaseName("IX_NotificationSettings_Tenant_User_Channel");

        // String constraints
        builder.Property(ns => ns.ChannelAddress).HasMaxLength(256);
        builder.Property(ns => ns.PreferredLanguage).HasMaxLength(10);

        // Relationships
        builder.HasOne(ns => ns.User)
            .WithMany()
            .HasForeignKey(ns => ns.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
