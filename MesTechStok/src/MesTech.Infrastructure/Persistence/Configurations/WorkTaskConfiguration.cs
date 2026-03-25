using MesTech.Domain.Entities.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Title).HasMaxLength(300);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Tags).HasMaxLength(500);

        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_WorkTasks_Tenant_Status");

        builder.HasIndex(e => new { e.TenantId, e.ProjectId })
            .HasDatabaseName("IX_WorkTasks_Tenant_Project");

        builder.HasIndex(e => new { e.TenantId, e.AssignedToUserId })
            .HasDatabaseName("IX_WorkTasks_Tenant_Assigned");

        builder.HasIndex(e => new { e.TenantId, e.MilestoneId })
            .HasDatabaseName("IX_WorkTasks_Tenant_Milestone");
    }
}
