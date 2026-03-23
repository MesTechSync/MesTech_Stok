using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.Property(s => s.IpAddress).HasMaxLength(50);
        builder.Property(s => s.UserAgent).HasMaxLength(500);
        builder.Property(s => s.DeviceInfo).HasMaxLength(500);

        // Computed properties — ignore from persistence
        builder.Ignore(s => s.IsExpired);
        builder.Ignore(s => s.IsValid);

        builder.HasIndex(s => new { s.TenantId, s.UserId, s.IsActive })
            .HasDatabaseName("IX_Sessions_Tenant_User_Active");

        builder.HasIndex(s => s.ExpiresAt)
            .HasDatabaseName("IX_Sessions_ExpiresAt");
    }
}
