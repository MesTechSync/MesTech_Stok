using AvNotification = Avalonia.Controls.Notifications;

namespace MesTech.Avalonia.Services;

/// <summary>
/// Toast notification service using Avalonia WindowNotificationManager.
/// Registered alongside Ursa theme (UrsaTheme in App.axaml) for future Ursa control adoption.
/// </summary>
public interface IToastService
{
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
}

public class ToastService : IToastService
{
    private AvNotification.WindowNotificationManager? _manager;

    private AvNotification.WindowNotificationManager? GetManager()
    {
        if (_manager != null) return _manager;

        if (global::Avalonia.Application.Current?.ApplicationLifetime is
            global::Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is { } window)
        {
            _manager = new AvNotification.WindowNotificationManager(window)
            {
                Position = AvNotification.NotificationPosition.TopRight,
                MaxItems = 4,
            };
        }

        return _manager;
    }

    public void ShowSuccess(string message) =>
        GetManager()?.Show(new AvNotification.Notification("Basarili", message, AvNotification.NotificationType.Success));

    public void ShowError(string message) =>
        GetManager()?.Show(new AvNotification.Notification("Hata", message, AvNotification.NotificationType.Error));

    public void ShowWarning(string message) =>
        GetManager()?.Show(new AvNotification.Notification("Uyari", message, AvNotification.NotificationType.Warning));

    public void ShowInfo(string message) =>
        GetManager()?.Show(new AvNotification.Notification("Bilgi", message, AvNotification.NotificationType.Information));
}
