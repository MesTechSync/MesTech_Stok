using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Finance;
public record ExpensePaidEvent(Guid ExpenseId, Guid TenantId, Guid BankAccountId, DateTime OccurredAt) : IDomainEvent;
