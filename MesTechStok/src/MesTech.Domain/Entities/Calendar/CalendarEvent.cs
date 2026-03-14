using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Calendar;

namespace MesTech.Domain.Entities.Calendar;

public class CalendarEvent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateTime StartAt { get; private set; }
    public DateTime EndAt { get; private set; }
    public bool IsAllDay { get; private set; }
    public EventType Type { get; private set; }
    public bool IsRecurring { get; private set; }
    public string? RecurrenceRule { get; private set; }
    public string? Location { get; private set; }
    public string? Color { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public Guid? RelatedOrderId { get; private set; }
    public Guid? RelatedDealId { get; private set; }
    public Guid? RelatedWorkTaskId { get; private set; }

    private CalendarEvent() { }

    public static CalendarEvent Create(
        Guid tenantId, string title, DateTime startAt, DateTime endAt,
        EventType type = EventType.Custom, bool isAllDay = false,
        Guid? createdByUserId = null, string? description = null,
        string? location = null, string? color = null,
        Guid? relatedOrderId = null, Guid? relatedDealId = null,
        Guid? relatedWorkTaskId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (endAt <= startAt) throw new ArgumentException("EndAt must be after StartAt.");
        var ev = new CalendarEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Title = title,
            StartAt = startAt,
            EndAt = endAt,
            IsAllDay = isAllDay,
            Type = type,
            Description = description,
            Location = location,
            Color = color,
            CreatedByUserId = createdByUserId,
            RelatedOrderId = relatedOrderId,
            RelatedDealId = relatedDealId,
            RelatedWorkTaskId = relatedWorkTaskId,
            CreatedAt = DateTime.UtcNow
        };
        ev.RaiseDomainEvent(new CalendarEventCreatedEvent(ev.Id, startAt, DateTime.UtcNow));
        return ev;
    }
}
