using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Calendar;
public record CalendarEventCreatedEvent(Guid EventId, Guid TenantId, DateTime StartAt, DateTime OccurredAt) : IDomainEvent;
