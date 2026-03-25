using MesTech.Domain.Common;

namespace MesTech.Domain.Accounting.Events;

/// <summary>
/// Komisyon kaydı oluşturulduğunda fırlatılır.
/// Zincir 6 trigger: CommissionChargedGLHandler bu event'i dinler
/// ve komisyon giderini GL yevmiye kaydı olarak oluşturur.
/// </summary>
public record CommissionChargedEvent(
    Guid CommissionRecordId,
    Guid TenantId,
    string Platform,
    string? OrderId,
    decimal GrossAmount,
    decimal CommissionRate,
    decimal CommissionAmount,
    DateTime OccurredAt
) : IDomainEvent;
