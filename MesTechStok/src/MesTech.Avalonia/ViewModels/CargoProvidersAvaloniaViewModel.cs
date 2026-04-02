using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Avalonia.Controls;
using MesTech.Domain.Enums;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Cargo Providers screen — I-05 Siparis/Kargo Celiklestirme.
/// Displays all cargo provider cards with connection status and stats.
/// </summary>
public partial class CargoProvidersAvaloniaViewModel : ViewModelBase
{
    [ObservableProperty] private int totalProviders;
    [ObservableProperty] private int connectedProviders;

    public ObservableCollection<CargoProviderCardViewModel> Providers { get; } = [];


    public override Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        try
        {

            Providers.Clear();
            Providers.Add(new() { Provider = CargoProvider.YurticiKargo, IsConnected = true, LastShipmentText = "2 saat once", TodayStats = "24 gonderim", AvgDeliveryDays = 2.1 });
            Providers.Add(new() { Provider = CargoProvider.ArasKargo, IsConnected = true, LastShipmentText = "45 dk once", TodayStats = "18 gonderim", AvgDeliveryDays = 2.4 });
            Providers.Add(new() { Provider = CargoProvider.SuratKargo, IsConnected = true, LastShipmentText = "1 saat once", TodayStats = "12 gonderim", AvgDeliveryDays = 1.8 });
            Providers.Add(new() { Provider = CargoProvider.MngKargo, IsConnected = false, LastShipmentText = "3 gun once", TodayStats = "0 gonderim", AvgDeliveryDays = 2.7 });
            Providers.Add(new() { Provider = CargoProvider.PttKargo, IsConnected = true, LastShipmentText = "5 saat once", TodayStats = "8 gonderim", AvgDeliveryDays = 3.2 });
            Providers.Add(new() { Provider = CargoProvider.Hepsijet, IsConnected = true, LastShipmentText = "30 dk once", TodayStats = "31 gonderim", AvgDeliveryDays = 1.5 });
            Providers.Add(new() { Provider = CargoProvider.Sendeo, IsConnected = false, LastShipmentText = "1 hafta once", TodayStats = "0 gonderim", AvgDeliveryDays = 2.9 });

            TotalProviders = Providers.Count;
            ConnectedProviders = Providers.Count(p => p.IsConnected);
            IsEmpty = Providers.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kargo firmalari yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();
}
