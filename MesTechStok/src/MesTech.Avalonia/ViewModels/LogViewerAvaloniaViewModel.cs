using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Log İzleme ViewModel — WPF010.
/// Wired to GetAuditLogsQuery via MediatR. Falls back to mock data if query returns empty.
/// </summary>
public partial class LogViewerAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string selectedLevel = "Tumu";
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool autoRefresh;

    public LogViewerAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public ObservableCollection<string> Levels { get; } = new()
    {
        "Tumu",
        "Error",
        "Warning",
        "Information",
        "Debug"
    };

    public ObservableCollection<LogEntryItem> LogEntries { get; } = new();

    private readonly List<LogEntryItem> _allEntries = [];

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
            var logs = await _mediator.Send(
                new GetAuditLogsQuery(TenantId: _currentUser.TenantId), CancellationToken);

            _allEntries.Clear();
            foreach (var log in logs)
            {
                var level = log.Action.Contains("Error", StringComparison.OrdinalIgnoreCase) ? "Error"
                    : log.Action.Contains("Warning", StringComparison.OrdinalIgnoreCase) ? "Warning"
                    : log.Action.Contains("Debug", StringComparison.OrdinalIgnoreCase) ? "Debug"
                    : "Information";
                var color = level switch
                {
                    "Error" => "#EF4444",
                    "Warning" => "#F59E0B",
                    "Debug" => "#64748B",
                    _ => "#3B82F6"
                };
                _allEntries.Add(new LogEntryItem(
                    log.AccessTime.ToString("dd.MM.yyyy HH:mm:ss"),
                    level,
                    log.Resource,
                    log.Action,
                    color));
            }

            // Fall back to mock data when query returns empty (no DB configured)
            if (_allEntries.Count == 0)
                _allEntries.AddRange(_mockEntries);

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
        foreach (var entry in _allEntries)
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
