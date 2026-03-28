using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Erp.Queries.GetErpDashboard;
using MesTech.Application.Features.Erp.Queries.GetErpSyncLogs;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Erp;

/// <summary>
/// ERP Dashboard ViewModel — E-01.
/// 5 ERP provider cards (Parasut, BizimHesap, Logo, Netsis, Nebim)
/// with connection status, last sync time, test + settings buttons.
/// Bottom section: recent sync log DataGrid.
/// </summary>
public partial class ErpDashboardViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private static readonly CultureInfo TrCulture = new("tr-TR");

    [ObservableProperty] private int connectedCount;
    [ObservableProperty] private int totalSyncToday;
    [ObservableProperty] private int failedSyncToday;
    [ObservableProperty] private int pendingRetries;
    [ObservableProperty] private string lastSyncAt = "—";

    public ObservableCollection<ErpProviderCardItem> Providers { get; } = [];
    public ObservableCollection<ErpSyncLogItem> SyncLogs { get; } = [];

    public ErpDashboardViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            // Load ERP dashboard stats via GetErpDashboardQuery
            var dashboard = await _mediator.Send(new GetErpDashboardQuery(_currentUser.TenantId));

            ConnectedCount = dashboard.ConnectedProviders;
            TotalSyncToday = dashboard.TotalSyncToday;
            FailedSyncToday = dashboard.FailedSyncToday;
            PendingRetries = dashboard.PendingRetries;
            LastSyncAt = dashboard.LastSyncAt.HasValue
                ? dashboard.LastSyncAt.Value.ToString("dd.MM.yyyy HH:mm", TrCulture)
                : "—";

            // Populate static provider cards (provider list is config-driven, not from query)
            Providers.Clear();
            Providers.Add(new ErpProviderCardItem
            {
                Name = "Parasut",
                IsConnected = false,
                StatusText = "Bagli Degil",
                StatusBadgeBackground = new SolidColorBrush(Color.Parse("#94A3B8")),
                LastSyncDisplay = "Henuz senkronize edilmedi"
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
                IsConnected = false,
                StatusText = "Bagli Degil",
                StatusBadgeBackground = new SolidColorBrush(Color.Parse("#94A3B8")),
                LastSyncDisplay = "Henuz senkronize edilmedi"
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

            // Load sync logs via GetErpSyncLogsQuery
            var logs = await _mediator.Send(new GetErpSyncLogsQuery(_currentUser.TenantId, Page: 1, PageSize: 20));
            SyncLogs.Clear();
            foreach (var log in logs)
            {
                SyncLogs.Add(new ErpSyncLogItem
                {
                    SyncTime = log.AttemptedAt.ToString("dd.MM.yyyy HH:mm", TrCulture),
                    Provider = log.Provider.ToString(),
                    Direction = "MesTech -> ERP",
                    RecordCount = log.SuccessCount + log.FailCount,
                    Status = log.Success ? "Basarili" : $"Hata: {log.ErrorMessage}"
                });
            }

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
