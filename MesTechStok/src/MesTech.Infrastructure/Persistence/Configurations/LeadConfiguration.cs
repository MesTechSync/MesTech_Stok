using MesTech.Domain.Entities.Crm;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.FullName).HasMaxLength(200);
        builder.Property(e => e.Email).HasMaxLength(254);
        builder.Property(e => e.Phone).HasMaxLength(20);
        builder.Property(e => e.Company).HasMaxLength(200);
        builder.Property(e => e.Notes).HasMaxLength(2000);

        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Leads_Tenant_Status");

        builder.HasIndex(e => new { e.TenantId, e.AssignedToUserId })
            .HasDatabaseName("IX_Leads_Tenant_Assigned");
    }
}
