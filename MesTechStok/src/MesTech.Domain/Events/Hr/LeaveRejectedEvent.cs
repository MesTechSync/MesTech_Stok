using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Hr;
public record LeaveRejectedEvent(Guid LeaveId, Guid TenantId, Guid EmployeeId, string Reason, DateTime OccurredAt) : IDomainEvent;
