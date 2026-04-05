using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Banka hesap bakiyesi değiştiğinde tetiklenir.
/// Muhasebe GL sync + nakit akış raporu + bildirim zinciri.
/// </summary>
public record BankBalanceChangedEvent(
    Guid BankAccountId,
    Guid TenantId,
    string AccountName,
    decimal PreviousBalance,
    decimal NewBalance,
    decimal Delta,
    DateTime OccurredAt
) : IDomainEvent;
