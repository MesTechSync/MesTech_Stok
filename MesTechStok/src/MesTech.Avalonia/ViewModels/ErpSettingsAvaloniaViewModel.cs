using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Settings.Commands.SaveErpSettings;
using MesTech.Application.Features.Settings.Commands.TestErpConnection;
using MesTech.Application.Features.Settings.Queries.GetErpSettings;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ERP Entegrasyon Ayarlari ViewModel.
/// Provider secimi, dinamik form alanlari, baglanti testi, sync ayarlari, sync gecmisi.
/// Desteklenen provider'lar: Yok, Parasut, Logo, Netsis, Nebim, BizimHesap.
/// </summary>
public partial class ErpSettingsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string _statusMessage = string.Empty;

    // Provider selection
    [ObservableProperty] private string selectedErpProvider = "Yok";

    // Logo fields
    [ObservableProperty] private string logoBaseUrl = string.Empty;
    [ObservableProperty] private string logoUsername = string.Empty;
    [ObservableProperty] private string logoPassword = string.Empty;
    [ObservableProperty] private string logoFirmNumber = string.Empty;
    [ObservableProperty] private string logoPeriodNumber = string.Empty;

    // Netsis fields
    [ObservableProperty] private string netsisBaseUrl = string.Empty;
    [ObservableProperty] private string netsisUsername = string.Empty;
    [ObservableProperty] private string netsisPassword = string.Empty;
    [ObservableProperty] private string netsisCompanyCode = string.Empty;
    [ObservableProperty] private string netsisBranchCode = string.Empty;

    // Nebim fields
    [ObservableProperty] private string nebimBaseUrl = string.Empty;
    [ObservableProperty] private string nebimApiKey = string.Empty;
    [ObservableProperty] private string nebimDatabaseCode = string.Empty;
    [ObservableProperty] private string nebimOfficeCode = string.Empty;

    // Parasut fields
    [ObservableProperty] private string parasutClientId = string.Empty;
    [ObservableProperty] private string parasutClientSecret = string.Empty;
    [ObservableProperty] private string parasutCompanyId = string.Empty;
    [ObservableProperty] private bool parasutSandbox = true;

    // Connection status
    [ObservableProperty] private bool isConnected;
    [ObservableProperty] private string lastTestResult = "Test edilmedi";
    [ObservableProperty] private string connectionStatusColor = "#94A3B8";

    // Sync settings
    [ObservableProperty] private bool autoSyncStock = true;
    [ObservableProperty] private bool autoSyncInvoice = true;
    [ObservableProperty] private int stockSyncPeriodMinutes = 30;
    [ObservableProperty] private int priceSyncPeriodMinutes = 60;

    // Computed visibility
    public bool IsLogoSelected => SelectedErpProvider == "Logo";
    public bool IsNetsisSelected => SelectedErpProvider == "Netsis";
    public bool IsNebimSelected => SelectedErpProvider == "Nebim";
    public bool IsParasutSelected => SelectedErpProvider == "Parasut";
    public bool IsProviderSelected => SelectedErpProvider != "Yok";

    public ObservableCollection<string> ErpProviders { get; } =
    [
        "Yok",
        "Parasut",
        "Logo",
        "Netsis",
        "Nebim",
        "BizimHesap"
    ];

    public ObservableCollection<ErpSyncHistoryItem> SyncHistory { get; } = [];

    private readonly ICurrentUserService _currentUser;

    public ErpSettingsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    partial void OnSelectedErpProviderChanged(string value)
    {
        OnPropertyChanged(nameof(IsLogoSelected));
        OnPropertyChanged(nameof(IsNetsisSelected));
        OnPropertyChanged(nameof(IsNebimSelected));
        OnPropertyChanged(nameof(IsParasutSelected));
        OnPropertyChanged(nameof(IsProviderSelected));
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var settings = await _mediator.Send(new GetErpSettingsQuery(_currentUser.TenantId));

            SelectedErpProvider = settings.ActiveProvider.ToString();
            IsConnected = settings.IsConnected;
            AutoSyncStock = settings.AutoSyncStock;
            AutoSyncInvoice = settings.AutoSyncInvoice;
            StockSyncPeriodMinutes = settings.StockSyncPeriodMinutes;
            PriceSyncPeriodMinutes = settings.PriceSyncPeriodMinutes;
            LastTestResult = settings.IsConnected ? "Bagli" : "Bagli degil";
            ConnectionStatusColor = settings.IsConnected ? "#22C55E" : "#94A3B8";

            SyncHistory.Clear();
            foreach (var h in settings.RecentSyncHistory)
            {
                var isError = h.Status.StartsWith("Hata", StringComparison.OrdinalIgnoreCase);
                SyncHistory.Add(new ErpSyncHistoryItem
                {
                    SyncDate = h.SyncDate,
                    SyncType = h.SyncType,
                    RecordCount = h.RecordCount,
                    Status = h.Status,
                    StatusColor = isError ? "#EF4444" : "#22C55E",
                    Duration = h.Duration
                });
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"ERP ayarlari yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task SaveSettings()
    {
        IsLoading = true;
        try
        {
            var provider = Enum.TryParse<ErpProvider>(SelectedErpProvider, out var p) ? p : ErpProvider.None;
            await _mediator.Send(new SaveErpSettingsCommand(
                _currentUser.TenantId,
                provider,
                AutoSyncStock,
                AutoSyncInvoice,
                StockSyncPeriodMinutes,
                PriceSyncPeriodMinutes));
            StatusMessage = "ERP ayarlari kaydedildi.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"ERP ayarlari kaydedilemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task TestConnection()
    {
        IsLoading = true;
        LastTestResult = "Test ediliyor...";
        ConnectionStatusColor = "#F59E0B";
        try
        {
            var provider = Enum.TryParse<ErpProvider>(SelectedErpProvider, out var p) ? p : ErpProvider.None;
            var result = await _mediator.Send(new TestErpConnectionCommand(_currentUser.TenantId, provider));
            IsConnected = result.IsSuccess;
            LastTestResult = result.Message;
            ConnectionStatusColor = result.IsSuccess ? "#22C55E" : "#EF4444";
        }
        catch (Exception ex)
        {
            IsConnected = false;
            LastTestResult = $"Baglanti hatasi: {ex.Message}";
            ConnectionStatusColor = "#EF4444";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

/// <summary>
/// Sync history list item for ERP settings view.
/// </summary>
public class ErpSyncHistoryItem : ObservableObject
{
    public DateTime SyncDate { get; set; }
    public string SyncType { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#94A3B8";
    public string Duration { get; set; } = string.Empty;
}
