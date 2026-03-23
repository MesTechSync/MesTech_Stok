using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels.Erp;

/// <summary>
/// ERP Dashboard ViewModel — E-01.
/// 5 ERP provider cards (Parasut, BizimHesap, Logo, Netsis, Nebim)
/// with connection status, last sync time, test + settings buttons.
/// Bottom section: recent sync log DataGrid.
/// </summary>
public partial class ErpDashboardViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int connectedCount;

    public ObservableCollection<ErpProviderCardItem> Providers { get; } = [];
    public ObservableCollection<ErpSyncLogItem> SyncLogs { get; } = [];

    public ErpDashboardViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR query

            Providers.Clear();
            Providers.Add(new ErpProviderCardItem
            {
                Name = "Parasut",
                IsConnected = true,
                StatusText = "Bagli",
                StatusBadgeBackground = new SolidColorBrush(Color.Parse("#22C55E")),
                LastSyncDisplay = "Son sync: 24.03.2026 10:30"
            });
            Providers.Add(new ErpProviderCardItem
            {
                Name = "BizimHesap",
                IsConnected = false,
                StatusText = "Bagli Degil",
                StatusBadgeBackground = new SolidColorBrush(Color.Parse("#94A3B8")),
                LastSyncDisplay = "Henuz senkronize edilmedi"
            });
            Providers.Add(new ErpProviderCardItem
            {
                Name = "Logo",
                IsConnected = true,
                StatusText = "Bagli",
                StatusBadgeBackground = new SolidColorBrush(Color.Parse("#22C55E")),
                LastSyncDisplay = "Son sync: 23.03.2026 16:45"
            });
            Providers.Add(new ErpProviderCardItem
            {
                Name = "Netsis",
                IsConnected = false,
                StatusText = "Bagli Degil",
                StatusBadgeBackground = new SolidColorBrush(Color.Parse("#94A3B8")),
                LastSyncDisplay = "Henuz senkronize edilmedi"
            });
            Providers.Add(new ErpProviderCardItem
            {
                Name = "Nebim",
                IsConnected = false,
                StatusText = "Bagli Degil",
                StatusBadgeBackground = new SolidColorBrush(Color.Parse("#94A3B8")),
                LastSyncDisplay = "Henuz senkronize edilmedi"
            });

            ConnectedCount = Providers.Count(p => p.IsConnected);

            // Demo sync logs
            SyncLogs.Clear();
            SyncLogs.Add(new ErpSyncLogItem
            {
                SyncTime = "24.03.2026 10:30",
                Provider = "Parasut",
                Direction = "MesTech -> ERP",
                RecordCount = 245,
                Status = "Basarili"
            });
            SyncLogs.Add(new ErpSyncLogItem
            {
                SyncTime = "24.03.2026 09:15",
                Provider = "Logo",
                Direction = "ERP -> MesTech",
                RecordCount = 128,
                Status = "Basarili"
            });
            SyncLogs.Add(new ErpSyncLogItem
            {
                SyncTime = "23.03.2026 16:45",
                Provider = "Logo",
                Direction = "MesTech -> ERP",
                RecordCount = 312,
                Status = "Basarili"
            });
            SyncLogs.Add(new ErpSyncLogItem
            {
                SyncTime = "23.03.2026 14:00",
                Provider = "Parasut",
                Direction = "MesTech -> ERP",
                RecordCount = 0,
                Status = "Hata: Zaman asimi"
            });
            SyncLogs.Add(new ErpSyncLogItem
            {
                SyncTime = "23.03.2026 12:30",
                Provider = "Parasut",
                Direction = "ERP -> MesTech",
                RecordCount = 56,
                Status = "Basarili"
            });

            IsEmpty = Providers.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"ERP verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task TestConnection(string? providerName)
    {
        if (string.IsNullOrEmpty(providerName)) return;

        IsLoading = true;
        try
        {
            await Task.Delay(1000); // Will be replaced with real TestConnectionAsync call

            var provider = Providers.FirstOrDefault(p => p.Name == providerName);
            if (provider != null)
            {
                provider.IsConnected = true;
                provider.StatusText = "Bagli";
                provider.StatusBadgeBackground = new SolidColorBrush(Color.Parse("#22C55E"));
                provider.LastSyncDisplay = $"Son test: {DateTime.Now:dd.MM.yyyy HH:mm}";
                ConnectedCount = Providers.Count(p => p.IsConnected);
            }
        }
        catch (Exception ex)
        {
            var provider = Providers.FirstOrDefault(p => p.Name == providerName);
            if (provider != null)
            {
                provider.IsConnected = false;
                provider.StatusText = "Hata";
                provider.StatusBadgeBackground = new SolidColorBrush(Color.Parse("#EF4444"));
            }
            ErrorMessage = $"Baglanti testi basarisiz: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task OpenSettings(string? providerName)
    {
        // Will navigate to ErpSettingsAvaloniaView with selected provider
        return Task.CompletedTask;
    }
}

/// <summary>
/// Provider card item for ERP dashboard.
/// </summary>
public partial class ErpProviderCardItem : ObservableObject
{
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private bool isConnected;
    [ObservableProperty] private string statusText = "Bagli Degil";
    [ObservableProperty] private IBrush statusBadgeBackground = new SolidColorBrush(Color.Parse("#94A3B8"));
    [ObservableProperty] private string lastSyncDisplay = "Henuz senkronize edilmedi";
}

/// <summary>
/// Sync log entry for ERP dashboard DataGrid.
/// </summary>
public class ErpSyncLogItem
{
    public string SyncTime { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public string Status { get; set; } = string.Empty;
}
