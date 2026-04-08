using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetWarehouseSummary;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Depo ozet kartlari ViewModel — N depo x kapasite cubugu x kritik urun sayisi.
/// I-06 Gorev 6: Kapasite renkleri: &lt;50% yesil, 50-80% turuncu, &gt;80% kirmizi.
/// </summary>
public partial class WarehouseSummaryAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;

    [ObservableProperty] private int totalWarehouses;

    public ObservableCollection<WarehouseSummaryCardDto> WarehouseCards { get; } = [];

    public WarehouseSummaryAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetWarehouseSummaryQuery(_currentUser.TenantId), ct);

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
            IsEmpty = WarehouseCards.Count == 0;
        }, "Depo ozeti yuklenirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task ShowDetail()
    {
        await _dialog.ShowInfoAsync("Depo detay sayfasi yakinda aktif olacak.", "MesTech");
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

    public IRelayCommand? ShowDetailCommand { get; set; }
}
