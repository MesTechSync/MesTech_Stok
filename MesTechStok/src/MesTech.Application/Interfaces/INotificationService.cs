namespace MesTech.Application.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(string title, string message, NotificationLevel level = NotificationLevel.Info, CancellationToken ct = default);
    Task NotifyLowStockAsync(string sku, int currentStock, int minimumStock, CancellationToken ct = default);
    Task NotifySyncErrorAsync(string platformCode, string errorMessage, CancellationToken ct = default);
}

public enum NotificationLevel
{
    Info,
    Warning,
    Error,
    Critical
}
