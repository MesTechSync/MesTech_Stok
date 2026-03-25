using MesTech.Domain.Entities.Calendar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Title).HasMaxLength(300);
        builder.Property(e => e.Description).HasMaxLength(2000);
        builder.Property(e => e.Location).HasMaxLength(300);
        builder.Property(e => e.Color).HasMaxLength(20);
        builder.Property(e => e.RecurrenceRule).HasMaxLength(500);

        builder.HasMany(e => e.Attendees)
            .WithOne()
            .HasForeignKey(a => a.CalendarEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.TenantId, e.StartAt })
            .HasDatabaseName("IX_CalendarEvents_Tenant_Start");

        builder.HasIndex(e => new { e.TenantId, e.CreatedByUserId })
            .HasDatabaseName("IX_CalendarEvents_Tenant_Creator");
    }
}
