using MesTech.Domain.Entities.Calendar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MesTech.Infrastructure.Persistence.Configurations;

public class CalendarEventAttendeeConfiguration : IEntityTypeConfiguration<CalendarEventAttendee>
{
    public void Configure(EntityTypeBuilder<CalendarEventAttendee> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasIndex(e => new { e.TenantId, e.CalendarEventId, e.UserId })
            .IsUnique()
            .HasDatabaseName("IX_CalendarEventAttendees_Tenant_Event_User");
    }
}
