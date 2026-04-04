using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Domain.Enums;

namespace MesTech.Avalonia.Controls;

public partial class CargoProviderCardViewModel : ObservableObject
{
    private static Color Token(string key) =>
        global::Avalonia.Application.Current?.FindResource(key) is Color c ? c : Colors.Gray;

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
        CargoProvider.YurticiKargo => new SolidColorBrush(Token("MesBrandYurtici")),
        CargoProvider.ArasKargo => new SolidColorBrush(Token("MesBrandAras")),
        CargoProvider.SuratKargo => new SolidColorBrush(Token("MesBrandSurat")),
        CargoProvider.MngKargo => new SolidColorBrush(Token("MesBrandMng")),
        CargoProvider.PttKargo => new SolidColorBrush(Token("MesBrandPttKargo")),
        CargoProvider.Hepsijet => new SolidColorBrush(Token("MesBrandHepsijet")),
        CargoProvider.Sendeo => new SolidColorBrush(Token("MesBrandSendeo")),
        _ => new SolidColorBrush(Token("MesNeutralGray"))
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
        ? new SolidColorBrush(Token("MesConnectedGreen"))
        : new SolidColorBrush(Token("MesDisconnectedRed"));

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
