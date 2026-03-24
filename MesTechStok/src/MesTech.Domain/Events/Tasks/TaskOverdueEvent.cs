using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Tasks;
public record TaskOverdueEvent(Guid TaskId, Guid TenantId, DateTime DueDate, DateTime OccurredAt) : IDomainEvent;
