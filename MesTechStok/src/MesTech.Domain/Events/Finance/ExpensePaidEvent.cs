using MesTech.Domain.Common;
namespace MesTech.Domain.Events.Finance;
public record ExpensePaidEvent(Guid ExpenseId, Guid BankAccountId, DateTime OccurredAt) : IDomainEvent;
