using MesTech.Domain.Entities.Hr;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class LeaveConfiguration : IEntityTypeConfiguration<Leave>
{
    public void Configure(EntityTypeBuilder<Leave> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Reason).HasMaxLength(1000);
        builder.Property(e => e.RejectionReason).HasMaxLength(500);

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.EmployeeId })
            .HasDatabaseName("IX_Leaves_Tenant_Employee");

        builder.HasIndex(e => new { e.TenantId, e.Status })
            .HasDatabaseName("IX_Leaves_Tenant_Status");
    }
}
