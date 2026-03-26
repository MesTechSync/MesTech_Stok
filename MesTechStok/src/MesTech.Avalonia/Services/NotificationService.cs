using System.Collections.ObjectModel;

namespace MesTech.Avalonia.Services;

/// <summary>
/// Notification type for icon / color mapping.
/// </summary>
public enum NotificationType { Success, Error, Warning, Info }

/// <summary>
/// Single toast notification item — bound to the overlay ItemsControl in MainWindow.
/// </summary>
public class NotificationItem
{
    public string Message  { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public string Icon     { get; init; } = string.Empty;
    public string Color    { get; init; } = string.Empty;
    public bool IsClosing  { get; set;  }
}

/// <summary>
/// Shows auto-dismissing toast notifications overlaid on the main window.
/// Max 5 visible at a time; success/info dismiss after 3s, error/warning after 5s.
/// </summary>
public interface INotificationService
{
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
    ObservableCollection<NotificationItem> Notifications { get; }
}

public class NotificationService : INotificationService
{
    private const int MaxVisible   = 5;
    private const int SuccessDelay = 3000;
    private const int ErrorDelay   = 5000;

    public ObservableCollection<NotificationItem> Notifications { get; } = [];

    public void ShowSuccess(string message) =>
        Show(message, NotificationType.Success, "✓", "#2ECC71", SuccessDelay);

    public void ShowError(string message) =>
        Show(message, NotificationType.Error, "✕", "#E74C3C", ErrorDelay);

    public void ShowWarning(string message) =>
        Show(message, NotificationType.Warning, "⚠", "#F39C12", ErrorDelay);

    public void ShowInfo(string message) =>
        Show(message, NotificationType.Info, "ℹ", "#3498DB", SuccessDelay);

    // ── internals ──────────────────────────────────────────────────────────────

    private void Show(string message, NotificationType type, string icon, string color, int dismissMs)
    {
        var item = new NotificationItem
        {
            Message = message,
            Type    = type,
            Icon    = icon,
            Color   = color
        };

        // Enforce max-5 cap (remove oldest first)
        while (Notifications.Count >= MaxVisible)
            Notifications.RemoveAt(0);

        Notifications.Add(item);

        // Auto-dismiss
        _ = Task.Run(async () =>
        {
            await Task.Delay(dismissMs);
            Dismiss(item);
        });
    }

    private void Dismiss(NotificationItem item)
    {
        // Marshal to UI thread via Avalonia dispatcher
        global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            item.IsClosing = true;
            Notifications.Remove(item);
        });
    }
}
