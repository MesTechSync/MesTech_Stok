using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Finance;
public record ExpenseApprovedEvent(Guid ExpenseId, Guid TenantId, Guid ApprovedByUserId, DateTime OccurredAt) : IDomainEvent;
