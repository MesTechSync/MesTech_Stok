using Prometheus;

namespace MesTech.Infrastructure.Monitoring;

/// <summary>
/// Muhasebe modulu Prometheus metrikleri.
/// prometheus-net kullanarak /metrics endpoint ile otomatik expose edilir.
///
/// Dalga 12 Wave 3 — DEV 4: yevmiye, mutabakat, hesap kesimi, fatura,
/// kargo ve bildirim islemlerini izler.
/// </summary>
public static class AccountingMetrics
{
    // ══════════════════════════════════════════════════════════════════════════
    // Counters — monotonically increasing totals
    // ══════════════════════════════════════════════════════════════════════════

    public static readonly Counter JournalEntriesCreated = Metrics.CreateCounter(
        "mestech_journal_entries_created_total",
        "Total number of journal entries created");

    public static readonly Counter ReconciliationMatchesFound = Metrics.CreateCounter(
        "mestech_reconciliation_matches_total",
        "Total number of reconciliation matches found");

    public static readonly Counter SettlementsParsed = Metrics.CreateCounter(
        "mestech_settlements_parsed_total",
        "Total number of settlements parsed");

    public static readonly Counter InvoicesIssued = Metrics.CreateCounter(
        "mestech_invoices_issued_total",
        "Total number of invoices issued");

    public static readonly Counter ShipmentsCreated = Metrics.CreateCounter(
        "mestech_shipments_created_total",
        "Total number of shipments created");

    public static readonly Counter NotificationsSent = Metrics.CreateCounter(
        "mestech_notifications_sent_total",
        "Total number of notifications sent");

    // ══════════════════════════════════════════════════════════════════════════
    // Histograms — duration tracking (seconds)
    // ══════════════════════════════════════════════════════════════════════════

    public static readonly Histogram ReconciliationDuration = Metrics.CreateHistogram(
        "mestech_reconciliation_duration_seconds",
        "Duration of reconciliation operations in seconds");

    public static readonly Histogram SettlementParseDuration = Metrics.CreateHistogram(
        "mestech_settlement_parse_duration_seconds",
        "Duration of settlement parse operations in seconds");

    public static readonly Histogram AutoShipmentDuration = Metrics.CreateHistogram(
        "mestech_auto_shipment_duration_seconds",
        "Duration of auto-shipment assignment in seconds");

    // ══════════════════════════════════════════════════════════════════════════
    // Gauges — current state
    // ══════════════════════════════════════════════════════════════════════════

    public static readonly Gauge PendingReconciliations = Metrics.CreateGauge(
        "mestech_pending_reconciliations",
        "Number of pending reconciliation items");

    public static readonly Gauge UnreconciledAmount = Metrics.CreateGauge(
        "mestech_unreconciled_amount_total",
        "Total unreconciled amount in TRY");
}
