using MesTech.Domain.Common;
using MesTech.Domain.Entities.Finance;

namespace MesTech.Domain.Events;

public record CashTransactionRecordedEvent(
    Guid TenantId,
    Guid CashRegisterId,
    Guid TransactionId,
    CashTransactionType Type,
    decimal Amount,
    decimal NewBalance,
    DateTime OccurredAt
) : IDomainEvent;
