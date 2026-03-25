using MassTransit;
using MesTech.Application.Interfaces;

namespace MesTech.Infrastructure.Messaging;

/// <summary>
/// MassTransit IPublishEndpoint üzerinden IMessagePublisher implementasyonu.
/// Application katmanını MassTransit bağımlılığından ayırır.
/// </summary>
public sealed class MassTransitMessagePublisher : IMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public MassTransitMessagePublisher(IPublishEndpoint publishEndpoint)
        => _publishEndpoint = publishEndpoint;

    public Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
        => _publishEndpoint.Publish(message, ct);
}
