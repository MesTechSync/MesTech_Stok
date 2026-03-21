using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Calendar;

public class CalendarEventAttendee : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CalendarEventId { get; private set; }
    public Guid UserId { get; private set; }
    public AttendeeStatus Status { get; private set; }

    private CalendarEventAttendee() { }

    public static CalendarEventAttendee Create(Guid calendarEventId, Guid userId)
        => new CalendarEventAttendee
        {
            Id = Guid.NewGuid(),
            CalendarEventId = calendarEventId,
            UserId = userId,
            Status = AttendeeStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

    public void Accept() { Status = AttendeeStatus.Accepted; UpdatedAt = DateTime.UtcNow; }
    public void Decline() { Status = AttendeeStatus.Declined; UpdatedAt = DateTime.UtcNow; }
}
