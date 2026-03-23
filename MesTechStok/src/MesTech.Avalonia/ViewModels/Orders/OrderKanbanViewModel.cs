using System.Collections.ObjectModel;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels.Orders;

/// <summary>
/// Sipariş Kanban Board ViewModel — Bitrix24 pattern.
/// Yeni → Onaylandı → Hazırlanıyor → Kargoda → Teslim Edildi
/// </summary>
public partial class OrderKanbanViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string selectedPlatform = "Tümü";

    public ObservableCollection<string> PlatformFilters { get; } =
        ["Tümü", "Trendyol", "Hepsiburada", "N11", "Çiçeksepeti", "Pazarama"];

    public ObservableCollection<KanbanColumnVm> KanbanColumns { get; } = [];
    public ObservableCollection<ColumnSummaryVm> ColumnSummaries { get; } = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(300);
            BuildKanbanBoard();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildKanbanBoard()
    {
        KanbanColumns.Clear();
        ColumnSummaries.Clear();

        var columns = new[]
        {
            ("Yeni", "#3B82F6", new[]
            {
                new KanbanOrderCard("SIP-0041", "Trendyol", "TR", "#F27A1A", "Ahmet Yılmaz", "2,450 TL", "3 ürün", "2 saat"),
                new KanbanOrderCard("SIP-0037", "Trendyol", "TR", "#F27A1A", "Ali Öztürk", "3,150 TL", "1 ürün", "6 saat"),
                new KanbanOrderCard("SIP-0031", "Pazarama", "PZ", "#FF5722", "Emre Aksoy", "1,675 TL", "2 ürün", "12 saat"),
                new KanbanOrderCard("SIP-0027", "N11", "N11", "#6B21A8", "Neşe Karaca", "8,900 TL", "5 ürün", "1 gün"),
            }),
            ("Hazırlanıyor", "#F59E0B", new[]
            {
                new KanbanOrderCard("SIP-0040", "Hepsiburada", "HB", "#FF6000", "Fatma Demir", "1,890 TL", "2 ürün", "3 saat"),
                new KanbanOrderCard("SIP-0036", "Çiçeksepeti", "ÇS", "#E91E63", "Zeynep Arslan", "4,720 TL", "4 ürün", "8 saat"),
                new KanbanOrderCard("SIP-0030", "Hepsiburada", "HB", "#FF6000", "Deniz Polat", "3,890 TL", "1 ürün", "1 gün"),
            }),
            ("Kargoda", "#10B981", new[]
            {
                new KanbanOrderCard("SIP-0039", "Trendyol", "TR", "#F27A1A", "Mehmet Kaya", "5,200 TL", "2 ürün", "1 gün"),
                new KanbanOrderCard("SIP-0035", "Hepsiburada", "HB", "#FF6000", "Hasan Doğan", "1,340 TL", "1 ürün", "2 gün"),
                new KanbanOrderCard("SIP-0032", "Trendyol", "TR", "#F27A1A", "Selin Korkmaz", "7,450 TL", "3 ürün", "2 gün"),
                new KanbanOrderCard("SIP-0028", "Çiçeksepeti", "ÇS", "#E91E63", "Cem Aydın", "4,200 TL", "2 ürün", "3 gün"),
            }),
            ("Teslim Edildi", "#6366F1", new[]
            {
                new KanbanOrderCard("SIP-0038", "N11", "N11", "#6B21A8", "Ayşe Çelik", "890 TL", "1 ürün", "2 gün"),
                new KanbanOrderCard("SIP-0034", "Trendyol", "TR", "#F27A1A", "Elif Şahin", "6,890 TL", "4 ürün", "3 gün"),
                new KanbanOrderCard("SIP-0033", "N11", "N11", "#6B21A8", "Burak Yıldız", "2,100 TL", "1 ürün", "4 gün"),
                new KanbanOrderCard("SIP-0029", "Trendyol", "TR", "#F27A1A", "Gül Erdem", "12,350 TL", "7 ürün", "5 gün"),
            }),
        };

        foreach (var (name, color, orders) in columns)
        {
            var col = new KanbanColumnVm(name, SolidColorBrush.Parse(color));
            foreach (var o in orders) col.Orders.Add(o);
            KanbanColumns.Add(col);
            ColumnSummaries.Add(new ColumnSummaryVm(name, SolidColorBrush.Parse(color), orders.Length));
        }
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    partial void OnSelectedPlatformChanged(string value)
    {
        // Platform filtre uygulanacak — şimdilik tüm verileri göster
        _ = LoadAsync();
    }
}

public class KanbanColumnVm
{
    public KanbanColumnVm(string statusName, ISolidColorBrush headerColor)
    {
        StatusName = statusName;
        HeaderColor = headerColor;
    }

    public string StatusName { get; }
    public ISolidColorBrush HeaderColor { get; }
    public ObservableCollection<KanbanOrderCard> Orders { get; } = [];
}

public class KanbanOrderCard
{
    public KanbanOrderCard(string orderNumber, string platform, string platformShort,
        string platformColor, string customerName, string totalAmountText,
        string itemCountText, string timeAgo)
    {
        OrderNumber = orderNumber;
        Platform = platform;
        PlatformShort = platformShort;
        PlatformColor = SolidColorBrush.Parse(platformColor);
        CustomerName = customerName;
        TotalAmountText = totalAmountText;
        ItemCountText = itemCountText;
        TimeAgo = timeAgo;
    }

    public string OrderNumber { get; }
    public string Platform { get; }
    public string PlatformShort { get; }
    public ISolidColorBrush PlatformColor { get; }
    public string CustomerName { get; }
    public string TotalAmountText { get; }
    public string ItemCountText { get; }
    public string TimeAgo { get; }
}

public class ColumnSummaryVm
{
    public ColumnSummaryVm(string statusName, ISolidColorBrush headerColor, int count)
    {
        StatusName = statusName;
        HeaderColor = headerColor;
        Count = count;
    }

    public string StatusName { get; }
    public ISolidColorBrush HeaderColor { get; }
    public int Count { get; }
}
