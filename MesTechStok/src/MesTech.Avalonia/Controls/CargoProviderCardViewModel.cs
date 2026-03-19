using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Domain.Enums;

namespace MesTech.Avalonia.Controls;

public partial class CargoProviderCardViewModel : ObservableObject
{
    [ObservableProperty] private CargoProvider provider;
    [ObservableProperty] private bool isConnected;
    [ObservableProperty] private string lastShipmentText = "—";
    [ObservableProperty] private string todayStats = "0 gönderim";
    [ObservableProperty] private double avgDeliveryDays;

    public string ProviderName => Provider switch
    {
        CargoProvider.YurticiKargo => "Yurtiçi Kargo",
        CargoProvider.ArasKargo => "Aras Kargo",
        CargoProvider.SuratKargo => "Sürat Kargo",
        CargoProvider.MngKargo => "MNG Kargo",
        CargoProvider.PttKargo => "PTT Kargo",
        CargoProvider.Hepsijet => "HepsiJet",
        CargoProvider.Sendeo => "Sendeo",
        _ => Provider.ToString()
    };

    public ISolidColorBrush ProviderColor => Provider switch
    {
        CargoProvider.YurticiKargo => new SolidColorBrush(Color.Parse("#E31E24")),
        CargoProvider.ArasKargo => new SolidColorBrush(Color.Parse("#00A651")),
        CargoProvider.SuratKargo => new SolidColorBrush(Color.Parse("#ED1C24")),
        CargoProvider.MngKargo => new SolidColorBrush(Color.Parse("#E30613")),
        CargoProvider.PttKargo => new SolidColorBrush(Color.Parse("#FFC107")),
        CargoProvider.Hepsijet => new SolidColorBrush(Color.Parse("#FF6000")),
        CargoProvider.Sendeo => new SolidColorBrush(Color.Parse("#00BCD4")),
        _ => new SolidColorBrush(Color.Parse("#9E9E9E"))
    };

    public string ApiTypeText => Provider switch
    {
        CargoProvider.YurticiKargo => "SOAP XML",
        CargoProvider.ArasKargo => "SOAP XML",
        CargoProvider.SuratKargo => "REST API",
        CargoProvider.MngKargo => "REST API",
        CargoProvider.PttKargo => "SOAP/REST",
        CargoProvider.Hepsijet => "REST API",
        CargoProvider.Sendeo => "REST API",
        _ => "API"
    };

    public ISolidColorBrush ConnectionStatusColor => IsConnected
        ? new SolidColorBrush(Color.Parse("#4CAF50"))
        : new SolidColorBrush(Color.Parse("#F44336"));

    public string ConnectionStatusTooltip => IsConnected ? "Bağlantı aktif" : "Bağlantı kopuk";

    [RelayCommand]
    private void CreateShipment()
    {
        // Will be wired to navigation by parent view
    }

    [RelayCommand]
    private void Track()
    {
        // Will be wired to cargo tracking by parent view
    }
}
