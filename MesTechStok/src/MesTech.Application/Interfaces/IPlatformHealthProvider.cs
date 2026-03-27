namespace MesTech.Application.Interfaces;

/// <summary>
/// Platform sağlık verisi sağlayıcısı — son 24 saatlik uptime, hata sayısı, yanıt süresi.
/// Infrastructure'da PlatformHealthHistory ile implement edilir.
/// Application handler'ları bu interface üzerinden health verisi çeker.
/// </summary>
public interface IPlatformHealthProvider
{
    /// <summary>
    /// Belirli bir platform için sağlık özeti döner (son 24 saat).
    /// Null dönerse: o platform için henüz health check yapılmamış.
    /// </summary>
    PlatformHealthSummaryDto? GetHealthSummary(string platformCode);

    /// <summary>
    /// Tüm platformların sağlık özetlerini döner.
    /// </summary>
    IReadOnlyList<PlatformHealthSummaryDto> GetAllHealthSummaries();
}

/// <summary>
/// Platform sağlık özeti DTO — Application katmanı kullanımı için.
/// </summary>
public sealed record PlatformHealthSummaryDto(
    string PlatformCode,
    DateTime LastCheckUtc,
    decimal UptimePercent24h,
    int FailedChecks24h,
    long AvgResponseTimeMs,
    int TotalChecks24h);
