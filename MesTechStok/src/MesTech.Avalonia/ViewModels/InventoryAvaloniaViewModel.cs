using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetInventoryPaged;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Envanter yonetimi ViewModel — wired to GetInventoryPagedQuery via MediatR.
/// 4 seviye stok badge + depo filtre + KPI kartlari + sayfalama.
/// </summary>
public partial class InventoryAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int alarmCount;

    // KPI
    [ObservableProperty] private int kpiTotal;
    [ObservableProperty] private int kpiCritical;
    [ObservableProperty] private decimal kpiStockValue;
    [ObservableProperty] private int kpiOutOfStock;

    // Warehouse filter
    [ObservableProperty] private string? selectedWarehouse;
    public ObservableCollection<string> WarehouseFilter { get; } = [];

    // Pagination
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private bool canGoPrevious;
    [ObservableProperty] private bool canGoNext;
    [ObservableProperty] private string paginationInfo = string.Empty;
    private const int PageSize = 25;

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    public ObservableCollection<InventoryItemDto> Items { get; } = [];
    private List<InventoryItemDto> _allItems = [];

    public InventoryAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetInventoryPagedQuery(
                Page: 1, PageSize: 500, SearchTerm: null), ct);

            _allItems = result.Items.Select(i => new InventoryItemDto
            {
                Sku = i.Barcode,
                Ad = i.ProductName,
                Miktar = i.Stock,
                MinStok = i.MinimumStock,
                MaxStok = i.MinimumStock * 10,
                Depo = string.IsNullOrEmpty(i.Location) ? "Ana Depo" : i.Location,
                UnitPrice = i.Price
            }).ToList();

            // Populate warehouse filter
            WarehouseFilter.Clear();
            WarehouseFilter.Add("Tum Depolar");
            foreach (var w in _allItems.Select(i => i.Depo).Distinct().OrderBy(w => w))
                WarehouseFilter.Add(w);

            if (SelectedWarehouse is null)
                SelectedWarehouse = "Tum Depolar";

            // KPI
            KpiTotal = _allItems.Count;
            KpiCritical = _allItems.Count(i => i.Miktar > 0 && i.Miktar <= i.MinStok);
            KpiStockValue = _allItems.Sum(i => i.Miktar * i.UnitPrice);
            KpiOutOfStock = _allItems.Count(i => i.Miktar <= 0);

            CurrentPage = 1;
            ApplyFilters();
        }, "Envanter yuklenirken hata");
    }

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SelectedWarehouse) && SelectedWarehouse != "Tum Depolar")
            filtered = filtered.Where(i => i.Depo == SelectedWarehouse);

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.Ad.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                i.Depo.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var filteredList = filtered.ToList();
        TotalCount = filteredList.Count;
        AlarmCount = filteredList.Count(i => i.Miktar < i.MinStok);

        // Sort
        filteredList = SortColumn switch
        {
            "Sku"     => SortAscending ? filteredList.OrderBy(x => x.Sku).ToList()     : filteredList.OrderByDescending(x => x.Sku).ToList(),
            "Ad"      => SortAscending ? filteredList.OrderBy(x => x.Ad).ToList()      : filteredList.OrderByDescending(x => x.Ad).ToList(),
            "Miktar"  => SortAscending ? filteredList.OrderBy(x => x.Miktar).ToList()  : filteredList.OrderByDescending(x => x.Miktar).ToList(),
            "Depo"    => SortAscending ? filteredList.OrderBy(x => x.Depo).ToList()    : filteredList.OrderByDescending(x => x.Depo).ToList(),
            "MinStok" => SortAscending ? filteredList.OrderBy(x => x.MinStok).ToList() : filteredList.OrderByDescending(x => x.MinStok).ToList(),
            _         => SortAscending ? filteredList.OrderBy(x => x.Ad).ToList()      : filteredList.OrderByDescending(x => x.Ad).ToList(),
        };

        // Pagination
        int totalPages = Math.Max(1, (int)Math.Ceiling((double)filteredList.Count / PageSize));
        if (CurrentPage > totalPages) CurrentPage = totalPages;

        var paged = filteredList
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        Items.Clear();
        foreach (var item in paged)
            Items.Add(item);

        IsEmpty = filteredList.Count == 0;
        CanGoPrevious = CurrentPage > 1;
        CanGoNext = CurrentPage < totalPages;
        PaginationInfo = $"{filteredList.Count} urunden {(CurrentPage - 1) * PageSize + 1}-{Math.Min(CurrentPage * PageSize, filteredList.Count)} gosteriliyor";
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        CurrentPage = 1;
        ApplyFilters();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void NextPage()
    {
        CurrentPage++;
        ApplyFilters();
    }

    [RelayCommand]
    private void PreviousPage()
    {
        CurrentPage--;
        ApplyFilters();
    }

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
        {
            CurrentPage = 1;
            ApplyFilters();
        }
    }

    partial void OnSelectedWarehouseChanged(string? value)
    {
        if (_allItems.Count > 0)
        {
            CurrentPage = 1;
            ApplyFilters();
        }
    }
}

public class InventoryItemDto
{
    public string Sku { get; set; } = string.Empty;
    public string Ad { get; set; } = string.Empty;
    public int Miktar { get; set; }
    public int MinStok { get; set; }
    public int MaxStok { get; set; }
    public string Depo { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public bool IsAlarm => Miktar < MinStok;

    public string StokDurum
    {
        get
        {
            if (Miktar <= 0) return "TUKENDI";
            if (Miktar <= MinStok) return "KRITIK";
            if (Miktar <= MinStok * 1.5) return "DUSUK";
            return "YETERLI";
        }
    }

    public string StokDurumRenk => StokDurum switch
    {
        "TUKENDI" => "#D32F2F",
        "KRITIK" => "#D32F2F",
        "DUSUK" => "#F57C00",
        "YETERLI" => "#388E3C",
        _ => "#388E3C"
    };
}
