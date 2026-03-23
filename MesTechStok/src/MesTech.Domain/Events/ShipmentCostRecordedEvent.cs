using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>
/// Kargo gönderimi oluşturulduğunda fırlatılır.
/// Zincir 7 trigger: DEV 1'in ShipmentCostJournalHandler'ı bu event'i dinler
/// ve kargo maliyetini gider yevmiye kaydı olarak oluşturur.
/// </summary>
public record ShipmentCostRecordedEvent(
    Guid OrderId,
    Guid TenantId,
    string TrackingNumber,
    string CargoProvider,
    decimal ShippingCost,
    DateTime OccurredAt) : IDomainEvent;
