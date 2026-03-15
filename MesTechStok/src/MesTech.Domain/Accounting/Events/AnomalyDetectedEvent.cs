namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Muhasebe anomalisi tespit edildiginde tetiklenir (ornegin beklenmeyen tutar farki).
/// </summary>
public record AnomalyDetectedEvent : AccountingDomainEvent
{
    public string AnomalyType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal? ExpectedAmount { get; init; }
    public decimal? ActualAmount { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
}
