using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Application.Features.Product.Queries.GetProducts;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stock Update ViewModel — DataGrid with SKU, Ad, Mevcut Stok, Yeni Stok, Platform.
/// Wired to GetProductsQuery + BulkUpdateStockCommand via MediatR.
/// </summary>
public partial class StockUpdateAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string updateStatus = string.Empty;

    public ObservableCollection<StockUpdateItemDto> StockItems { get; } = [];

    private List<StockUpdateItemDto> _allItems = [];

    public StockUpdateAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        UpdateStatus = string.Empty;
        try
        {
            var result = await _mediator.Send(
                new GetProductsQuery(_tenantProvider.GetCurrentTenantId(), SearchTerm: null, IsActive: true, Page: 1, PageSize: 200),
                CancellationToken);

            _allItems = result.Items.Select(p => new StockUpdateItemDto
            {
                Sku = p.SKU,
                UrunAdi = p.Name,
                MevcutStok = p.Stock,
                YeniStok = p.Stock,
                Platform = p.SupplierName ?? "MesTech"
            }).ToList();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Stok verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(s =>
                s.Sku.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.UrunAdi.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Platform.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        StockItems.Clear();
        foreach (var item in filtered)
            StockItems.Add(item);

        TotalCount = StockItems.Count;
        IsEmpty = StockItems.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task BulkUpdateAsync()
    {
        IsLoading = true;
        UpdateStatus = string.Empty;
        try
        {
            var changedItems = _allItems
                .Where(i => i.MevcutStok != i.YeniStok)
                .Select(i => new BulkUpdateStockItem(i.Sku, i.YeniStok))
                .ToList();

            if (changedItems.Count == 0)
            {
                UpdateStatus = "Guncellenecek stok degisikligi bulunamadi.";
                return;
            }

            var result = await _mediator.Send(
                new BulkUpdateStockCommand(changedItems), CancellationToken);

            // Sync local state
            foreach (var item in _allItems.Where(i => i.MevcutStok != i.YeniStok))
                item.MevcutStok = item.YeniStok;

            ApplyFilters();
            UpdateStatus = $"{result.SuccessCount} urun stoku guncellendi." +
                (result.FailedCount > 0 ? $" {result.FailedCount} basarisiz." : "");
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Toplu guncelleme basarisiz: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (_allItems.Count > 0)
            ApplyFilters();
    }
}

public class StockUpdateItemDto
{
    public string Sku { get; set; } = string.Empty;
    public string UrunAdi { get; set; } = string.Empty;
    public int MevcutStok { get; set; }
    public int YeniStok { get; set; }
    public string Platform { get; set; } = string.Empty;
}
