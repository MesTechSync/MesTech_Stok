namespace MesTech.Application.Interfaces;

/// <summary>
/// Feed sync job tetikleme arayüzü.
/// Infrastructure'da Hangfire ile implemente edilir.
/// </summary>
public interface IFeedSyncJobService
{
    /// <summary>Belirtilen feed için tek seferlik sync job'ı kuyruğa ekler.</summary>
    string EnqueueFeedSync(Guid feedId);
}
