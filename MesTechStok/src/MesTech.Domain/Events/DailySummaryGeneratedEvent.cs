using MesTech.Domain.Common;

namespace MesTech.Domain.Events;

/// <summary>Gunluk ozet raporu uretildi — MESA OS KPI dashboard icin.</summary>
public record DailySummaryGeneratedEvent(
    DateTime Date,
    int OrderCount,
    decimal Revenue,
    int StockAlerts,
    int InvoiceCount,
    DateTime OccurredAt) : IDomainEvent;
