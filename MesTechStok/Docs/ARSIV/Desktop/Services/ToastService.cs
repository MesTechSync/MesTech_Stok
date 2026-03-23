using System.Windows;
using System.Windows.Controls;
using MesTechStok.Desktop.Controls;

namespace MesTechStok.Desktop.Services;

/// <summary>
/// Toast bildirim servisi — sag alt kosede 3sn gosterim.
/// Usage: ToastService.Show("Basarili", "Urun kaydedildi", ToastType.Success);
/// </summary>
public static class ToastService
{
    private static Panel? _container;
    private const int MaxVisible = 3;

    /// <summary>
    /// Ana penceredeki toast container'i ayarla. App.xaml.cs OnStartup'ta cagrilmali.
    /// </summary>
    public static void Initialize(Panel container)
    {
        _container = container;
    }

    public static void Show(string title, string message, ToastType type = ToastType.Info)
    {
        if (_container == null)
            return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            // Limit visible toasts
            while (_container.Children.Count >= MaxVisible)
            {
                if (_container.Children[0] is ToastNotification oldest)
                    oldest.Hide();
                else
                    _container.Children.RemoveAt(0);
            }

            var toast = new ToastNotification
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 20, 0)
            };

            _container.Children.Add(toast);
            toast.Show(title, message, type);
        });
    }

    public static void Success(string title, string message) => Show(title, message, ToastType.Success);
    public static void Warning(string title, string message) => Show(title, message, ToastType.Warning);
    public static void Error(string title, string message) => Show(title, message, ToastType.Error);
    public static void Info(string title, string message) => Show(title, message, ToastType.Info);
}
