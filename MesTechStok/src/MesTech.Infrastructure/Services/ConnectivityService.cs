using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.Services;

/// <summary>
/// İnternet bağlantı kontrolü — iskelet implementasyon (Dalga 3'te tamamlanacak).
/// Şu an her zaman online döner.
/// </summary>
public sealed class ConnectivityService : IConnectivityService
{
    private readonly ILogger<ConnectivityService> _logger;

    public ConnectivityService(ILogger<ConnectivityService> logger)
    {
        _logger = logger;
    }

    public bool IsOnline => true;

    public Task<bool> CheckConnectivityAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;
}
