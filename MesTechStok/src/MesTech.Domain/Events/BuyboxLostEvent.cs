using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

public record BuyboxLostEvent(
    Guid ProductId,
    string SKU,
    decimal CurrentPrice,
    decimal CompetitorPrice,
    string CompetitorName,
    DateTime OccurredAt
) : IDomainEvent;
