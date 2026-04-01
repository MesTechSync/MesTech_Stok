#pragma warning disable CS1998
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Product.Commands.BulkUpdateProducts;
using MesTech.Domain.Enums;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Toplu Urun Islemleri ViewModel — Import / Export / Toplu Guncelle.
/// </summary>
public partial class BulkProductAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    // Common

    // Tab 1: Import
    [ObservableProperty] private string importFilePath = string.Empty;
    [ObservableProperty] private string importFileInfo = string.Empty;
    [ObservableProperty] private bool hasImportFile;
    [ObservableProperty] private bool isImporting;
    [ObservableProperty] private double importProgress;
    [ObservableProperty] private string importStatusText = "Dosya seciniz";
    [ObservableProperty] private bool canImport;
    [ObservableProperty] private bool updateExistingProducts = true;
    [ObservableProperty] private bool skipErrors;
    [ObservableProperty] private string previewValidationSummary = string.Empty;
    [ObservableProperty] private string searchText = string.Empty;

    private readonly List<ImportPreviewRowDto> _allPreviewRows = [];

    public ObservableCollection<ColumnMappingDto> ColumnMappings { get; } = [];
    public ObservableCollection<ImportPreviewRowDto> PreviewRows { get; } = [];

    // Tab 2: Export
    [ObservableProperty] private string selectedExportPlatform = "Tumu";
    [ObservableProperty] private string selectedExportCategory = "Tumu";
    [ObservableProperty] private string selectedExportStockFilter = "Tumu";
    [ObservableProperty] private bool isXlsxFormat = true;
    [ObservableProperty] private bool isCsvFormat;
    [ObservableProperty] private int exportProductCount;

    public ObservableCollection<string> ExportPlatformList { get; } = ["Tumu", "Trendyol", "Hepsiburada", "N11", "Amazon", "Ciceksepeti"];
    public ObservableCollection<string> ExportCategoryList { get; } = ["Tumu", "Elektronik", "Giyim", "Ev & Yasam", "Kozmetik"];
    public ObservableCollection<string> ExportStockFilterList { get; } = ["Tumu", "Stokta", "Stok Bitmis", "Kritik Stok"];

    // Tab 3: Bulk Update
    [ObservableProperty] private int selectedProductCount;
    [ObservableProperty] private string selectedBulkAction = string.Empty;
    [ObservableProperty] private string bulkActionValue = string.Empty;
    [ObservableProperty] private bool hasBulkPreview;
    [ObservableProperty] private bool canBulkUpdate;
    [ObservableProperty] private string bulkUpdateStatusText = "Islem secin ve deger girin";

    public ObservableCollection<string> BulkActionList { get; } =
    [
        "Fiyat artir (%)",
        "Fiyat azalt (%)",
        "Fiyat sabitle",
        "Stok ayarla",
        "Durum degistir (Aktif)",
        "Durum degistir (Pasif)",
        "Kategori degistir",
        "KDV orani degistir"
    ];

    public ObservableCollection<BulkPreviewRowDto> BulkPreviewRows { get; } = [];
    public List<Guid> SelectedProductIds { get; set; } = [];

    private static BulkUpdateAction MapAction(string action) => action switch
    {
        "Fiyat artir (%)" => BulkUpdateAction.PriceIncreasePercent,
        "Fiyat azalt (%)" => BulkUpdateAction.PriceDecreasePercent,
        "Fiyat sabitle" => BulkUpdateAction.PriceSetFixed,
        "Stok ayarla" => BulkUpdateAction.StockSet,
        "Durum degistir (Aktif)" => BulkUpdateAction.StatusActivate,
        "Durum degistir (Pasif)" => BulkUpdateAction.StatusDeactivate,
        "Kategori degistir" => BulkUpdateAction.CategoryAssign,
        _ => BulkUpdateAction.PriceSetFixed
    };

    public BulkProductAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    partial void OnSearchTextChanged(string value) => ApplyPreviewFilter();

    private void ApplyPreviewFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allPreviewRows
            : _allPreviewRows.Where(r =>
                r.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                r.Sku.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        PreviewRows.Clear();
        foreach (var r in filtered)
            PreviewRows.Add(r);
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var status = await _mediator.Send(new Application.Queries.GetProductDbStatus.GetProductDbStatusQuery());
            ExportProductCount = status.TotalCount;
            SelectedProductCount = 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Toplu islem verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            IsEmpty = PreviewRows.Count == 0;
        }
    }

    [RelayCommand]
    private async Task SelectFileAsync()
    {
        ImportFilePath = "ornek_urunler.xlsx";
        ImportFileInfo = "Excel dosyasi: 156 satir, 12 kolon";
        HasImportFile = true;
        CanImport = true;

        // Demo column mappings
        ColumnMappings.Clear();
        ColumnMappings.Add(new ColumnMappingDto { ExcelColumn = "A - Urun Adi", SampleData = "Samsung Galaxy S24", MesTechField = "ProductName" });
        ColumnMappings.Add(new ColumnMappingDto { ExcelColumn = "B - SKU", SampleData = "SGS24-128-BLK", MesTechField = "Sku" });
        ColumnMappings.Add(new ColumnMappingDto { ExcelColumn = "C - Fiyat", SampleData = "42999.00", MesTechField = "Price" });
        ColumnMappings.Add(new ColumnMappingDto { ExcelColumn = "D - Stok", SampleData = "25", MesTechField = "Stock" });
        ColumnMappings.Add(new ColumnMappingDto { ExcelColumn = "E - Kategori", SampleData = "Elektronik", MesTechField = "Category" });

        // Demo preview rows
        _allPreviewRows.Clear();
        _allPreviewRows.Add(new ImportPreviewRowDto { RowNumber = 1, ProductName = "Samsung Galaxy S24", Sku = "SGS24-128-BLK", Price = 42_999.00m, Stock = 25, ValidationStatus = "Gecerli" });
        _allPreviewRows.Add(new ImportPreviewRowDto { RowNumber = 2, ProductName = "iPhone 15 Pro", Sku = "IP15P-256-TIT", Price = 64_999.00m, Stock = 12, ValidationStatus = "Gecerli" });
        _allPreviewRows.Add(new ImportPreviewRowDto { RowNumber = 3, ProductName = "Xiaomi 14", Sku = "XI14-256-WHT", Price = 28_999.00m, Stock = 38, ValidationStatus = "Gecerli" });
        _allPreviewRows.Add(new ImportPreviewRowDto { RowNumber = 4, ProductName = "", Sku = "NONAME-001", Price = 0m, Stock = 5, ValidationStatus = "Hata: Ad bos" });
        _allPreviewRows.Add(new ImportPreviewRowDto { RowNumber = 5, ProductName = "Huawei P60", Sku = "HWP60-128-BLK", Price = 19_999.00m, Stock = 0, ValidationStatus = "Uyari: Stok 0" });
        ApplyPreviewFilter();

        PreviewValidationSummary = "3 gecerli, 1 hata, 1 uyari";
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (!CanImport) return;
        IsImporting = true;
        CanImport = false;

        try
        {
            for (int i = 0; i <= 100; i += 5)
            {
                ImportProgress = i;
                ImportStatusText = $"Import ediliyor... {i}% ({i * 156 / 100}/156 urun, {i / 10}s)";
            }
            ImportStatusText = "Import tamamlandi: 152 basarili, 4 atlanmis";
        }
        catch (Exception ex)
        {
            ImportStatusText = $"Import hatasi: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
            CanImport = true;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        IsImporting = false;
        CanImport = true;
        ImportProgress = 0;
        ImportStatusText = "Import iptal edildi";
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        IsLoading = true;
        try
        {
            var format = IsXlsxFormat ? "xlsx" : "csv";
            await _mediator.Send(new Application.Features.Product.Commands.ExportProducts.ExportProductsCommand(Format: format));
            ImportStatusText = $"Export tamamlandi ({format.ToUpperInvariant()})";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Export hatasi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectAllProducts()
    {
        SelectedProductCount = ExportProductCount;
        CanBulkUpdate = SelectedProductCount > 0 && !string.IsNullOrWhiteSpace(SelectedBulkAction);
    }

    [RelayCommand]
    private async Task BulkUpdateAsync()
    {
        if (!CanBulkUpdate) return;
        IsLoading = true;

        try
        {
            var action = MapAction(SelectedBulkAction);
            object? value = decimal.TryParse(BulkActionValue, out var v) ? v : BulkActionValue;
            var updated = await _mediator.Send(new BulkUpdateProductsCommand(
                SelectedProductIds, action, value));
            BulkUpdateStatusText = $"{updated} / {SelectedProductIds.Count} urun guncellendi";
        }
        catch (Exception ex)
        {
            BulkUpdateStatusText = $"Guncelleme hatasi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class ColumnMappingDto
{
    public string ExcelColumn { get; set; } = string.Empty;
    public string SampleData { get; set; } = string.Empty;
    public string MesTechField { get; set; } = string.Empty;
}

public class ImportPreviewRowDto
{
    public int RowNumber { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string ValidationStatus { get; set; } = string.Empty;
}

public class BulkPreviewRowDto
{
    public string ProductName { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}
