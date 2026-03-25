namespace MesTech.Application.Interfaces;

/// <summary>
/// İnternet bağlantısı durumunu izler.
/// Tam implementasyon Dalga 3'te, şimdi sadece kontrat.
/// </summary>
public interface IConnectivityService
{
    bool IsOnline { get; }
    Task<bool> CheckConnectivityAsync(CancellationToken ct = default);
    event EventHandler<ConnectivityChangedEventArgs>? ConnectivityChanged;
}

public sealed class ConnectivityChangedEventArgs : EventArgs
{
    public bool IsOnline { get; }

    public ConnectivityChangedEventArgs(bool isOnline)
    {
        IsOnline = isOnline;
    }
}
