namespace MesTech.Application.Interfaces;

/// <summary>
/// MESA OS event istatistik takibi.
/// Bridge handler'lar ve consumer'lar her event'te bu servisi cagirir.
/// </summary>
public interface IMesaEventMonitor
{
    /// <summary>MESA'ya publish edilen event kaydi.</summary>
    void RecordPublish(string eventType);

    /// <summary>MESA'dan consume edilen event kaydi.</summary>
    void RecordConsume(string eventType);

    /// <summary>Hata kaydi.</summary>
    void RecordError(string eventType, string errorMessage);

    /// <summary>Tum istatistikleri doner.</summary>
    MesaMonitorStatus GetStatus();
}

/// <summary>Monitoring durum raporu.</summary>
public record MesaMonitorStatus(
    string BridgeStatus,
    Dictionary<string, EventCounter> Events,
    long UptimeSeconds);

/// <summary>Tek bir event tipi icin sayac.</summary>
public record EventCounter(
    long Published,
    long Consumed,
    long Errors,
    DateTime? LastPublishAt,
    DateTime? LastConsumeAt);
