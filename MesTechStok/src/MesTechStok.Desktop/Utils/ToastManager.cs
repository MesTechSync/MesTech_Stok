using System;
using System.Windows;
using System.Windows.Threading;

namespace MesTechStok.Desktop.Utils
{
    public static class ToastManager
    {
        public static void ShowSuccess(string message, string title = "Başarılı")
        {
            ShowToast(message, title, MessageBoxImage.Information);
        }

        public static void ShowError(string message, string title = "Hata")
        {
            ShowToast(message, title, MessageBoxImage.Error);
        }

        public static void ShowWarning(string message, string title = "Uyarı")
        {
            ShowToast(message, title, MessageBoxImage.Warning);
        }

        public static void ShowInfo(string message, string title = "Bilgi")
        {
            ShowToast(message, title, MessageBoxImage.Information);
        }

        private static void ShowToast(string message, string title, MessageBoxImage icon)
        {
            try
            {
                // Ensure we're on the UI thread
                if (Application.Current?.Dispatcher?.CheckAccess() == false)
                {
                    Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Normal,
                        new Action(() => ShowToast(message, title, icon)));
                    return;
                }

                // For now, use MessageBox as a simple implementation
                // In a real application, you might want to use a custom toast notification
                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            }
            catch (Exception ex)
            {
                // Fallback to console if UI is not available
#if DEBUG
                Console.WriteLine($"[TOAST] {title}: {message}");
                Console.WriteLine($"[ERROR] Toast display failed: {ex.Message}");
#endif
            }
        }

        // Enhanced toast methods for better UX (future implementation)
        public static void ShowToastWithTimeout(string message, string title = "Bilgi", int timeoutMs = 3000)
        {
            // This could be implemented with a custom WPF UserControl in the future
            ShowToast(message, title, MessageBoxImage.Information);
        }

        public static void ShowProgressToast(string message, string title = "İşlem Devam Ediyor")
        {
            // This could show a progress indicator in the future
            ShowToast(message, title, MessageBoxImage.Information);
        }
    }
}