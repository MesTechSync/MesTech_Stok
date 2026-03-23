using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.Property(n => n.Title).HasMaxLength(300);
        builder.Property(n => n.Message).HasMaxLength(2000);
        builder.Property(n => n.ActionUrl).HasMaxLength(500);

        builder.HasIndex(n => new { n.TenantId, n.UserId, n.IsRead })
            .HasDatabaseName("IX_UserNotifications_Tenant_User_Read");

        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
