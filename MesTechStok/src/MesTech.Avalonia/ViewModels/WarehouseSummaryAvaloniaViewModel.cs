using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetWarehouseSummary;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Depo ozet kartlari ViewModel — N depo x kapasite cubugu x kritik urun sayisi.
/// I-06 Gorev 6: Kapasite renkleri: &lt;50% yesil, 50-80% turuncu, &gt;80% kirmizi.
/// </summary>
public partial class WarehouseSummaryAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int totalWarehouses;

    public ObservableCollection<WarehouseSummaryCardDto> WarehouseCards { get; } = [];

    public WarehouseSummaryAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetWarehouseSummaryQuery(Guid.Empty));

            WarehouseCards.Clear();
            foreach (var w in result)
            {
                WarehouseCards.Add(new WarehouseSummaryCardDto
                {
                    Name = w.Name,
                    Location = w.Location ?? string.Empty,
                    ProductCount = w.ProductCount,
                    Capacity = (int)(w.MaxCapacity ?? 0),
                    OutOfStockCount = w.OutOfStockCount,
                    CriticalCount = w.CriticalStockCount,
                    NormalCount = w.NormalStockCount
                });
            }

            TotalWarehouses = WarehouseCards.Count;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Depo verileri yuklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void ShowDetail()
    {
        // TODO: Navigate to warehouse detail view
    }
}

public class WarehouseSummaryCardDto
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int Capacity { get; set; }
    public int OutOfStockCount { get; set; }
    public int CriticalCount { get; set; }
    public int NormalCount { get; set; }

    // Computed
    public string ProductCountText => $"{ProductCount:N0} urun";
    public int CapacityPercent => Capacity > 0 ? (int)Math.Round((double)ProductCount / Capacity * 100) : 0;
    public double CapacityBarWidth => Capacity > 0 ? Math.Min(280, 280.0 * ProductCount / Capacity) : 0;
    public string CapacityText => $"{CapacityPercent}% dolu ({ProductCount:N0} / {Capacity:N0})";

    public string CapacityBarColor => CapacityPercent switch
    {
        > 80 => "#EF4444",
        > 50 => "#F59E0B",
        _ => "#22C55E"
    };

    public bool HasOutOfStock => OutOfStockCount > 0;
    public bool HasCritical => CriticalCount > 0;
    public bool IsHealthy => OutOfStockCount == 0 && CriticalCount == 0;
    public string OutOfStockText => $"{OutOfStockCount} tukendi";
    public string CriticalText => $"{CriticalCount} kritik";
    public string NormalText => $"{NormalCount} normal";
}
