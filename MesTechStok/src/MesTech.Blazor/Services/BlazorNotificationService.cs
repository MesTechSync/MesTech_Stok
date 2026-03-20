namespace MesTech.Blazor.Services;

public sealed record AppNotification(string Title, string Message, DateTime Timestamp, string Icon = "fa-bell", string Level = "info");

public interface IBlazorNotificationService
{
    IReadOnlyList<AppNotification> Notifications { get; }
    int UnreadCount { get; }
    event Action? OnChange;
    void Push(string title, string message, string icon = "fa-bell", string level = "info");
    void MarkAllRead();
    void Clear();
}

public sealed class BlazorNotificationService : IBlazorNotificationService
{
    private readonly List<AppNotification> _notifications = [];
    private int _unreadCount;

    public IReadOnlyList<AppNotification> Notifications => _notifications;
    public int UnreadCount => _unreadCount;
    public event Action? OnChange;

    public void Push(string title, string message, string icon = "fa-bell", string level = "info")
    {
        _notifications.Insert(0, new AppNotification(title, message, DateTime.Now, icon, level));
        if (_notifications.Count > 50) _notifications.RemoveAt(_notifications.Count - 1);
        _unreadCount++;
        OnChange?.Invoke();
    }

    public void MarkAllRead()
    {
        _unreadCount = 0;
        OnChange?.Invoke();
    }

    public void Clear()
    {
        _notifications.Clear();
        _unreadCount = 0;
        OnChange?.Invoke();
    }
}
