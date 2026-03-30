using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetStockMovements;

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

            var movements = await _mediator.Send(new GetStockMovementsQuery(), CancellationToken);

            Movements.Clear();
            foreach (var m in movements)
            {
                Movements.Add(new StockTimelineItemDto
                {
                    Date = m.Date,
                    MovementType = m.MovementType,
                    Quantity = m.Quantity,
                    ResultStock = m.NewStock,
                    Reason = m.Reason ?? m.DocumentNumber ?? string.Empty
                });
            }

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
