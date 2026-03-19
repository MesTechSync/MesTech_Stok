using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class ErpSettingsAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    // ERP Settings
    [ObservableProperty] private string selectedErpProvider = "Parasut";
    [ObservableProperty] private string serverAddress = string.Empty;
    [ObservableProperty] private string databaseName = string.Empty;
    [ObservableProperty] private string erpUsername = string.Empty;
    [ObservableProperty] private string erpPassword = string.Empty;
    [ObservableProperty] private bool autoSyncStock = true;
    [ObservableProperty] private bool autoSyncInvoice = true;
    [ObservableProperty] private int syncIntervalMinutes = 30;
    [ObservableProperty] private string connectionStatusText = "Baglanti test edilmedi";
    [ObservableProperty] private string connectionStatusColor = "#94A3B8";

    public ObservableCollection<string> ErpProviders { get; } =
    [
        "Parasut",
        "Logo Tiger",
        "Logo Go3",
        "Netsis",
        "Mikro",
        "Eta",
        "DIA",
        "Nebim",
        "Wolvox",
        "Uyumsoft"
    ];

    public ErpSettingsAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(100); // Will be replaced with MediatR query
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
    private async Task Save()
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
        ConnectionStatusText = "Test ediliyor...";
        ConnectionStatusColor = "#F59E0B";
        try
        {
            await Task.Delay(1000); // Will be replaced with real connection test
            ConnectionStatusText = "Baglanti basarili";
            ConnectionStatusColor = "#22C55E";
        }
        catch (Exception ex)
        {
            ConnectionStatusText = $"Baglanti hatasi: {ex.Message}";
            ConnectionStatusColor = "#EF4444";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
