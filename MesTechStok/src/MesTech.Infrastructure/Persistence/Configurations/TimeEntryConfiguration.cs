using MesTech.Domain.Entities.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class TimeEntryConfiguration : IEntityTypeConfiguration<TimeEntry>
{
    public void Configure(EntityTypeBuilder<TimeEntry> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Description).HasMaxLength(500);
        builder.Property(e => e.HourlyRate).HasPrecision(18, 4);

        builder.HasIndex(e => new { e.TenantId, e.WorkTaskId })
            .HasDatabaseName("IX_TimeEntries_Tenant_Task");

        builder.HasIndex(e => new { e.TenantId, e.UserId, e.StartedAt })
            .HasDatabaseName("IX_TimeEntries_Tenant_User_Start");
    }
}
