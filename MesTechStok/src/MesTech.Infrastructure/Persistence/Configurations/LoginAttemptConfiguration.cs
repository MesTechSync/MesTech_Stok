using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class LoginAttemptConfiguration : IEntityTypeConfiguration<LoginAttempt>
{
    public void Configure(EntityTypeBuilder<LoginAttempt> builder)
    {
        // Indexes
        builder.HasIndex(l => new { l.TenantId, l.Username, l.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Tenant_User_Time");

        builder.HasIndex(l => new { l.IpAddress, l.AttemptedAt })
            .HasDatabaseName("IX_LoginAttempts_Ip_Time");

        // String constraints
        builder.Property(l => l.Username).HasMaxLength(100).IsRequired();
        builder.Property(l => l.IpAddress).HasMaxLength(50).IsRequired();
        builder.Property(l => l.UserAgent).HasMaxLength(500);
    }
}
