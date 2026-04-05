using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
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
    [ObservableProperty] private string searchText = string.Empty;

    private readonly List<StockTimelineItemDto> _allMovements = [];

    public ObservableCollection<StockTimelineItemDto> Movements { get; } = [];

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = false; // newest first

    public StockTimelineAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allMovements
            : _allMovements.Where(m =>
                m.Reason.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                m.MovementType.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                m.TypeText.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                m.DateText.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        // Sort
        var sortedList = SortColumn switch
        {
            "Date"         => SortAscending ? filtered.OrderBy(x => x.Date).ToList()         : filtered.OrderByDescending(x => x.Date).ToList(),
            "MovementType" => SortAscending ? filtered.OrderBy(x => x.MovementType).ToList() : filtered.OrderByDescending(x => x.MovementType).ToList(),
            "Quantity"     => SortAscending ? filtered.OrderBy(x => x.Quantity).ToList()     : filtered.OrderByDescending(x => x.Quantity).ToList(),
            "ResultStock"  => SortAscending ? filtered.OrderBy(x => x.ResultStock).ToList()  : filtered.OrderByDescending(x => x.ResultStock).ToList(),
            _              => SortAscending ? filtered.OrderBy(x => x.Date).ToList()         : filtered.OrderByDescending(x => x.Date).ToList(),
        };

        Movements.Clear();
        foreach (var m in sortedList)
            Movements.Add(m);

        TotalCount = Movements.Count;
        IsEmpty = Movements.Count == 0;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var movements = await _mediator.Send(new GetStockMovementsQuery(), ct) ?? [];

            _allMovements.Clear();
            foreach (var m in movements)
            {
                _allMovements.Add(new StockTimelineItemDto
                {
                    Date = m.Date,
                    MovementType = m.MovementType,
                    Quantity = m.Quantity,
                    ResultStock = m.NewStock,
                    Reason = m.Reason ?? m.DocumentNumber ?? string.Empty
                });
            }

            ApplyFilter();
        }, "Stok hareketleri yuklenirken hata");
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilter();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(Guid.Empty, "stock-timeline", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Stok hareket gecmisi disa aktarilirken hata");
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
