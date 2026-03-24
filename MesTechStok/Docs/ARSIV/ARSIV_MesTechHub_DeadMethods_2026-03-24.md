# ARŞİV: MesTechHub Dead Code Methods
# Tarih: 2026-03-24
# Neden: DEV 6 SignalR audit — 3 method hiçbir handler'dan çağrılmıyor (dead code)
# Kaynak: src/MesTech.WebApi/Hubs/MesTechHub.cs
# Karar: V5 P2 Faz C audit sonucu kaldırıldı. İhtiyaç halinde buradan geri alınabilir.

## 1. NotifyReportReady (line 88-100)

```csharp
/// <summary>
/// Rapor hazır bildirimi — tenant grubuna broadcast.
/// Client event: "ReportReady" → { reportType, reportUrl, generatedAt }
/// </summary>
public static async Task NotifyReportReady(
    IHubContext<MesTechHub> hubContext,
    string tenantId,
    string reportType,
    string reportUrl)
{
    await hubContext.Clients.Group($"tenant-{tenantId}").SendAsync("ReportReady", new
    {
        reportType,
        reportUrl,
        generatedAt = DateTime.UtcNow
    });
}
```

## 2. NotifyReconciliationDone (line 106-120)

```csharp
/// <summary>
/// ERP mutabakat tamamlandı bildirimi.
/// Client event: "ReconciliationDone" → { erpProvider, matched, unmatched, generatedAt }
/// </summary>
public static async Task NotifyReconciliationDone(
    IHubContext<MesTechHub> hubContext,
    string tenantId,
    string erpProvider,
    int matchedCount,
    int unmatchedCount)
{
    await hubContext.Clients.Group($"tenant-{tenantId}").SendAsync("ReconciliationDone", new
    {
        erpProvider,
        matchedCount,
        unmatchedCount,
        generatedAt = DateTime.UtcNow
    });
}
```

## 3. NotifyFulfillmentAlert (line 126-140)

```csharp
/// <summary>
/// Fulfillment stok uyarısı bildirimi.
/// Client event: "FulfillmentAlert" → { center, alertType, message, timestamp }
/// </summary>
public static async Task NotifyFulfillmentAlert(
    IHubContext<MesTechHub> hubContext,
    string tenantId,
    string center,
    string alertType,
    string message)
{
    await hubContext.Clients.Group($"tenant-{tenantId}").SendAsync("FulfillmentAlert", new
    {
        center,
        alertType,
        message,
        timestamp = DateTime.UtcNow
    });
}
```
