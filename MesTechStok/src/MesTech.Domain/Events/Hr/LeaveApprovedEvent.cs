using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Hr;
public record LeaveApprovedEvent(Guid LeaveId, Guid TenantId, Guid EmployeeId, DateTime OccurredAt) : IDomainEvent;
