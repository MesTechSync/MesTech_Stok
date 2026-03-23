using MesTech.Domain.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Subject).HasMaxLength(300);
        builder.Property(e => e.Description).HasMaxLength(2000);

        builder.HasIndex(e => new { e.TenantId, e.OccurredAt })
            .HasDatabaseName("IX_Activities_Tenant_OccurredAt");

        builder.HasIndex(e => new { e.TenantId, e.CrmContactId })
            .HasDatabaseName("IX_Activities_Tenant_Contact");

        builder.HasIndex(e => new { e.TenantId, e.DealId })
            .HasDatabaseName("IX_Activities_Tenant_Deal");
    }
}
