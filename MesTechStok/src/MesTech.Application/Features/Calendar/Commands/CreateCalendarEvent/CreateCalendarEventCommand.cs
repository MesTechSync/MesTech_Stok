using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;

public record CreateCalendarEventCommand(
    Guid TenantId, string Title, DateTime StartAt, DateTime EndAt,
    EventType Type = EventType.Custom, bool IsAllDay = false,
    Guid? CreatedByUserId = null, string? Description = null,
    string? Location = null, Guid? RelatedOrderId = null,
    Guid? RelatedDealId = null, Guid? RelatedWorkTaskId = null,
    IReadOnlyList<Guid>? AttendeeUserIds = null
) : IRequest<Guid>;
