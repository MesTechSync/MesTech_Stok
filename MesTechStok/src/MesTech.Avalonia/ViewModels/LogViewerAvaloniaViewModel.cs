using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Log İzleme ViewModel — WPF010.
/// Seq API entegrasyonu hazır; şimdilik mock veri ile çalışır.
/// </summary>
public partial class LogViewerAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private string selectedLevel = "Tumu";
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool autoRefresh;

    public ObservableCollection<string> Levels { get; } = new()
    {
        "Tumu",
        "Error",
        "Warning",
        "Information",
        "Debug"
    };

    public ObservableCollection<LogEntryItem> LogEntries { get; } = new();

    private static readonly LogEntryItem[] _mockEntries =
    [
        new(DateTime.Now.AddMinutes(-1).ToString("dd.MM.yyyy HH:mm:ss"),  "Error",       "MesTech.Api",              "Trendyol siparis cekme basarisiz: 429 Too Many Requests",           "#EF4444"),
        new(DateTime.Now.AddMinutes(-3).ToString("dd.MM.yyyy HH:mm:ss"),  "Warning",      "StockAvaloniaViewModel",   "Stok seviyesi kritik esige ulasti: SKU-00421",                       "#F59E0B"),
        new(DateTime.Now.AddMinutes(-5).ToString("dd.MM.yyyy HH:mm:ss"),  "Information",  "Hangfire.Server",          "Job tamamlandi: SyncTrendyolOrders [elapsed: 4.2s]",                 "#3B82F6"),
        new(DateTime.Now.AddMinutes(-7).ToString("dd.MM.yyyy HH:mm:ss"),  "Debug",        "EF Core",                  "Executing DbCommand [Parameters=[@p0='42']] SELECT * FROM Products", "#64748B"),
        new(DateTime.Now.AddMinutes(-10).ToString("dd.MM.yyyy HH:mm:ss"), "Information",  "LoginAvaloniaViewModel",   "Kullanici giris yapti: admin@mestech.com",                           "#3B82F6"),
        new(DateTime.Now.AddMinutes(-12).ToString("dd.MM.yyyy HH:mm:ss"), "Warning",      "RabbitMQ.Consumer",        "Mesaj isleme suresi asimi: OrderCreated [12.3s > 10s limit]",        "#F59E0B"),
        new(DateTime.Now.AddMinutes(-15).ToString("dd.MM.yyyy HH:mm:ss"), "Error",        "InvoiceProvider.Sovos",    "e-Fatura gonderme hatasi: UBL-TR dogrulama basarisiz",               "#EF4444"),
        new(DateTime.Now.AddMinutes(-18).ToString("dd.MM.yyyy HH:mm:ss"), "Information",  "BackupAvaloniaViewModel",  "Otomatik yedekleme baslatildi",                                      "#3B82F6"),
        new(DateTime.Now.AddMinutes(-20).ToString("dd.MM.yyyy HH:mm:ss"), "Debug",        "MassTransit",              "Sending message OrderShipped to exchange mestech.orders",            "#64748B"),
        new(DateTime.Now.AddMinutes(-25).ToString("dd.MM.yyyy HH:mm:ss"), "Information",  "N11Adapter",               "Urun fiyat guncelleme senkronizasyonu tamamlandi [47 urun]",         "#3B82F6"),
        new(DateTime.Now.AddMinutes(-30).ToString("dd.MM.yyyy HH:mm:ss"), "Warning",      "PostgreSQL",               "Slow query detected: 3.8s — consider adding index on Orders.Date",  "#F59E0B"),
        new(DateTime.Now.AddMinutes(-35).ToString("dd.MM.yyyy HH:mm:ss"), "Information",  "MinIO.Storage",            "Dosya yuklendi: invoice_2026_03_26.pdf [124 KB]",                   "#3B82F6"),
    ];

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            // DEV3-DEPENDENCY: Seq API entegrasyonu hazır olunca gerçek veriye geçecek
            // GET http://localhost:3343/api/events?level=SelectedLevel&filter=SearchText
            await Task.Delay(300, CancellationToken); // simulate network
            ApplyFilter();
        }
        catch (OperationCanceledException)
        {
            // View kapandı
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Log kayıtları yüklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilter()
    {
        LogEntries.Clear();
        foreach (var entry in _mockEntries)
        {
            bool levelMatch = SelectedLevel == "Tumu" || entry.Level == SelectedLevel;
            bool searchMatch = string.IsNullOrWhiteSpace(SearchText)
                || entry.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || entry.Source.Contains(SearchText, StringComparison.OrdinalIgnoreCase);

            if (levelMatch && searchMatch)
                LogEntries.Add(entry);
        }

        IsEmpty = LogEntries.Count == 0;
    }

    partial void OnSelectedLevelChanged(string value) => ApplyFilter();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

/// <summary>Log satır modeli.</summary>
public sealed class LogEntryItem
{
    public string Timestamp { get; }
    public string Level { get; }
    public string Source { get; }
    public string Message { get; }
    /// <summary>Hex renk kodu: Error=#EF4444, Warning=#F59E0B, Information=#3B82F6, Debug=#64748B</summary>
    public string LevelColor { get; }

    public LogEntryItem(string timestamp, string level, string source, string message, string levelColor)
    {
        Timestamp = timestamp;
        Level = level;
        Source = source;
        Message = message;
        LevelColor = levelColor;
    }
}
