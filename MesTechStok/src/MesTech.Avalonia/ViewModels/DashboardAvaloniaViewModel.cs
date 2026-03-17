using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Avalonia Dashboard — 4 KPI cards (Toplam Satis, Siparis, Stok Uyari, Gunluk Gelir)
/// + 10 recent orders DataGrid.
/// Mirrors DashboardView.xaml.cs logic but as a proper ViewModel (no code-behind).
/// </summary>
public partial class DashboardAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    // KPI cards
    [ObservableProperty] private string totalProducts = "0";
    [ObservableProperty] private string activeOrders = "0";
    [ObservableProperty] private string todayRevenue = "0 TL";
    [ObservableProperty] private string stockAlerts = "0";

    // L/E/E states
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string lastUpdated = "--:--";

    public ObservableCollection<RecentOrderDto> RecentOrders { get; } = [];

    public DashboardAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(300); // Simulate async load

            // KPI demo values
            TotalProducts = "1,247";
            ActiveOrders = "38";
            TodayRevenue = "24,580 TL";
            StockAlerts = "7";
            LastUpdated = DateTime.Now.ToString("HH:mm:ss");

            // Recent orders demo data (Turkish, realistic)
            RecentOrders.Clear();
            RecentOrders.Add(new RecentOrderDto { OrderNo = "SIP-2026-0041", Date = "17.03.2026", Customer = "Ahmet Yilmaz", Amount = "2,450.00 TL", Status = "Yeni" });
            RecentOrders.Add(new RecentOrderDto { OrderNo = "SIP-2026-0040", Date = "17.03.2026", Customer = "Fatma Demir", Amount = "1,890.50 TL", Status = "Hazirlaniyor" });
            RecentOrders.Add(new RecentOrderDto { OrderNo = "SIP-2026-0039", Date = "16.03.2026", Customer = "Mehmet Kaya", Amount = "5,200.00 TL", Status = "Kargoda" });
            RecentOrders.Add(new RecentOrderDto { OrderNo = "SIP-2026-0038", Date = "16.03.2026", Customer = "Ayse Celik", Amount = "890.00 TL", Status = "Teslim Edildi" });
            RecentOrders.Add(new RecentOrderDto { OrderNo = "SIP-2026-0037", Date = "16.03.2026", Customer = "Ali Ozturk", Amount = "3,150.75 TL", Status = "Yeni" });
            RecentOrders.Add(new RecentOrderDto { OrderNo = "SIP-2026-0036", Date = "15.03.2026", Customer = "Zeynep Arslan", Amount = "4,720.00 TL", Status = "Hazirlaniyor" });
            RecentOrders.Add(new RecentOrderDto { OrderNo = "SIP-2026-0035", Date = "15.03.2026", Customer = "Hasan Dogan", Amount = "1,340.25 TL", Status = "Kargoda" });
            RecentOrders.Add(new RecentOrderDto { OrderNo = "SIP-2026-0034", Date = "15.03.2026", Customer = "Elif Sahin", Amount = "6,890.00 TL", Status = "Teslim Edildi" });
            RecentOrders.Add(new RecentOrderDto { OrderNo = "SIP-2026-0033", Date = "14.03.2026", Customer = "Burak Yildiz", Amount = "2,100.00 TL", Status = "Teslim Edildi" });
            RecentOrders.Add(new RecentOrderDto { OrderNo = "SIP-2026-0032", Date = "14.03.2026", Customer = "Selin Korkmaz", Amount = "7,450.50 TL", Status = "Kargoda" });

            IsEmpty = RecentOrders.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Dashboard yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class RecentOrderDto
{
    public string OrderNo { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Customer { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
