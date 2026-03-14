using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Hr;
public record LeaveApprovedEvent(Guid LeaveId, Guid EmployeeId, DateTime OccurredAt) : IDomainEvent;
