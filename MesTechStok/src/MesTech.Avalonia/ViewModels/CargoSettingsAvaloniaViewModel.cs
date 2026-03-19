using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class CargoSettingsAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    // Settings fields
    [ObservableProperty] private string selectedCargoProvider = "Yurtici Kargo";
    [ObservableProperty] private int desiMultiplier = 3000;
    [ObservableProperty] private decimal minDesi = 0.5m;
    [ObservableProperty] private decimal maxDesi = 30m;
    [ObservableProperty] private bool autoCreateShipment = true;
    [ObservableProperty] private bool autoUpdateTracking = true;
    [ObservableProperty] private string apiKey = string.Empty;
    [ObservableProperty] private string apiSecret = string.Empty;

    public ObservableCollection<string> CargoProviders { get; } =
    [
        "Yurtici Kargo",
        "Aras Kargo",
        "Surat Kargo",
        "MNG Kargo",
        "PTT Kargo",
        "Sendeo",
        "Kolay Gelsin",
        "Hepsijet",
        "UPS"
    ];

    public CargoSettingsAvaloniaViewModel(IMediator mediator)
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
            // Settings loaded from persistence
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kargo ayarlari yuklenemedi: {ex.Message}";
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
            // Settings saved
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kargo ayarlari kaydedilemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
