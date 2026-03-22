using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels.Monitoring;

/// <summary>
/// Gecikmiş sipariş monitoring ViewModel — Chain 11 UI.
/// 48+ saat gönderilmemiş siparişleri listeler.
/// DEV 1'in GetStaleOrdersQuery endpoint'ine bağlanacak.
/// </summary>
public partial class StaleOrdersAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<StaleOrderItem> StaleOrders { get; } = [];

    public int TotalStaleCount => StaleOrders.Count;
    public int Warning48hCount => StaleOrders.Count(o => o.ElapsedHours >= 48 && o.ElapsedHours < 72);
    public int Critical72hCount => StaleOrders.Count(o => o.ElapsedHours >= 72);
    public bool IsEmpty => !IsLoading && StaleOrders.Count == 0;

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(200); // Simulate — will connect to GetStaleOrdersQuery

            StaleOrders.Clear();
            // Demo data — gerçek API bağlantısında MediatR Send kullanılacak
            StaleOrders.Add(new StaleOrderItem("SIP-2026-0891", "Trendyol", "Ahmet Yılmaz", DateTime.UtcNow.AddHours(-52)));
            StaleOrders.Add(new StaleOrderItem("SIP-2026-0887", "Hepsiburada", "Mehmet Demir", DateTime.UtcNow.AddHours(-74)));
            StaleOrders.Add(new StaleOrderItem("SIP-2026-0882", "N11", "Ayşe Kara", DateTime.UtcNow.AddHours(-96)));
            StaleOrders.Add(new StaleOrderItem("SIP-2026-0879", "Çiçeksepeti", "Elif Şahin", DateTime.UtcNow.AddHours(-120)));
            StaleOrders.Add(new StaleOrderItem("SIP-2026-0875", "Trendyol", "Hüseyin Öztürk", DateTime.UtcNow.AddHours(-168)));

            OnPropertyChanged(nameof(TotalStaleCount));
            OnPropertyChanged(nameof(Warning48hCount));
            OnPropertyChanged(nameof(Critical72hCount));
            OnPropertyChanged(nameof(IsEmpty));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}

public class StaleOrderItem
{
    public StaleOrderItem(string orderNumber, string platform, string customerName, DateTime orderDate)
    {
        OrderNumber = orderNumber;
        Platform = platform;
        CustomerName = customerName;
        OrderDate = orderDate;
        ElapsedHours = (DateTime.UtcNow - orderDate).TotalHours;
    }

    public string OrderNumber { get; }
    public string Platform { get; }
    public string CustomerName { get; }
    public DateTime OrderDate { get; }
    public double ElapsedHours { get; }

    public string OrderDateText => OrderDate.ToString("dd.MM.yyyy HH:mm");
    public string ElapsedText => ElapsedHours switch
    {
        < 72 => $"{(int)ElapsedHours} saat",
        < 168 => $"{(int)(ElapsedHours / 24)} gun",
        _ => $"{(int)(ElapsedHours / 24)} gun"
    };
    public string SeverityText => ElapsedHours switch
    {
        < 72 => "Uyari",
        < 120 => "Kritik",
        _ => "Acil"
    };
}
