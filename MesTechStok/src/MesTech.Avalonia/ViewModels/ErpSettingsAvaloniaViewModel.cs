using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ERP Entegrasyon Ayarlari ViewModel.
/// Provider secimi, dinamik form alanlari, baglanti testi, sync ayarlari, sync gecmisi.
/// Desteklenen provider'lar: Yok, Parasut, Logo, Netsis, Nebim, BizimHesap.
/// </summary>
public partial class ErpSettingsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


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

    public ErpSettingsAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
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
            await Task.Delay(100); // Will be replaced with MediatR query

            // Demo sync history
            SyncHistory.Clear();
            SyncHistory.Add(new ErpSyncHistoryItem
            {
                SyncDate = new DateTime(2026, 3, 19, 14, 30, 0),
                SyncType = "Stok",
                RecordCount = 245,
                Status = "Basarili",
                StatusColor = "#22C55E",
                Duration = "12s"
            });
            SyncHistory.Add(new ErpSyncHistoryItem
            {
                SyncDate = new DateTime(2026, 3, 19, 13, 0, 0),
                SyncType = "Fatura",
                RecordCount = 18,
                Status = "Basarili",
                StatusColor = "#22C55E",
                Duration = "3s"
            });
            SyncHistory.Add(new ErpSyncHistoryItem
            {
                SyncDate = new DateTime(2026, 3, 19, 12, 0, 0),
                SyncType = "Stok",
                RecordCount = 0,
                Status = "Hata: Baglanti zaman asimi",
                StatusColor = "#EF4444",
                Duration = "30s"
            });
            SyncHistory.Add(new ErpSyncHistoryItem
            {
                SyncDate = new DateTime(2026, 3, 18, 16, 45, 0),
                SyncType = "Fiyat",
                RecordCount = 312,
                Status = "Basarili",
                StatusColor = "#22C55E",
                Duration = "8s"
            });
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
            await Task.Delay(200); // Will be replaced with MediatR command
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
            await Task.Delay(1000); // Will be replaced with real connection test
            IsConnected = true;
            LastTestResult = "Baglanti basarili";
            ConnectionStatusColor = "#22C55E";
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
