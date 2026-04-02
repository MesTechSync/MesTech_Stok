using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Application.Queries.GetStockMovements;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Stock Update (Toplu Stok Guncelleme) screen.
/// Displays stock items with editable "Yeni Stok" column and bulk update action.
/// </summary>
public partial class StockMovementAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly PropertyChangedEventHandler _itemChangedHandler;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int changedCount;
    [ObservableProperty] private string updateStatus = string.Empty;

    private readonly List<StockMovementItemDto> _allItems = [];
    public ObservableCollection<StockMovementItemDto> Items { get; } = [];

    public StockMovementAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
        _itemChangedHandler = (_, _) => RecalculateChangedCount();
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
            UnsubscribeItemEvents();
            Items.Clear();

            var movements = await _mediator.Send(new GetStockMovementsQuery()) ?? [];
            _allItems.Clear();
            foreach (var m in movements)
            {
                _allItems.Add(new StockMovementItemDto
                {
                    Sku = m.ProductSKU ?? string.Empty,
                    UrunAdi = m.ProductName ?? string.Empty,
                    MevcutStok = m.PreviousStock,
                    YeniStok = m.NewStock,
                    Platform = m.MovementType
                });
            }

            ApplyFilter();

            // Subscribe to changes (named handler — unsubscribe possible)
            foreach (var item in Items)
                item.PropertyChanged += _itemChangedHandler;

            TotalCount = Items.Count;
            IsEmpty = Items.Count == 0;
            RecalculateChangedCount();
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

    private void RecalculateChangedCount()
    {
        ChangedCount = Items.Count(x => x.MevcutStok != x.YeniStok);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        UnsubscribeItemEvents();
        Items.Clear();

        var filtered = _allItems.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(i =>
                i.UrunAdi.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                i.Platform.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                i.Sku.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in filtered)
            Items.Add(item);

        foreach (var item in Items)
            item.PropertyChanged += _itemChangedHandler;

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
        RecalculateChangedCount();
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    private void UnsubscribeItemEvents()
    {
        foreach (var item in Items)
            item.PropertyChanged -= _itemChangedHandler;
    }

    protected override void OnDispose()
    {
        UnsubscribeItemEvents();
        Items.Clear();
    }

    [RelayCommand]
    private async Task BulkUpdate()
    {
        var changed = Items.Where(x => x.MevcutStok != x.YeniStok).ToList();
        if (changed.Count == 0)
        {
            UpdateStatus = "Degisiklik yapilmadi.";
            return;
        }

        IsLoading = true;
        UpdateStatus = string.Empty;
        try
        {
            var bulkItems = changed.Select(c => new BulkUpdateStockItem(c.Sku, c.YeniStok)).ToList();
            await _mediator.Send(new BulkUpdateStockCommand(bulkItems));

            foreach (var item in changed)
                item.MevcutStok = item.YeniStok;

            RecalculateChangedCount();
            UpdateStatus = $"{changed.Count} urun stok bilgisi guncellendi.";
        }
        catch (Exception ex)
        {
            UpdateStatus = $"Guncelleme hatasi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public partial class StockMovementItemDto : ObservableObject
{
    public string Sku { get; set; } = string.Empty;
    public string UrunAdi { get; set; } = string.Empty;
    [ObservableProperty] private int mevcutStok;
    [ObservableProperty] private int yeniStok;
    public string Platform { get; set; } = string.Empty;
}
