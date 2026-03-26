using MesTech.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class UserConsentConfiguration : IEntityTypeConfiguration<UserConsent>
{
    public void Configure(EntityTypeBuilder<UserConsent> builder)
    {
        builder.ToTable("UserConsents");

        builder.HasIndex(x => new { x.TenantId, x.UserId, x.ConsentType })
            .HasDatabaseName("IX_UserConsents_Tenant_User_Type");

        builder.HasIndex(x => new { x.TenantId, x.ConsentType, x.IsAccepted })
            .HasDatabaseName("IX_UserConsents_Tenant_Type_Accepted");

        builder.Property(x => x.IpAddress).HasMaxLength(45).IsRequired();
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);
    }
}
