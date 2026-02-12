using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Serilog;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using MesTechStok.Core.Diagnostics;
using MesTechStok.Core.Interfaces;
using System.IO.Compression;

namespace MesTechStok.Desktop.Views
{
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Media.Imaging;

    public partial class LogView : UserControl
    {
        private readonly List<LogEntry> _allLogs;
        private readonly DispatcherTimer _refreshTimer;
        private int _errorCount = 0;
        private string _searchText = "";
        private string _selectedLevel = "Tümü";
        private string _selectedEventType = "Tümü";
        private int _displayLimit = 200;
        private string _correlationFilter = string.Empty;
        private bool _auditOnly = false;
        private bool _liveEnabled = true;
        private bool _suspendUi = false;

        public LogView()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // XAML parse / init hatasını dosyaya da yaz
                Log.Error(ex, "LogView InitializeComponent failed");
                MessageBox.Show($"LogView yükleme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
            _allLogs = new List<LogEntry>();

            // A++++ LOG İYİLEŞTİRMESİ: Türkçe karakter sorun tespiti
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    MesTechStok.Desktop.Utils.GlobalLogger.Instance.LoadLogsWithCharacterValidation();
                }
                catch (Exception ex)
                {
                    AddLog("🔴", $"Log karakter analizi başlatma hatası: {ex.Message}", "LogView", Colors.Red);
                }
            }), DispatcherPriority.Loaded);

            // Ekran daralırsa üst filtre çubuğu taşmasın: combo/text genişliklerini azalt
            SizeChanged += (_, __) =>
            {
                try
                {
                    var width = ActualWidth;
                    if (width > 0)
                    {
                        double scale = width < 900 ? 0.85 : (width < 700 ? 0.75 : 1.0);
                        if (LogLevelFilter != null) LogLevelFilter.Width = 160 * scale;
                        if (EventTypeFilter != null) EventTypeFilter.Width = 200 * scale;
                        if (SearchBox != null) SearchBox.Width = 220 * scale;
                        if (CorrelationFilter != null) CorrelationFilter.Width = 200 * scale;
                    }
                }
                catch { }
            };

            // Auto refresh timer
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();

            // Subscribe to unified global logger (Utils)
            MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAdded += OnLogAdded;

            // USB HID entegrasyonu: Core dinleyicinin olaylarını merkeze köprüle
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var hid = sp?.GetService<MesTechStok.Core.Integrations.Barcode.IBarcodeScannerService>();
                if (hid != null)
                {
                    hid.BarcodeScanned += (s, e) =>
                    {
                        try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogEvent("BARCODE", $"HID received value={e.Barcode}", "USB_HID"); } catch { }
                    };
                    hid.ScanError += (s, e) =>
                    {
                        try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogError($"HID error: {e.ErrorMessage}", "USB_HID"); } catch { }
                    };
                }
            }
            catch { }

            // Global buffer'daki geçmiş logları ilk yüklemede getir (tek UI güncellemesiyle)
            try
            {
                _suspendUi = true;
                var snapshot = MesTechStok.Desktop.Utils.GlobalLogger.Instance.GetSnapshot(500);
                foreach (var entry in snapshot)
                {
                    var normalizedLevel = NormalizeLevel(entry.Level);
                    string eventType = "";
                    string sanitizedMessage = entry.Message;
                    if (!string.IsNullOrWhiteSpace(entry.Message) && entry.Message.Length > 3 && entry.Message[0] == '[')
                    {
                        var endIdx = entry.Message.IndexOf(']');
                        if (endIdx > 1 && endIdx < 64)
                        {
                            eventType = entry.Message.Substring(1, endIdx - 1).Trim();
                            sanitizedMessage = entry.Message.Length > endIdx + 1 && entry.Message[endIdx + 1] == ' '
                                ? entry.Message.Substring(endIdx + 2)
                                : entry.Message.Substring(endIdx + 1);
                        }
                    }
                    _allLogs.Insert(0, new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Level = normalizedLevel,
                        Message = sanitizedMessage,
                        Source = entry.Source,
                        Color = entry.Color,
                        EventType = eventType
                    });
                }
            }
            catch { }
            finally
            {
                _suspendUi = false;
                UpdateLogDisplay();
            }

            // Add initial log
            AddLog("ℹ️", "Log sistemi başlatıldı", "System", Colors.Blue);
        }

        private void OnLogAdded(object? sender, LogEntry log)
        {
            if (!_liveEnabled) return;
            Dispatcher.Invoke(() => AddLog(log.Level, log.Message, log.Source, log.Color));
        }

        private static string NormalizeLevel(string level)
        {
            var lv = (level ?? string.Empty).Trim();
            return lv switch
            {
                "🔴" or "Hata" or "ERROR" => "🔴 Hata",
                "⚠️" or "Uyarı" or "WARNING" => "⚠️ Uyarı",
                "ℹ️" or "Bilgi" or "INFO" => "ℹ️ Bilgi",
                "🐛" or "Debug" or "DEBUG" => "🐛 Debug",
                _ => lv
            };
        }

        public void AddLog(string level, string message, string source, Color color)
        {
            try
            {
                var normalizedLevel = NormalizeLevel(level);
                // EventType ayrıştırma: [EVENT] Mesaj → EventType=EVENT, Message=Mesaj
                string eventType = "";
                string sanitizedMessage = message;
                if (!string.IsNullOrWhiteSpace(message) && message.Length > 3 && message[0] == '[')
                {
                    var endIdx = message.IndexOf(']');
                    if (endIdx > 1 && endIdx < 64)
                    {
                        eventType = message.Substring(1, endIdx - 1).Trim();
                        if (message.Length > endIdx + 1 && message[endIdx + 1] == ' ')
                            sanitizedMessage = message.Substring(endIdx + 2);
                        else
                            sanitizedMessage = message.Substring(endIdx + 1);
                    }
                }

                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = normalizedLevel,
                    Message = sanitizedMessage,
                    Source = source,
                    Color = color,
                    EventType = eventType
                };

                _allLogs.Insert(0, logEntry);

                if (normalizedLevel == "🔴 Hata")
                {
                    _errorCount++;
                    ErrorCountText.Text = $"🔴 {_errorCount} hata";
                }

                // Keep only last 1000 logs
                if (_allLogs.Count > 1000)
                {
                    _allLogs.RemoveAt(_allLogs.Count - 1);
                }

                if (!_suspendUi)
                {
                    UpdateLogDisplay();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log add error: {ex.Message}");
            }
        }

        private void UpdateLogDisplay()
        {
            try
            {
                // Sanallaştırılan listeye geçildi: LogListBox kullan
                var items = new List<LogEntry>();

                var filteredLogs = _allLogs.AsEnumerable();

                // Level filter
                if (_selectedLevel != "Tümü")
                {
                    filteredLogs = filteredLogs.Where(l => l.Level == _selectedLevel);
                }

                // EventType filter (EventType köşeli mesaj formatında: [TYPE] ... )
                if (_selectedEventType != "Tümü")
                {
                    filteredLogs = filteredLogs.Where(l => string.Equals(l.EventType, _selectedEventType, StringComparison.OrdinalIgnoreCase));
                }

                // Search filter
                if (!string.IsNullOrEmpty(_searchText) && _searchText != "Arama...")
                {
                    filteredLogs = filteredLogs.Where(l =>
                        l.Message.ToLower().Contains(_searchText.ToLower()) ||
                        l.Source.ToLower().Contains(_searchText.ToLower()));
                }

                // CorrelationId filter (Message içinde corrId=... deseni arar)
                if (!string.IsNullOrWhiteSpace(_correlationFilter))
                {
                    var token = _correlationFilter.Trim();
                    filteredLogs = filteredLogs.Where(l => l.Message.Contains(token, StringComparison.OrdinalIgnoreCase));
                }

                // Audit-only toggle
                if (_auditOnly)
                {
                    filteredLogs = filteredLogs.Where(l => l.Message.StartsWith("[AUDIT:"));
                }

                // En yeni loglar en üstte görünsün
                var displayLogs = filteredLogs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(_displayLimit)
                    .ToList();

                LogListBox.ItemsSource = displayLogs;

                LogCountText.Text = $"({displayLogs.Count} log)";
                LastUpdateText.Text = $"Son güncelleme: {DateTime.Now:HH:mm:ss}";

                // Otomatik kaydırma (en üste)
                // ListBox varsayılan scroll davranışı yeterli; otomatik kaydırma gerekmiyor
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log display update error: {ex.Message}");
            }
        }

        // CreateLogBlock kaldırıldı; sanallaştırma ile DataTemplate kullanılıyor

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            // Timer'da özel bir işlem yapmıyoruz, loglar gerçek zamanlı ekleniyor
        }

        private void RefreshLogs_Click(object sender, RoutedEventArgs e)
        {
            UpdateLogDisplay();
            StatusText.Text = "Loglar yenilendi";
        }

        private void ClearLogs_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Tüm logları temizlemek istiyor musunuz?\n\nEvet: Sadece ekrandaki logları temizle\nHayır: İptal\n\nDisk loglarını da (Logs/*.log) silmek için 'Evet'e bastıktan sonra bir soru daha gelecek.",
                "Logları Temizle", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _allLogs.Clear();
                _errorCount = 0;
                ErrorCountText.Text = "🔴 0 hata";
                UpdateLogDisplay();
                StatusText.Text = "Loglar temizlendi";

                AddLog("ℹ️", "Loglar temizlendi", "System", Colors.Blue);

                // Optional: ask for disk purge with dry-run preview
                try
                {
                    var logsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                    if (Directory.Exists(logsDir))
                    {
                        var files = Directory.GetFiles(logsDir, "mestech-*.log");
                        if (files.Length > 0)
                        {
                            var preview = string.Join("\n", files.Select(f => System.IO.Path.GetFileName(f)).Take(10));
                            var more = files.Length > 10 ? $"\n... (+{files.Length - 10} daha)" : string.Empty;
                            var confirmDisk = MessageBox.Show(
                                $"Diskte {files.Length} log dosyası bulundu.\n\nÖrnekler:\n{preview}{more}\n\nBu dosyaları da silmek istiyor musunuz?",
                                "Disk Loglarını Temizle", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                            if (confirmDisk == MessageBoxResult.Yes)
                            {
                                int deleted = 0, failed = 0;
                                foreach (var f in files)
                                {
                                    try { File.Delete(f); deleted++; }
                                    catch { failed++; }
                                }
                                StatusText.Text = $"Disk logları temizlendi: {deleted} silindi, {failed} başarısız";
                                AddLog("ℹ️", StatusText.Text, "System", Colors.Blue);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddLog("🔴", $"Disk log temizleme hatası: {ex.Message}", "LogView", Colors.Red);
                }
            }
        }

        private async void CorrelationSelfTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var corr = CorrelationContext.StartNew($"TEST-{Guid.NewGuid():N}".Substring(0, 12));
                var corrId = CorrelationContext.CurrentId ?? "<none>";

                // 1) Serilog + UI log
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("SELFTEST", $"corrId={corrId} message=Correlation self-test başlıyor", nameof(LogView)); } catch { }
                Log.Information("[SelfTest] Starting correlation self-test with CorrelationId={CorrelationId}", corrId);

                // 2) DB telemetry: write a dummy ApiCallLog
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                if (sp == null) { AddLog("🔴 Hata", "Self-test: ServiceProvider null", nameof(LogView), Colors.Red); return; }
                using var scope = sp.CreateScope();
                var telemetry = scope.ServiceProvider.GetService<ITelemetryService>();
                if (telemetry == null) { AddLog("🔴 Hata", "Self-test: ITelemetryService bulunamadı", nameof(LogView), Colors.Red); return; }

                await telemetry.LogApiCallAsync("/self-test", "GET", success: true, statusCode: 200, durationMs: 1, category: "SELFTEST", correlationId: corrId);

                // 3) Proof to UI
                AddLog("ℹ️", $"[SELFTEST] CorrId={corrId} hem dosya loguna hem DB'ye yazıldı. CorrId ile filtreleyin.", nameof(LogView), Colors.DarkGreen);
                StatusText.Text = $"SELFTEST tamamlandı: CorrId={corrId}";
                CorrelationFilter.Text = corrId;
            }
            catch (Exception ex)
            {
                AddLog("🔴 Hata", $"Correlation self-test hatası: {ex.Message}", nameof(LogView), Colors.Red);
            }
        }

        private void SafePurge_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logsDir)) { StatusText.Text = "Logs klasörü yok"; return; }

                var files = Directory.GetFiles(logsDir, "mestech-*.log");
                long totalBytes = files.Sum(f => new FileInfo(f).Length);
                var summary = $"{files.Length} dosya, toplam {totalBytes / 1024.0:F1} KB";

                var ans = MessageBox.Show($"Günlükleri güvenli temizlemek istiyor musunuz?\n\nÖnizleme: {summary}\n\nEvet: Önce ZIP yedeği al ve sonra sil\nHayır: İptal", "Güvenli Temizle", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (ans != MessageBoxResult.Yes) return;

                // 1) Backup
                string? backupZip = null;
                try
                {
                    var backupDir = System.IO.Path.Combine(logsDir, "Backup");
                    Directory.CreateDirectory(backupDir);
                    backupZip = System.IO.Path.Combine(backupDir, $"logs_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
                    using (var zip = ZipFile.Open(backupZip, ZipArchiveMode.Create))
                    {
                        foreach (var f in files)
                        {
                            zip.CreateEntryFromFile(f, System.IO.Path.GetFileName(f));
                        }
                    }
                }
                catch (Exception ex)
                {
                    AddLog("🔴 Hata", $"Yedek alma başarısız: {ex.Message}", nameof(LogView), Colors.Red);
                    var cont = MessageBox.Show("Yedek alınamadı. Silme işlemine yine de devam edilsin mi?", "Uyarı", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (cont != MessageBoxResult.Yes) return;
                }

                // 2) Delete
                int deleted = 0, failed = 0;
                foreach (var f in files)
                {
                    try { File.Delete(f); deleted++; }
                    catch { failed++; }
                }

                // 3) Audit + UI
                try { MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAudit("LOG_PURGE", $"deleted={deleted} failed={failed} backup={(backupZip ?? "<none>")}", nameof(LogView)); } catch { }
                AddLog("ℹ️", $"Güvenli temizleme tamamlandı: {deleted} silindi, {failed} başarısız. Yedek={(backupZip ?? "yok")}", nameof(LogView), Colors.SteelBlue);
                StatusText.Text = "Güvenli temizleme tamamlandı";
            }
            catch (Exception ex)
            {
                AddLog("🔴 Hata", $"Güvenli temizleme hatası: {ex.Message}", nameof(LogView), Colors.Red);
            }
        }

        private void SaveLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|Log files (*.log)|*.log|All files (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"MesTech_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var logContent = string.Join("\n", _allLogs.Select(l =>
                        $"[{l.Timestamp:yyyy-MM-dd HH:mm:ss}] {l.Level} [{l.Source}] {l.Message}"));

                    File.WriteAllText(saveFileDialog.FileName, logContent);
                    StatusText.Text = $"Loglar kaydedildi: {Path.GetFileName(saveFileDialog.FileName)}";

                    MessageBox.Show($"Loglar başarıyla kaydedildi!\n\nDosya: {saveFileDialog.FileName}",
                        "Kaydet", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                AddLog("🔴", $"Log kaydetme hatası: {ex.Message}", "LogView", Colors.Red);
            }
        }

        private void SaveLogsCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"MesTech_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Şu an ekranda görünen filtreli listeden CSV üretelim
                    var filteredLogs = _allLogs.AsEnumerable();
                    if (_selectedLevel != "Tümü")
                        filteredLogs = filteredLogs.Where(l => l.Level == _selectedLevel);
                    if (!string.IsNullOrEmpty(_searchText) && _searchText != "Arama...")
                        filteredLogs = filteredLogs.Where(l => l.Message.ToLower().Contains(_searchText.ToLower()) || l.Source.ToLower().Contains(_searchText.ToLower()));
                    if (_selectedEventType != "Tümü")
                        filteredLogs = filteredLogs.Where(l => string.Equals(l.EventType, _selectedEventType, StringComparison.OrdinalIgnoreCase));

                    var export = filteredLogs
                        .OrderByDescending(l => l.Timestamp)
                        .Take(_displayLimit)
                        .Select(l => new[]
                        {
                            l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                            l.Level,
                            string.IsNullOrWhiteSpace(l.EventType) ? "-" : l.EventType,
                            l.Source,
                            l.Message.Replace("\n", " ").Replace("\r", " ")
                        })
                        .ToList();

                    using var sw = new System.IO.StreamWriter(saveFileDialog.FileName, false, System.Text.Encoding.UTF8);
                    sw.WriteLine("Timestamp,Level,EventType,Source,Message");
                    foreach (var row in export)
                    {
                        sw.WriteLine(string.Join(",", row.Select(value => "\"" + value.Replace("\"", "\"\"") + "\"")));
                    }
                    StatusText.Text = $"CSV kaydedildi: {System.IO.Path.GetFileName(saveFileDialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                AddLog("🔴", $"CSV kaydetme hatası: {ex.Message}", "LogView", Colors.Red);
            }
        }

        private void LogLevelFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (LogLevelFilter.SelectedItem is ComboBoxItem item)
            {
                _selectedLevel = item.Content.ToString() ?? "Tümü";
                UpdateLogDisplay();
            }
        }

        private void EventTypeFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (EventTypeFilter.SelectedItem is ComboBoxItem item)
            {
                _selectedEventType = item.Content.ToString() ?? "Tümü";
                UpdateLogDisplay();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchBox.Text;
            UpdateLogDisplay();
        }

        private void DisplayLimitCombo_ChangedInternal()
        {
            if (DisplayLimitCombo?.SelectedItem is ComboBoxItem item && int.TryParse(item.Content.ToString(), out var limit))
            {
                _displayLimit = limit;
                UpdateLogDisplay();
            }
        }

        private void DisplayLimitCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DisplayLimitCombo_ChangedInternal();
        }

        private void ChangelogFilter_Click(object sender, RoutedEventArgs e)
        {
            // Hızlı filtre: CHANGELOG
            _selectedEventType = "CHANGELOG";
            if (EventTypeFilter != null)
            {
                foreach (var it in EventTypeFilter.Items)
                {
                    if (it is ComboBoxItem cb && (cb.Content?.ToString() ?? "") == "CHANGELOG")
                    {
                        EventTypeFilter.SelectedItem = cb;
                        break;
                    }
                }
            }
            UpdateLogDisplay();
        }

        private void CorrelationFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            _correlationFilter = CorrelationFilter.Text ?? string.Empty;
            UpdateLogDisplay();
        }

        private void AuditOnlyCheck_Changed(object sender, RoutedEventArgs e)
        {
            _auditOnly = AuditOnlyCheck.IsChecked == true;
            UpdateLogDisplay();
        }

        private void LoadFileLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openDlg = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Log/Metin|*.log;*.txt;*.csv|Tümü|*.*",
                    Multiselect = false,
                    InitialDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs")
                };
                if (openDlg.ShowDialog() == true)
                {
                    var lines = System.IO.File.ReadAllLines(openDlg.FileName);
                    // Basit satır içe aktarma: her satırı INFO olarak ekle (görsel amaçlı)
                    foreach (var line in lines.Reverse())
                    {
                        AddLog("ℹ️", line, System.IO.Path.GetFileName(openDlg.FileName), Colors.SteelBlue);
                    }
                    StatusText.Text = $"Dosyadan {lines.Length} satır yüklendi";
                }
            }
            catch (Exception ex)
            {
                AddLog("🔴", $"Dosya log yükleme hatası: {ex.Message}", "LogView", Colors.Red);
            }
        }

        private void LivePauseCheck_Changed(object sender, RoutedEventArgs e)
        {
            _liveEnabled = LivePauseCheck.IsChecked == true;
            if (StatusText != null)
            {
                StatusText.Text = _liveEnabled ? "Canlı akış açık" : "Canlı akış duraklatıldı";
            }
            else
            {
                // UI henüz tam inşa edilmediyse, oluşturma sonrasına ertele
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (StatusText != null)
                    {
                        StatusText.Text = _liveEnabled ? "Canlı akış açık" : "Canlı akış duraklatıldı";
                    }
                }), DispatcherPriority.Loaded);
            }
        }

        private void QuickBarcode_Click(object sender, RoutedEventArgs e)
        {
            _selectedEventType = "BARCODE";
            if (EventTypeFilter != null)
            {
                foreach (var it in EventTypeFilter.Items)
                {
                    if (it is ComboBoxItem cb && (cb.Content?.ToString() ?? "") == "BARCODE")
                    {
                        EventTypeFilter.SelectedItem = cb;
                        break;
                    }
                }
            }
            UpdateLogDisplay();
        }

        private void QuickDb_Click(object sender, RoutedEventArgs e)
        {
            _selectedEventType = "DB";
            if (EventTypeFilter != null)
            {
                foreach (var it in EventTypeFilter.Items)
                {
                    if (it is ComboBoxItem cb && (cb.Content?.ToString() ?? "") == "DB")
                    {
                        EventTypeFilter.SelectedItem = cb;
                        break;
                    }
                }
            }
            UpdateLogDisplay();
        }

        private void QuickImport_Click(object sender, RoutedEventArgs e)
        {
            _selectedEventType = "IMPORT";
            if (EventTypeFilter != null)
            {
                foreach (var it in EventTypeFilter.Items)
                {
                    if (it is ComboBoxItem cb && (cb.Content?.ToString() ?? "") == "IMPORT")
                    {
                        EventTypeFilter.SelectedItem = cb; break;
                    }
                }
            }
            UpdateLogDisplay();
            StatusText.Text = "IMPORT olayları gösteriliyor";
        }

        private void QuickExport_Click(object sender, RoutedEventArgs e)
        {
            _selectedEventType = "EXPORT";
            if (EventTypeFilter != null)
            {
                foreach (var it in EventTypeFilter.Items)
                {
                    if (it is ComboBoxItem cb && (cb.Content?.ToString() ?? "") == "EXPORT")
                    {
                        EventTypeFilter.SelectedItem = cb; break;
                    }
                }
            }
            UpdateLogDisplay();
            StatusText.Text = "EXPORT olayları gösteriliyor";
        }

        private void QuickProduct_Click(object sender, RoutedEventArgs e)
        {
            _selectedEventType = "PRODUCT";
            if (EventTypeFilter != null)
            {
                foreach (var it in EventTypeFilter.Items)
                {
                    if (it is ComboBoxItem cb && (cb.Content?.ToString() ?? "") == "PRODUCT")
                    { EventTypeFilter.SelectedItem = cb; break; }
                }
            }
            UpdateLogDisplay();
            StatusText.Text = "PRODUCT olayları gösteriliyor";
        }
        private void QuickPrice_Click(object sender, RoutedEventArgs e)
        {
            _selectedEventType = "PRICE";
            if (EventTypeFilter != null)
            {
                foreach (var it in EventTypeFilter.Items)
                {
                    if (it is ComboBoxItem cb && (cb.Content?.ToString() ?? "") == "PRICE")
                    { EventTypeFilter.SelectedItem = cb; break; }
                }
            }
            UpdateLogDisplay();
            StatusText.Text = "PRICE olayları gösteriliyor";
        }
        private void QuickStock_Click(object sender, RoutedEventArgs e)
        {
            _selectedEventType = "STOCK";
            if (EventTypeFilter != null)
            {
                foreach (var it in EventTypeFilter.Items)
                {
                    if (it is ComboBoxItem cb && (cb.Content?.ToString() ?? "") == "STOCK")
                    { EventTypeFilter.SelectedItem = cb; break; }
                }
            }
            UpdateLogDisplay();
            StatusText.Text = "STOCK olayları gösteriliyor";
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            _selectedLevel = "Tümü";
            _selectedEventType = "Tümü";
            _displayLimit = 200;
            _searchText = "";
            _correlationFilter = "";
            if (LogLevelFilter?.Items.Count > 0)
                LogLevelFilter.SelectedIndex = 0;
            if (EventTypeFilter?.Items.Count > 0)
                EventTypeFilter.SelectedIndex = 0;
            if (DisplayLimitCombo?.Items.Count > 0)
            {
                foreach (var it in DisplayLimitCombo.Items)
                {
                    if (it is ComboBoxItem cb && (cb.Content?.ToString() ?? "") == "200")
                    { DisplayLimitCombo.SelectedItem = cb; break; }
                }
            }
            SearchBox.Text = "Arama...";
            CorrelationFilter.Text = "";
            UpdateLogDisplay();
            StatusText.Text = "Filtreler sıfırlandı";
        }

        private void QuickErrors_Click(object sender, RoutedEventArgs e)
        {
            _selectedLevel = "🔴 Hata";
            if (LogLevelFilter?.Items.Count > 0)
            {
                foreach (var it in LogLevelFilter.Items)
                {
                    if (it is ComboBoxItem cb && (cb.Content?.ToString() ?? "") == "🔴 Hata")
                    { LogLevelFilter.SelectedItem = cb; break; }
                }
            }
            UpdateLogDisplay();
            StatusText.Text = "Sadece hatalar gösteriliyor";
        }

        private void OpenLogsFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = dir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                AddLog("🔴", $"Klasör açılamadı: {ex.Message}", "LogView", Colors.Red);
            }
        }

        private async void DbLoad_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var countStr = (DbCountCombo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "10";
                int take = int.TryParse(countStr, out var n) ? n : 10;
                DateTime? start = DbStartDate.SelectedDate;
                DateTime? end = DbEndDate.SelectedDate;
                string barcodeLike = DbBarcodeFilter.Text?.Trim() ?? string.Empty;

                var sp = MesTechStok.Desktop.App.ServiceProvider;
                if (sp == null) { StatusText.Text = "DB servisine erişilemiyor"; return; }
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetService<MesTechStok.Core.Data.AppDbContext>();
                if (db == null) { StatusText.Text = "AppDbContext yok"; return; }

                // Bağlantı kontrolü (kullanıcı tarafında SQL Server olmayabilir)
                try
                {
                    if (!await db.Database.CanConnectAsync())
                    {
                        StatusText.Text = "DB bağlantısı sağlanamadı (SQL Server ulaşılamıyor)";
                        AddLog("🔴 Hata", "[DB] Bağlantı kurulamadı. Lütfen SQL Server hizmetini ve bağlantı dizesini kontrol edin.", "LogView", Colors.Red);
                        return;
                    }
                }
                catch (Exception exConn)
                {
                    StatusText.Text = "DB bağlantısı hata verdi";
                    AddLog("🔴 Hata", $"[DB] Bağlantı kontrol hatası: {exConn.Message}", "LogView", Colors.Red);
                    return;
                }

                var q = db.BarcodeScanLogs.AsNoTracking().OrderByDescending(x => x.TimestampUtc).AsQueryable();
                if (start.HasValue)
                {
                    var s = start.Value.Date;
                    q = q.Where(x => x.TimestampUtc >= s.ToUniversalTime());
                }
                if (end.HasValue)
                {
                    var e2 = end.Value.Date.AddDays(1).AddTicks(-1);
                    q = q.Where(x => x.TimestampUtc <= e2.ToUniversalTime());
                }
                if (!string.IsNullOrWhiteSpace(barcodeLike))
                {
                    q = q.Where(x => x.Barcode.Contains(barcodeLike));
                }

                List<MesTechStok.Core.Data.Models.BarcodeScanLog> rows;
                try
                {
                    rows = await q.Take(take).ToListAsync();
                }
                catch (Exception exQuery)
                {
                    StatusText.Text = "DB sorgusu başarısız";
                    AddLog("🔴 Hata", $"[DB] Sorgu hatası: {exQuery.Message}", "LogView", Colors.Red);
                    return;
                }
                DbLogGrid.ItemsSource = rows.Select(x => new
                {
                    TimestampLocal = x.TimestampUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    x.Barcode,
                    x.Format,
                    x.Source,
                    x.DeviceId,
                    x.IsValid,
                    x.ValidationMessage,
                    x.RawLength,
                    x.CorrelationId
                }).ToList();

                StatusText.Text = $"DB'den {rows.Count} kayıt yüklendi";
            }
            catch (Exception ex)
            {
                AddLog("🔴 Hata", $"DB yükleme hatası: {ex.Message}", "LogView", Colors.Red);
            }
        }

        private void DbExport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DbLogGrid.ItemsSource == null)
                {
                    StatusText.Text = "Önce DB loglarını yükleyin";
                    return;
                }
                var save = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = "csv",
                    FileName = $"Barcode_DB_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };
                if (save.ShowDialog() == true)
                {
                    var list = DbLogGrid.ItemsSource;
                    using var sw = new System.IO.StreamWriter(save.FileName, false, System.Text.Encoding.UTF8);
                    sw.WriteLine("Timestamp,Barcode,Format,Source,DeviceId,IsValid,ValidationMessage,RawLength,CorrelationId");
                    foreach (var it in (IEnumerable<object>)list)
                    {
                        var t = it.GetType();
                        string Get(string n) => t.GetProperty(n)?.GetValue(it)?.ToString() ?? string.Empty;
                        var row = new[]
                        {
                            Get("TimestampLocal"), Get("Barcode"), Get("Format"), Get("Source"), Get("DeviceId"),
                            Get("IsValid"), Get("ValidationMessage"), Get("RawLength"), Get("CorrelationId")
                        };
                        sw.WriteLine(string.Join(",", row.Select(v => "\"" + v.Replace("\"", "\"\"") + "\"")));
                    }
                    StatusText.Text = $"DB CSV kaydedildi: {System.IO.Path.GetFileName(save.FileName)}";
                }
            }
            catch (Exception ex)
            {
                AddLog("🔴 Hata", $"DB CSV export hatası: {ex.Message}", "LogView", Colors.Red);
            }
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Arama...")
            {
                SearchBox.Text = "";
                SearchBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Arama...";
                SearchBox.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        public void Dispose()
        {
            _refreshTimer?.Stop();
            MesTechStok.Desktop.Utils.GlobalLogger.Instance.LogAdded -= OnLogAdded;
        }
    }

    // XAML için basit renk dönüştürücüler
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color c) return new SolidColorBrush(c);
            return new SolidColorBrush(Colors.Transparent);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ColorToLightBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color c)
            {
                byte a = 30;
                return new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B));
            }
            return new SolidColorBrush(Color.FromArgb(10, 0, 0, 0));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = "";
        public string Message { get; set; } = "";
        public string Source { get; set; } = "";
        public string EventType { get; set; } = "";
        public Color Color { get; set; }
        // XAML'de converter bağımlılığı olmadan bağlanabilen Brush property'leri
        public SolidColorBrush BorderBrush => new SolidColorBrush(Color);
        public SolidColorBrush LightBackground => new SolidColorBrush(Color.FromArgb(30, Color.R, Color.G, Color.B));
    }
}