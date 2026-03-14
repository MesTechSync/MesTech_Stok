using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Tasks;
public record TaskCompletedEvent(Guid TaskId, Guid CompletedByUserId, DateTime OccurredAt) : IDomainEvent;
