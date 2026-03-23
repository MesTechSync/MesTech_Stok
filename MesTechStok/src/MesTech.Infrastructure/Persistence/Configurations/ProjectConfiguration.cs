using MesTech.Domain.Entities.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Name).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Color).HasMaxLength(20);

        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Projects_Tenant_Status");

        builder.HasIndex(e => new { e.TenantId, e.OwnerUserId })
            .HasDatabaseName("IX_Projects_Tenant_Owner");
    }
}
