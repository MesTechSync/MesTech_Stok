using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Platform.Commands.TriggerSync;
using MesTech.Application.Features.Product.Queries.GetProducts;
using MesTech.Application.Features.Stock.Queries.GetStockSummary;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stock Management ViewModel — KPI summary + product DataGrid with stock info.
/// </summary>
public partial class StockAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;

    // KPI
    [ObservableProperty] private int totalProducts;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int inStockProducts;
    [ObservableProperty] private int outOfStockProducts;
    [ObservableProperty] private int lowStockProducts;
    [ObservableProperty] private decimal totalStockValue;
    [ObservableProperty] private int totalUnits;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private bool isSyncing;
    [ObservableProperty] private string syncStatus = string.Empty;

    // DataGrid
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedStockFilter = "Tumu";
    [ObservableProperty] private int currentPage = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private string paginationInfo = string.Empty;
    [ObservableProperty] private string sortColumn = "Name";
    [ObservableProperty] private bool sortAscending = true;
    public int[] PageSizeOptions { get; } = [25, 50, 100];
    public string[] StockFilterOptions { get; } = ["Tumu", "Stokta", "Dusuk Stok", "Stoksuz"];

    public ObservableCollection<StockProductItemDto> Products { get; } = [];
    private List<StockProductItemDto> _allProducts = [];

    public StockAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
        Title = "Stok Yonetimi";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            // KPI
            var summaryResult = await _mediator.Send(
                new GetStockSummaryQuery(_currentUser.TenantId), ct);

            TotalProducts = summaryResult.TotalProducts;
            InStockProducts = summaryResult.InStockProducts;
            OutOfStockProducts = summaryResult.OutOfStockProducts;
            LowStockProducts = summaryResult.LowStockProducts;
            TotalStockValue = summaryResult.TotalStockValue;
            TotalUnits = summaryResult.TotalUnits;
            Summary = $"{TotalProducts} urun — {TotalStockValue:N2} ₺ deger";

            // Product list with stock
            var products = await _mediator.Send(new GetProductsQuery(
                _currentUser.TenantId, PageSize: 5000), ct);

            _allProducts = products.Items.Select(p => new StockProductItemDto
            {
                Name = p.Name,
                SKU = p.SKU,
                Barcode = p.Barcode ?? string.Empty,
                Brand = p.Brand ?? string.Empty,
                Stock = p.Stock,
                MinimumStock = p.MinimumStock,
                SalePrice = p.SalePrice,
                StockValue = p.SalePrice * p.Stock,
                ImageUrl = p.ImageUrl ?? string.Empty
            }).ToList();

            ApplyFilters();
        }, "Stok verileri yuklenirken hata");
    }

    private void ApplyFilters()
    {
        var filtered = _allProducts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.SKU.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                p.Barcode.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        filtered = SelectedStockFilter switch
        {
            "Stokta" => filtered.Where(p => p.Stock > p.MinimumStock),
            "Dusuk Stok" => filtered.Where(p => p.Stock > 0 && p.Stock <= p.MinimumStock),
            "Stoksuz" => filtered.Where(p => p.Stock == 0),
            _ => filtered
        };

        filtered = SortColumn switch
        {
            "Stock" => SortAscending ? filtered.OrderBy(p => p.Stock) : filtered.OrderByDescending(p => p.Stock),
            "SKU" => SortAscending ? filtered.OrderBy(p => p.SKU) : filtered.OrderByDescending(p => p.SKU),
            "Price" => SortAscending ? filtered.OrderBy(p => p.SalePrice) : filtered.OrderByDescending(p => p.SalePrice),
            _ => SortAscending ? filtered.OrderBy(p => p.Name) : filtered.OrderByDescending(p => p.Name),
        };

        var all = filtered.ToList();
        TotalCount = all.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;

        Products.Clear();
        foreach (var item in all.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
            Products.Add(item);

        IsEmpty = TotalCount == 0;
        PaginationInfo = TotalCount > 0
            ? $"Sayfa {CurrentPage}/{TotalPages} ({TotalCount} urun)"
            : string.Empty;
    }

    partial void OnSearchTextChanged(string value) { if (_allProducts.Count > 0) { CurrentPage = 1; ApplyFilters(); } }
    partial void OnSelectedStockFilterChanged(string value) { if (_allProducts.Count > 0) { CurrentPage = 1; ApplyFilters(); } }
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; if (_allProducts.Count > 0) ApplyFilters(); }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        CurrentPage = 1;
        ApplyFilters();
    }

    [RelayCommand]
    private void NextPage() { if (CurrentPage < TotalPages) { CurrentPage++; ApplyFilters(); } }
    [RelayCommand]
    private void PrevPage() { if (CurrentPage > 1) { CurrentPage--; ApplyFilters(); } }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    // HH-DEV2-017: Platform stock sync
    [RelayCommand]
    private async Task SyncPlatformStock()
    {
        var confirmed = await _dialog.ShowConfirmAsync(
            "Tum platformlara stok gonderilecek. Devam etmek istiyor musunuz?",
            "Stok Senkronizasyonu");
        if (!confirmed) return;

        IsSyncing = true;
        SyncStatus = "Platformlara stok gonderiyor...";

        try
        {
            var result = await _mediator.Send(new TriggerSyncCommand(
                _currentUser.TenantId, "stock"), CancellationToken);

            SyncStatus = result.IsSuccess
                ? "Stok senkronizasyonu tamamlandi."
                : $"Hata: {result.ErrorMessage}";
        }
        catch (Exception ex)
        {
            SyncStatus = $"Senkronizasyon hatasi: {ex.Message}";
        }
        finally
        {
            IsSyncing = false;
        }
    }
}

public class StockProductItemDto
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int MinimumStock { get; set; }
    public decimal SalePrice { get; set; }
    public decimal StockValue { get; set; }
    public string ImageUrl { get; set; } = string.Empty;

    public string StockStatus => Stock == 0 ? "Stoksuz" : Stock <= MinimumStock ? "Dusuk" : "Normal";
    public string StockColor => Stock == 0 ? "#EF4444" : Stock <= MinimumStock ? "#F59E0B" : "#10B981";
}
