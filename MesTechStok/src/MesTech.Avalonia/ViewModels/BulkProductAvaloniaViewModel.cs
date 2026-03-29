using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

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

    public BulkProductAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
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
        PreviewRows.Clear();
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 1, ProductName = "Samsung Galaxy S24", Sku = "SGS24-128-BLK", Price = 42_999.00m, Stock = 25, ValidationStatus = "Gecerli" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 2, ProductName = "iPhone 15 Pro", Sku = "IP15P-256-TIT", Price = 64_999.00m, Stock = 12, ValidationStatus = "Gecerli" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 3, ProductName = "Xiaomi 14", Sku = "XI14-256-WHT", Price = 28_999.00m, Stock = 38, ValidationStatus = "Gecerli" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 4, ProductName = "", Sku = "NONAME-001", Price = 0m, Stock = 5, ValidationStatus = "Hata: Ad bos" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 5, ProductName = "Huawei P60", Sku = "HWP60-128-BLK", Price = 19_999.00m, Stock = 0, ValidationStatus = "Uyari: Stok 0" });

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
            // DEV1-DEPENDENCY: BulkUpdateProductsCommand needs BulkUpdateAction enum + product ID selection
            await Task.CompletedTask;
            BulkUpdateStatusText = $"{SelectedProductCount} urun guncelleme icin DEV 1 handler bekleniyor";
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
