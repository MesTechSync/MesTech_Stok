using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Finance;
public record ExpenseSubmittedEvent(Guid ExpenseId, Guid TenantId, DateTime OccurredAt) : IDomainEvent;
