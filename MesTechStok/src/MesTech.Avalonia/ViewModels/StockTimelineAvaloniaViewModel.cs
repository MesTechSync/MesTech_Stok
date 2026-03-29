using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stok hareket gecmisi timeline ViewModel — 5 hareket tipi x renk kodu.
/// I-06 Gorev 5: Alim(yesil), Satis(kirmizi), Transfer(mavi), Iade(mor), Duzeltme(gri).
/// </summary>
public partial class StockTimelineAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int totalCount;

    public ObservableCollection<StockTimelineItemDto> Movements { get; } = [];

    public StockTimelineAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            // MediatR handler bağlantısı bekliyor — Task.Delay kaldırıldı // Will be replaced with GetStockMovementsQuery via MediatR

            Movements.Clear();
            var now = DateTime.Now;

            Movements.Add(new StockTimelineItemDto { Date = now.AddHours(-2), MovementType = "Sale", Quantity = -2, ResultStock = 40, Reason = "Trendyol #T-1234" });
            Movements.Add(new StockTimelineItemDto { Date = now.AddHours(-8), MovementType = "Purchase", Quantity = 50, ResultStock = 42, Reason = "Tedarikci ABC" });
            Movements.Add(new StockTimelineItemDto { Date = now.AddDays(-1).AddHours(-3), MovementType = "Transfer", Quantity = -15, ResultStock = -8, Reason = "Ana Depo > Depo 2" });
            Movements.Add(new StockTimelineItemDto { Date = now.AddDays(-1).AddHours(-6), MovementType = "Sale", Quantity = -3, ResultStock = 23, Reason = "Hepsiburada #HB-5678" });
            Movements.Add(new StockTimelineItemDto { Date = now.AddDays(-2).AddHours(-1), MovementType = "Return", Quantity = 1, ResultStock = 26, Reason = "Musteri iade" });
            Movements.Add(new StockTimelineItemDto { Date = now.AddDays(-2).AddHours(-7), MovementType = "Adjustment", Quantity = 5, ResultStock = 25, Reason = "Sayim farki" });
            Movements.Add(new StockTimelineItemDto { Date = now.AddDays(-3), MovementType = "Sale", Quantity = -10, ResultStock = 20, Reason = "N11 #N-9876" });
            Movements.Add(new StockTimelineItemDto { Date = now.AddDays(-4), MovementType = "Purchase", Quantity = 100, ResultStock = 30, Reason = "Toplu alim — XYZ Ltd." });
            Movements.Add(new StockTimelineItemDto { Date = now.AddDays(-5), MovementType = "Transfer", Quantity = 20, ResultStock = -70, Reason = "Depo 2 > Ana Depo" });
            Movements.Add(new StockTimelineItemDto { Date = now.AddDays(-6), MovementType = "Adjustment", Quantity = -2, ResultStock = -90, Reason = "Fire kayit" });

            TotalCount = Movements.Count;
            IsEmpty = Movements.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Stok hareketleri yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task FilterWeek()
    {
        // Filter to last 7 days — will use MediatR query with date range
        await LoadAsync();
    }

    [RelayCommand]
    private async Task FilterMonth()
    {
        // Filter to last 30 days — will use MediatR query with date range
        await LoadAsync();
    }
}

public class StockTimelineItemDto
{
    public DateTime Date { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ResultStock { get; set; }
    public string Reason { get; set; } = string.Empty;

    // Computed display
    public string DateText => Date.ToString("dd.MM");
    public string TimeText => Date.ToString("HH:mm");

    public string TypeIcon => MovementType switch
    {
        "Purchase" => "\u2191",
        "Sale" => "\u2193",
        "Transfer" => "\u2194",
        "Return" => "\u21A9",
        "Adjustment" => "\u2699",
        _ => "?"
    };

    public string TypeText => MovementType switch
    {
        "Purchase" => "Alim",
        "Sale" => "Satis",
        "Transfer" => "Transfer",
        "Return" => "Iade",
        "Adjustment" => "Duzeltme",
        _ => MovementType
    };

    public string TypeColor => MovementType switch
    {
        "Purchase" => "#16A34A",
        "Sale" => "#DC2626",
        "Transfer" => "#2563EB",
        "Return" => "#7C3AED",
        "Adjustment" => "#6B7280",
        _ => "#6B7280"
    };

    public string QuantityText => Quantity > 0 ? $"+{Quantity}" : $"{Quantity}";
    public string QuantityColor => Quantity > 0 ? "#16A34A" : "#DC2626";
    public string ResultStockText => $"\u2192 {ResultStock}";
}
