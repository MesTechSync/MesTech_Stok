namespace MesTech.Infrastructure.Messaging.Mesa.Accounting.Events;

/// <summary>
/// Gunluk finansal brifing publish edilir.
/// MESA Bot Gateway WhatsApp/Telegram uzerinden iletir.
/// Exchange: mestech.mesa.finance.report.daily.v1
/// </summary>
public record FinanceReportDailyEvent(
    DateTime Date,
    int OrderCount,
    decimal TotalRevenue,
    decimal TotalCommission,
    decimal TotalCargo,
    decimal NetProfit,
    List<string> StockAlerts,
    List<string> Recommendations,
    Guid TenantId,
    DateTime OccurredAt);
