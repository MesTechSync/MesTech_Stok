using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Tasks;
public record TaskOverdueEvent(Guid TaskId, DateTime DueDate, DateTime OccurredAt) : IDomainEvent;
