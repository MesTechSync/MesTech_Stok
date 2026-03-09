using Prometheus;

namespace MesTech.Infrastructure.Monitoring;

/// <summary>
/// Finansal modul metrik tanimlari.
/// Fatura, cari hesap, iade, komisyon ve banka import islemlerini izler.
/// </summary>
public static class FinanceMetrics
{
    /// <summary>
    /// Toplam fatura sayisi.
    /// Label'lar: provider (sovos/parasut/mock), status (success/error/cancelled)
    /// </summary>
    public static readonly Counter InvoicesTotal = Metrics.CreateCounter(
        "mestech_invoices_total",
        "Total invoices processed",
        new CounterConfiguration
        {
            LabelNames = new[] { "provider", "status" }
        });

    /// <summary>
    /// Toplam fatura tutari (TL).
    /// Label'lar: provider, currency
    /// </summary>
    public static readonly Counter InvoiceAmountTotal = Metrics.CreateCounter(
        "mestech_invoice_amount_total",
        "Total invoice amount in currency units",
        new CounterConfiguration
        {
            LabelNames = new[] { "provider", "currency" }
        });

    /// <summary>
    /// Cari hesap toplam bakiye (anlik).
    /// Label'lar: account_type (customer/supplier)
    /// </summary>
    public static readonly Gauge AccountBalanceCurrent = Metrics.CreateGauge(
        "mestech_account_balance_current",
        "Current account balance",
        new GaugeConfiguration
        {
            LabelNames = new[] { "account_type" }
        });

    /// <summary>
    /// Toplam iade sayisi.
    /// Label'lar: platform, status (created/approved/rejected/resolved)
    /// </summary>
    public static readonly Counter ReturnsTotal = Metrics.CreateCounter(
        "mestech_returns_total",
        "Total return requests",
        new CounterConfiguration
        {
            LabelNames = new[] { "platform", "status" }
        });

    /// <summary>
    /// Toplam banka import sayisi.
    /// Label'lar: bank (garanti/isbank/yapikredi/akbank), status (success/error)
    /// </summary>
    public static readonly Counter BankImportsTotal = Metrics.CreateCounter(
        "mestech_bank_imports_total",
        "Total bank statement imports",
        new CounterConfiguration
        {
            LabelNames = new[] { "bank", "status" }
        });

    /// <summary>
    /// Platform komisyon tutari.
    /// Label'lar: platform, currency
    /// </summary>
    public static readonly Counter CommissionAmountTotal = Metrics.CreateCounter(
        "mestech_commission_amount_total",
        "Total platform commission amount",
        new CounterConfiguration
        {
            LabelNames = new[] { "platform", "currency" }
        });
}
