using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Variant Matrix Editor ViewModel — attribute groups with cartesian product generation.
/// Manages Color×Size (or any attribute) variant combinations with stock, price, status.
/// </summary>
public partial class ProductVariantMatrixViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    // Product header
    [ObservableProperty] private string productName = "Samsung Galaxy S24 Ultra";
    [ObservableProperty] private string productSKU = "TRY-ELK-001";

    // Loading

    // Attribute management
    [ObservableProperty] private string newAttributeName = string.Empty;

    public ObservableCollection<AttributeGroupDto> Attributes { get; } = [];

    // Variant grid
    public ObservableCollection<VariantRowDto> Variants { get; } = [];

    [ObservableProperty] private bool hasVariants;
    [ObservableProperty] private bool canGenerate;

    // Summary
    [ObservableProperty] private int totalVariants;
    [ObservableProperty] private int totalStock;
    [ObservableProperty] private int outOfStockCount;
    [ObservableProperty] private int activeVariantCount;
    [ObservableProperty] private string outOfStockColor = "#16A34A";

    // Bulk price update
    [ObservableProperty] private bool isBulkPriceVisible;
    [ObservableProperty] private string selectedPriceUpdateMode = "Yuzde Artis (%)";
    [ObservableProperty] private string priceUpdateValue = string.Empty;

    public ObservableCollection<string> PriceUpdateModes { get; } =
    [
        "Yuzde Artis (%)",
        "Yuzde Indirim (%)",
        "Sabit Tutar Ekle",
        "Sabit Tutar Cikart",
        "Yeni Fiyat Ata"
    ];

    public ProductVariantMatrixViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Attributes.Clear();
            Variants.Clear();
            UpdateCanGenerate();
        }
        finally
        {
            IsLoading = false;
            IsEmpty = true;
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void AddAttribute()
    {
        if (string.IsNullOrWhiteSpace(NewAttributeName)) return;

        var existing = Attributes.FirstOrDefault(a =>
            a.Name.Equals(NewAttributeName, StringComparison.OrdinalIgnoreCase));
        if (existing is not null) return;

        Attributes.Add(new AttributeGroupDto { Name = NewAttributeName.Trim() });
        NewAttributeName = string.Empty;
        UpdateCanGenerate();
    }

    [RelayCommand]
    private void RemoveAttribute(AttributeGroupDto? attr)
    {
        if (attr is null) return;
        Attributes.Remove(attr);
        UpdateCanGenerate();
    }

    [RelayCommand]
    private void GenerateVariants()
    {
        Variants.Clear();

        var groups = Attributes
            .Where(a => a.Values.Count > 0)
            .ToList();

        if (groups.Count == 0)
        {
            UpdateSummary();
            return;
        }

        // Cartesian product
        var combinations = CartesianProduct(groups);
        var index = 1;

        foreach (var combo in combinations)
        {
            var displayName = string.Join(" / ", combo.Select(c => c.Value));
            var skuSuffix = string.Join("-", combo.Select(c => c.Value.Replace(" ", "").Substring(0, Math.Min(3, c.Value.Replace(" ", "").Length)).ToUpperInvariant()));

            var stock = 0;
            var price = 0m;

            Variants.Add(new VariantRowDto
            {
                DisplayName = displayName,
                SKU = $"{ProductSKU}-{skuSuffix}",
                Barcode = $"869000{100 + index:D3}",
                Stock = stock,
                Price = price,
                IsActive = stock > 0,
                StockStatus = stock == 0 ? "Tukendi" : stock < 5 ? "Kritik" : stock < 15 ? "Dusuk" : "Normal"
            });
            index++;
        }

        UpdateSummary();
    }

    [RelayCommand]
    private Task SaveAllAsync()
    {
        IsLoading = true;
        try
        {
            // await _mediator.Send(new SaveProductVariantsCommand { ProductId = ..., Variants = ... });
        }
        finally
        {
            IsLoading = false;
        }
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void BulkUpdatePrice()
    {
        IsBulkPriceVisible = true;
        PriceUpdateValue = string.Empty;
    }

    [RelayCommand]
    private void ApplyBulkPrice()
    {
        if (!decimal.TryParse(PriceUpdateValue, out var value)) return;

        foreach (var variant in Variants)
        {
            variant.Price = SelectedPriceUpdateMode switch
            {
                "Yuzde Artis (%)" => variant.Price * (1 + value / 100),
                "Yuzde Indirim (%)" => variant.Price * (1 - value / 100),
                "Sabit Tutar Ekle" => variant.Price + value,
                "Sabit Tutar Cikart" => Math.Max(0, variant.Price - value),
                "Yeni Fiyat Ata" => value,
                _ => variant.Price
            };
        }

        IsBulkPriceVisible = false;

        // Force DataGrid refresh via replace
        var items = Variants.ToList();
        Variants.Clear();
        foreach (var item in items)
            Variants.Add(item);

        UpdateSummary();
    }

    [RelayCommand]
    private void CancelBulkPrice()
    {
        IsBulkPriceVisible = false;
    }

    [RelayCommand]
    private Task SendToPlatformAsync()
    {
        IsLoading = true;
        try
        {
        }
        finally
        {
            IsLoading = false;
        }
        return Task.CompletedTask;
    }

    private void UpdateCanGenerate()
    {
        CanGenerate = Attributes.Count > 0 && Attributes.Any(a => a.Values.Count > 0);
    }

    private void UpdateSummary()
    {
        HasVariants = Variants.Count > 0;
        TotalVariants = Variants.Count;
        TotalStock = Variants.Sum(v => v.Stock);
        OutOfStockCount = Variants.Count(v => v.Stock == 0);
        ActiveVariantCount = Variants.Count(v => v.IsActive);
        OutOfStockColor = OutOfStockCount > 0 ? "#DC2626" : "#16A34A";
    }

    /// <summary>
    /// Generates cartesian product of all attribute value lists.
    /// </summary>
    private static List<List<AttributeValueDto>> CartesianProduct(List<AttributeGroupDto> groups)
    {
        var result = new List<List<AttributeValueDto>>();

        if (groups.Count == 0) return result;

        // Seed with first group
        foreach (var val in groups[0].Values)
            result.Add([val]);

        // Multiply with remaining groups
        for (int i = 1; i < groups.Count; i++)
        {
            var newResult = new List<List<AttributeValueDto>>();
            foreach (var existing in result)
            {
                foreach (var val in groups[i].Values)
                {
                    var combo = new List<AttributeValueDto>(existing) { val };
                    newResult.Add(combo);
                }
            }
            result = newResult;
        }

        return result;
    }
}

/// <summary>
/// Attribute group (e.g. "Renk", "Beden") with its values.
/// </summary>
public partial class AttributeGroupDto : ObservableObject
{
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string newValue = string.Empty;

    public ObservableCollection<AttributeValueDto> Values { get; } = [];

    [RelayCommand]
    private void AddValue()
    {
        if (string.IsNullOrWhiteSpace(NewValue)) return;
        if (Values.Any(v => v.Value.Equals(NewValue, StringComparison.OrdinalIgnoreCase))) return;

        Values.Add(new AttributeValueDto { Value = NewValue.Trim() });
        NewValue = string.Empty;
    }

    [RelayCommand]
    private void RemoveValue(AttributeValueDto? val)
    {
        if (val is not null)
            Values.Remove(val);
    }
}

/// <summary>
/// Single attribute value (e.g. "Kirmizi", "L").
/// </summary>
public class AttributeValueDto
{
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Single variant row in the matrix DataGrid.
/// </summary>
public class VariantRowDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int Stock { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public string StockStatus { get; set; } = "Normal";
}
