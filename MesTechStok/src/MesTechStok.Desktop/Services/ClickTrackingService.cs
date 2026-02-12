using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MesTechStok.Desktop.Utils;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// Otomatik tıklama izleme ve kullanıcı aktivite takip servisi
    /// </summary>
    public interface IClickTrackingService
    {
        bool IsEnabled { get; set; }
        void StartTracking();
        void StopTracking();
        Task<List<ClickEvent>> GetClickHistoryAsync();
        event EventHandler<ClickEvent>? ClickRecorded;
    }

    public class ClickTrackingService : IClickTrackingService
    {
        private readonly ILogger<ClickTrackingService> _logger;
        private readonly List<ClickEvent> _clickHistory = new();
        private readonly object _lockObject = new();
        private bool _isTracking = false;

        public bool IsEnabled { get; set; } = true;
        public event EventHandler<ClickEvent>? ClickRecorded;

        public ClickTrackingService(ILogger<ClickTrackingService> logger)
        {
            _logger = logger;
        }

        public void StartTracking()
        {
            if (_isTracking) return;

            try
            {
                _isTracking = true;

                // Ana Application'a event handler ekle
                if (Application.Current?.MainWindow != null)
                {
                    RegisterWindowEvents(Application.Current.MainWindow);
                }

                GlobalLogger.Instance.LogInfo("🖱️ Click tracking başlatıldı", "ClickTracking");
                _logger.LogInformation("Click tracking service started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Click tracking başlatma hatası");
            }
        }

        public void StopTracking()
        {
            if (!_isTracking) return;

            try
            {
                _isTracking = false;
                GlobalLogger.Instance.LogInfo("🖱️ Click tracking durduruldu", "ClickTracking");
                _logger.LogInformation("Click tracking service stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Click tracking durdurma hatası");
            }
        }

        private void RegisterWindowEvents(Window window)
        {
            if (window == null) return;

            try
            {
                window.PreviewMouseLeftButtonDown += Window_PreviewMouseLeftButtonDown;
                window.PreviewMouseRightButtonDown += Window_PreviewMouseRightButtonDown;
                window.PreviewKeyDown += Window_PreviewKeyDown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Window event registration failed");
            }
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isTracking || !IsEnabled) return;
            RecordClick("LeftClick", GetClickDetails(sender, e));
        }

        private void Window_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_isTracking || !IsEnabled) return;
            RecordClick("RightClick", GetClickDetails(sender, e));
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isTracking || !IsEnabled) return;

            if (e.Key == Key.Enter || e.Key == Key.Escape || e.Key == Key.Tab)
            {
                var clickEvent = new ClickEvent
                {
                    Timestamp = DateTime.Now,
                    Type = "KeyPress",
                    Key = e.Key.ToString(),
                    WindowTitle = (sender as Window)?.Title ?? "Unknown",
                    ElementName = "Keyboard",
                    Details = $"Key: {e.Key}"
                };

                RecordClickEvent(clickEvent);
            }
        }

        private string GetClickDetails(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var element = e.OriginalSource as FrameworkElement;
                var position = e.GetPosition(sender as IInputElement);

                return $"Element: {element?.Name ?? "Unknown"}, Position: ({position.X:F0}, {position.Y:F0})";
            }
            catch
            {
                return "Click details unavailable";
            }
        }

        private void RecordClick(string type, string details)
        {
            var clickEvent = new ClickEvent
            {
                Timestamp = DateTime.Now,
                Type = type,
                Details = details,
                WindowTitle = Application.Current?.MainWindow?.Title ?? "Unknown"
            };

            RecordClickEvent(clickEvent);
        }

        private void RecordClickEvent(ClickEvent clickEvent)
        {
            lock (_lockObject)
            {
                _clickHistory.Add(clickEvent);

                if (_clickHistory.Count > 1000)
                {
                    _clickHistory.RemoveRange(0, _clickHistory.Count - 1000);
                }
            }

            var logMessage = $"🖱️ {clickEvent.Type}: {clickEvent.Details} - {clickEvent.WindowTitle}";
            GlobalLogger.Instance.LogInfo(logMessage, "ClickTracking");

            ClickRecorded?.Invoke(this, clickEvent);
        }

        public async Task<List<ClickEvent>> GetClickHistoryAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    return new List<ClickEvent>(_clickHistory);
                }
            });
        }
    }

    /// <summary>
    /// Click event detayları
    /// </summary>
    public class ClickEvent
    {
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = string.Empty; // "LeftClick", "RightClick", "ButtonClick", "MenuClick", etc.
        public string ElementName { get; set; } = string.Empty;
        public string ElementContent { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty; // For keyboard events
    }
}