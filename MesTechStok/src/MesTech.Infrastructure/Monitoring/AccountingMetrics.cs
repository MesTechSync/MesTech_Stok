using System.Diagnostics.Metrics;

namespace MesTech.Infrastructure.Monitoring;

/// <summary>
/// Muhasebe modulu Prometheus metrikleri.
/// System.Diagnostics.Metrics (.NET 8/9 built-in) kullanarak
/// OpenTelemetry Prometheus exporter ile otomatik expose edilir.
///
/// Dalga 12 Wave 3 — DEV 4: yevmiye, mutabakat, hesap kesimi, fatura,
/// kargo ve bildirim islemlerini izler.
/// </summary>
public static class AccountingMetrics
{
    private static readonly Meter Meter = new("MesTech.Accounting", "1.0.0");

    // ══════════════════════════════════════════════════════════════════════════
    // Counters — monotonically increasing totals
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Toplam olusturulan yevmiye kaydi sayisi.
    /// </summary>
    public static readonly Counter<long> JournalEntriesCreated =
        Meter.CreateCounter<long>(
            "mestech_journal_entries_created_total",
            unit: "{entries}",
            description: "Total number of journal entries created");

    /// <summary>
    /// Toplam mutabakat eslesmesi sayisi.
    /// </summary>
    public static readonly Counter<long> ReconciliationMatchesFound =
        Meter.CreateCounter<long>(
            "mestech_reconciliation_matches_total",
            unit: "{matches}",
            description: "Total number of reconciliation matches found");

    /// <summary>
    /// Toplam parse edilen hesap kesimi sayisi.
    /// </summary>
    public static readonly Counter<long> SettlementsParsed =
        Meter.CreateCounter<long>(
            "mestech_settlements_parsed_total",
            unit: "{settlements}",
            description: "Total number of settlements parsed");

    /// <summary>
    /// Toplam kesilen fatura sayisi.
    /// </summary>
    public static readonly Counter<long> InvoicesIssued =
        Meter.CreateCounter<long>(
            "mestech_invoices_issued_total",
            unit: "{invoices}",
            description: "Total number of invoices issued");

    /// <summary>
    /// Toplam olusturulan kargo gonderisi sayisi.
    /// </summary>
    public static readonly Counter<long> ShipmentsCreated =
        Meter.CreateCounter<long>(
            "mestech_shipments_created_total",
            unit: "{shipments}",
            description: "Total number of shipments created");

    /// <summary>
    /// Toplam gonderilen bildirim sayisi.
    /// </summary>
    public static readonly Counter<long> NotificationsSent =
        Meter.CreateCounter<long>(
            "mestech_notifications_sent_total",
            unit: "{notifications}",
            description: "Total number of notifications sent");

    // ══════════════════════════════════════════════════════════════════════════
    // Histograms — duration tracking (seconds)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Mutabakat islemi suresi (saniye).
    /// Banka hareketi - hesap kesimi eslestirme suresi.
    /// </summary>
    public static readonly Histogram<double> ReconciliationDuration =
        Meter.CreateHistogram<double>(
            "mestech_reconciliation_duration_seconds",
            unit: "s",
            description: "Duration of reconciliation operations in seconds");

    /// <summary>
    /// Hesap kesimi parse suresi (saniye).
    /// Platform hesap kesimi dosyasi islenme suresi.
    /// </summary>
    public static readonly Histogram<double> SettlementParseDuration =
        Meter.CreateHistogram<double>(
            "mestech_settlement_parse_duration_seconds",
            unit: "s",
            description: "Duration of settlement parse operations in seconds");

    /// <summary>
    /// Otomatik kargo atama suresi (saniye).
    /// Siparis onayindan kargo etiketi olusturulmasina kadar gecen sure.
    /// </summary>
    public static readonly Histogram<double> AutoShipmentDuration =
        Meter.CreateHistogram<double>(
            "mestech_auto_shipment_duration_seconds",
            unit: "s",
            description: "Duration of auto-shipment assignment in seconds");

    // ══════════════════════════════════════════════════════════════════════════
    // Gauges — current state (using ObservableGauge for callback-based values)
    // ══════════════════════════════════════════════════════════════════════════

    // Note: ObservableGauge requires a callback to supply the current value.
    // These are registered per-instance at application startup via RegisterGauges().
    // Example usage in Startup/DI:
    //   AccountingMetrics.RegisterGauges(
    //       pendingReconciliationCount: () => repo.GetPendingCount(),
    //       unreconciledAmount: () => repo.GetUnreconciledTotalAsync().Result);

    private static ObservableGauge<long>? _pendingReconciliationGauge;
    private static ObservableGauge<double>? _unreconciledAmountGauge;

    /// <summary>
    /// Gauge callback'lerini DI container'dan alinan servislerle kaydeder.
    /// Uygulama baslatildiginda bir kez cagirilir.
    /// </summary>
    /// <param name="pendingReconciliationCount">Bekleyen mutabakat sayisi callback'i.</param>
    /// <param name="unreconciledAmount">Eslesmemis toplam tutar callback'i.</param>
    public static void RegisterGauges(
        Func<long> pendingReconciliationCount,
        Func<double> unreconciledAmount)
    {
        _pendingReconciliationGauge ??= Meter.CreateObservableGauge(
            "mestech_pending_reconciliations",
            pendingReconciliationCount,
            unit: "{reconciliations}",
            description: "Number of pending reconciliation items");

        _unreconciledAmountGauge ??= Meter.CreateObservableGauge(
            "mestech_unreconciled_amount_total",
            unreconciledAmount,
            unit: "TRY",
            description: "Total unreconciled amount in TRY");
    }
}
