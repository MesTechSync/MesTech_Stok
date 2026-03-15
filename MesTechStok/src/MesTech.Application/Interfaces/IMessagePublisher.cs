namespace MesTech.Application.Interfaces;

/// <summary>
/// Mesaj yayimlama soyutlamasi.
/// Infrastructure katmaninda MassTransit IPublishEndpoint ile implemente edilir.
/// Application katmanini MassTransit bagimliligindan ayirir.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Mesaji ilgili exchange/queue'ya yayimlar.
    /// </summary>
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
}
