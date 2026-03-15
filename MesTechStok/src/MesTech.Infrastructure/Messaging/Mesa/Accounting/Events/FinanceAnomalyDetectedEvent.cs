namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// Muhasebe anomalisi tespit edildiginde MESA Bot'a bildirim olarak publish edilir.
/// AnomalyType: "DuplicateInvoice", "UnexpectedCommission", "AbnormalExpense"
/// Exchange: mestech.mesa.finance.anomaly.detected.v1
/// MESA Bot bu event'i alarak WhatsApp/Telegram uzerinden uyari gonderir.
/// </summary>
public record FinanceAnomalyDetectedEvent(
    string AnomalyType,
    string Description,
    decimal? ExpectedAmount,
    decimal? ActualAmount,
    string? EntityType,
    Guid? EntityId,
    Guid TenantId,
    DateTime OccurredAt)
{
    /// <summary>MUH-02: Anomali tutari (genisletilmis alan).</summary>
    public decimal Amount { get; init; }

    /// <summary>MUH-02: Ek detay bilgisi.</summary>
    public string? Details { get; init; }
};
