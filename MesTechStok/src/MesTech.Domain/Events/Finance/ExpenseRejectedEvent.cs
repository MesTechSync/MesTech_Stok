using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Finance;
public record ExpenseRejectedEvent(Guid ExpenseId, Guid TenantId, string? Reason, DateTime OccurredAt) : IDomainEvent;
