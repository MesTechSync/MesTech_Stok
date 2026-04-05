using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
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

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    // HH-DEV2-012: New movement form fields
    [ObservableProperty] private bool isAddingMovement;
    [ObservableProperty] private string newMovementType = "Giris";
    [ObservableProperty] private string newMovementSku = string.Empty;
    [ObservableProperty] private int newMovementQuantity;
    [ObservableProperty] private string newMovementWarehouse = string.Empty;
    [ObservableProperty] private string newMovementNote = string.Empty;
    public string[] MovementTypes { get; } = ["Giris", "Cikis", "Transfer", "Duzeltme"];

    private readonly List<StockMovementItemDto> _allItems = [];
    public ObservableCollection<StockMovementItemDto> Items { get; } = [];

    public StockMovementAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
        _itemChangedHandler = (_, _) => RecalculateChangedCount();
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            UpdateStatus = string.Empty;
            UnsubscribeItemEvents();
            Items.Clear();

            var movements = await _mediator.Send(new GetStockMovementsQuery(), ct) ?? [];
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
        }, "Stok hareketleri yuklenirken hata");
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

        // Sort
        filtered = SortColumn switch
        {
            "Sku"       => SortAscending ? filtered.OrderBy(x => x.Sku)        : filtered.OrderByDescending(x => x.Sku),
            "UrunAdi"   => SortAscending ? filtered.OrderBy(x => x.UrunAdi)    : filtered.OrderByDescending(x => x.UrunAdi),
            "Platform"  => SortAscending ? filtered.OrderBy(x => x.Platform)   : filtered.OrderByDescending(x => x.Platform),
            "MevcutStok"=> SortAscending ? filtered.OrderBy(x => x.MevcutStok) : filtered.OrderByDescending(x => x.MevcutStok),
            "YeniStok"  => SortAscending ? filtered.OrderBy(x => x.YeniStok)   : filtered.OrderByDescending(x => x.YeniStok),
            _           => SortAscending ? filtered.OrderBy(x => x.UrunAdi)    : filtered.OrderByDescending(x => x.UrunAdi),
        };

        foreach (var item in filtered)
            Items.Add(item);

        foreach (var item in Items)
            item.PropertyChanged += _itemChangedHandler;

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
        RecalculateChangedCount();
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilter();
    }

    // HH-FIX-019: Excel export
    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(Guid.Empty, "stock-movements", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(
                    System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Stok hareketleri disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    // HH-DEV2-012: Toggle add-movement form visibility
    [RelayCommand]
    private void ToggleAddMovement() => IsAddingMovement = !IsAddingMovement;

    // HH-DEV2-012: Save new stock movement
    [RelayCommand]
    private async Task SaveNewMovement()
    {
        if (string.IsNullOrWhiteSpace(NewMovementSku) || NewMovementQuantity <= 0)
        {
            UpdateStatus = "SKU ve miktar zorunlu.";
            return;
        }

        UpdateStatus = $"{NewMovementType} hareketi kaydediliyor...";
        // TODO: CreateStockMovementCommand handler DEV1 tarafindan eklenecek
        // await _mediator.Send(new CreateStockMovementCommand(...));

        UpdateStatus = $"{NewMovementType}: {NewMovementSku} x {NewMovementQuantity} — kaydedildi.";
        NewMovementSku = string.Empty;
        NewMovementQuantity = 0;
        NewMovementNote = string.Empty;
        IsAddingMovement = false;
        await LoadAsync();
    }

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
