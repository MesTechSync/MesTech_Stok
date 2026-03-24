using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Tasks;
public record TaskCompletedEvent(Guid TaskId, Guid TenantId, Guid CompletedByUserId, DateTime OccurredAt) : IDomainEvent;
