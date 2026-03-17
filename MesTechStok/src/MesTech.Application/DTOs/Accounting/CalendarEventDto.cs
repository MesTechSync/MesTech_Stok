using MesTech.Domain.Accounting.Enums;
using MesTech.Domain.Enums;

namespace MesTech.Application.DTOs.Accounting;

public class CalendarEventDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public bool IsAllDay { get; set; }
    public EventType Type { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public string? Location { get; set; }
    public string? Color { get; set; }
    public DateTime? ReminderDate { get; set; }
    public bool IsCompleted { get; set; }
    public CalendarPriority Priority { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
