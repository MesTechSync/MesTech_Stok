using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetWarehouses;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Depo yonetimi ViewModel — kart gorunumu + kapasite doluluk cubugu + yeni depo ekleme.
/// EMR-06 Gorev 4E: Enhanced from placeholder to functional view.
/// </summary>
public partial class WarehouseAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    // New warehouse dialog
    [ObservableProperty] private bool isAddingWarehouse;
    [ObservableProperty] private string newWarehouseName = string.Empty;
    [ObservableProperty] private string newWarehouseLocation = string.Empty;
    [ObservableProperty] private int newWarehouseCapacity = 1000;

    public ObservableCollection<WarehouseCardDto> Items { get; } = [];
    private List<WarehouseCardDto> _allItems = [];

    public WarehouseAvaloniaViewModel(IMediator mediator)
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
            var result = await _mediator.Send(new GetWarehousesQuery());

            _allItems = result.Select(w => new WarehouseCardDto
            {
                Name = w.Name,
                Location = string.IsNullOrWhiteSpace(w.City) ? (w.Address ?? string.Empty) : w.City,
                ProductCount = 0,
                TotalStock = 0,
                AlarmCount = 0,
                Capacity = 0,
                UsedCapacity = 0,
                ShelfCount = 0
            }).ToList();

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Depo verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length == 0 || value.Length >= 2)
            ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(w =>
                w.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                w.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    [RelayCommand]
    private void AddWarehouse()
    {
        NewWarehouseName = string.Empty;
        NewWarehouseLocation = string.Empty;
        NewWarehouseCapacity = 1000;
        IsAddingWarehouse = true;
    }

    [RelayCommand]
    private void CancelAddWarehouse()
    {
        IsAddingWarehouse = false;
    }

    [RelayCommand]
    private Task SaveWarehouseAsync()
    {
        if (string.IsNullOrWhiteSpace(NewWarehouseName)) return Task.CompletedTask;

        IsLoading = true;
        try
        {
            var newWarehouse = new WarehouseCardDto
            {
                Name = NewWarehouseName,
                Location = NewWarehouseLocation,
                ProductCount = 0,
                TotalStock = 0,
                AlarmCount = 0,
                Capacity = NewWarehouseCapacity,
                UsedCapacity = 0,
                ShelfCount = 0
            };

            _allItems.Add(newWarehouse);
            ApplyFilters();
            IsAddingWarehouse = false;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Depo eklenemedi: {ex.Message}";
        }
        finally { IsLoading = false; }
        return Task.CompletedTask;
    }
}

public class WarehouseCardDto
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public int TotalStock { get; set; }
    public int AlarmCount { get; set; }
    public int Capacity { get; set; }
    public int UsedCapacity { get; set; }
    public int ShelfCount { get; set; }

    public int CapacityPercent => Capacity > 0
        ? (int)Math.Round((double)UsedCapacity / Capacity * 100)
        : 0;

    /// <summary>Width for capacity bar (max ~280px inside 320px card).</summary>
    public double CapacityBarWidth => Capacity > 0
        ? Math.Min(280, 280.0 * UsedCapacity / Capacity)
        : 0;

    public string CapacityColor => CapacityPercent switch
    {
        >= 90 => "#EF4444",
        >= 70 => "#F59E0B",
        _ => "#22C55E"
    };
}
