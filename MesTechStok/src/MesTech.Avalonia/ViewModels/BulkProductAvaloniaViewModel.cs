using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Product.Commands.BulkUpdateProducts;
using MesTech.Application.Features.Product.Commands.ExecuteBulkImport;
using MesTech.Avalonia.Services;
using MesTech.Domain.Enums;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Toplu Urun Islemleri ViewModel — Import / Export / Toplu Guncelle.
/// </summary>
public partial class BulkProductAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IFilePickerService _filePicker;
    private string? _selectedFilePath;

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

    public BulkProductAvaloniaViewModel(IMediator mediator, IFilePickerService filePicker)
    {
        _mediator = mediator;
        _filePicker = filePicker;
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

    private static readonly FilePickerFileType ExcelFileType = new("Excel Dosyalari")
    {
        Patterns = ["*.xlsx", "*.xls"],
        MimeTypes = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
    };

    private static readonly FilePickerFileType CsvFileType = new("CSV Dosyalari")
    {
        Patterns = ["*.csv"],
        MimeTypes = ["text/csv"]
    };

    [RelayCommand]
    private async Task SelectFileAsync()
    {
        var path = await _filePicker.PickFileAsync(
            "Import Dosyasi Sec",
            [ExcelFileType, CsvFileType]);

        if (string.IsNullOrEmpty(path)) return;

        _selectedFilePath = path;
        ImportFilePath = Path.GetFileName(path);
        IsLoading = true;
        HasError = false;

        try
        {
            var isCsv = path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
            if (isCsv)
                ParseCsvFile(path);
            else
                ParseExcelFile(path);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Dosya okunamadi: {ex.Message}";
            HasImportFile = false;
            CanImport = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ParseExcelFile(string path)
    {
        using var workbook = new XLWorkbook(path);
        var ws = workbook.Worksheets.First();
        var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 1;

        ImportFileInfo = $"Excel dosyasi: {lastRow - 1} satir, {lastCol} kolon";
        HasImportFile = true;
        CanImport = true;

        ColumnMappings.Clear();
        for (var col = 1; col <= lastCol; col++)
        {
            var header = ws.Cell(1, col).GetString();
            var sample = lastRow > 1 ? ws.Cell(2, col).GetString() : string.Empty;
            ColumnMappings.Add(new ColumnMappingDto
            {
                ExcelColumn = $"{GetColumnLetter(col)} - {header}",
                SampleData = sample,
                MesTechField = AutoMapField(header)
            });
        }

        LoadPreviewRows(
            lastRow - 1,
            row => ws.Cell(row + 1, FindColumn(ws, "ProductName", lastCol)).GetString(),
            row => ws.Cell(row + 1, FindColumn(ws, "Sku", lastCol)).GetString(),
            row => ws.Cell(row + 1, FindColumn(ws, "Price", lastCol)).GetString(),
            row => ws.Cell(row + 1, FindColumn(ws, "Stock", lastCol)).GetString());
    }

    private void ParseCsvFile(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0) return;

        var headers = lines[0].Split(';', ',');
        ImportFileInfo = $"CSV dosyasi: {lines.Length - 1} satir, {headers.Length} kolon";
        HasImportFile = true;
        CanImport = true;

        ColumnMappings.Clear();
        var separator = lines[0].Contains(';') ? ';' : ',';
        for (var col = 0; col < headers.Length; col++)
        {
            var sample = lines.Length > 1 ? GetCsvCell(lines[1], col, separator) : string.Empty;
            ColumnMappings.Add(new ColumnMappingDto
            {
                ExcelColumn = $"{GetColumnLetter(col + 1)} - {headers[col].Trim()}",
                SampleData = sample,
                MesTechField = AutoMapField(headers[col])
            });
        }

        var nameCol = FindCsvColumn("ProductName");
        var skuCol = FindCsvColumn("Sku");
        var priceCol = FindCsvColumn("Price");
        var stockCol = FindCsvColumn("Stock");

        LoadPreviewRows(
            lines.Length - 1,
            row => row < lines.Length ? GetCsvCell(lines[row], nameCol, separator) : string.Empty,
            row => row < lines.Length ? GetCsvCell(lines[row], skuCol, separator) : string.Empty,
            row => row < lines.Length ? GetCsvCell(lines[row], priceCol, separator) : string.Empty,
            row => row < lines.Length ? GetCsvCell(lines[row], stockCol, separator) : string.Empty);
    }

    private void LoadPreviewRows(int totalRows, Func<int, string> getName, Func<int, string> getSku,
        Func<int, string> getPrice, Func<int, string> getStock)
    {
        _allPreviewRows.Clear();
        int valid = 0, errors = 0, warnings = 0;
        for (var i = 1; i <= Math.Min(totalRows, 50); i++)
        {
            var name = getName(i);
            var sku = getSku(i);
            decimal.TryParse(getPrice(i), out var price);
            int.TryParse(getStock(i), out var stock);

            var status = "Gecerli";
            if (string.IsNullOrWhiteSpace(name)) { status = "Hata: Ad bos"; errors++; }
            else if (stock == 0) { status = "Uyari: Stok 0"; warnings++; }
            else valid++;

            _allPreviewRows.Add(new ImportPreviewRowDto
            {
                RowNumber = i, ProductName = name, Sku = sku,
                Price = price, Stock = stock, ValidationStatus = status
            });
        }
        ApplyPreviewFilter();
        PreviewValidationSummary = $"{valid} gecerli, {errors} hata, {warnings} uyari";
    }

    private static string GetCsvCell(string line, int col, char separator)
    {
        var cells = line.Split(separator);
        return col < cells.Length ? cells[col].Trim().Trim('"') : string.Empty;
    }

    private int FindCsvColumn(string fieldName)
    {
        for (var i = 0; i < ColumnMappings.Count; i++)
            if (ColumnMappings[i].MesTechField == fieldName) return i;
        return 0;
    }

    private static string AutoMapField(string header)
    {
        var h = header.Trim().ToLowerInvariant();
        if (h.Contains("ad") || h.Contains("name") || h.Contains("urun")) return "ProductName";
        if (h.Contains("sku") || h.Contains("kod") || h.Contains("code")) return "Sku";
        if (h.Contains("fiyat") || h.Contains("price") || h.Contains("tutar")) return "Price";
        if (h.Contains("stok") || h.Contains("stock") || h.Contains("adet")) return "Stock";
        if (h.Contains("kategori") || h.Contains("category")) return "Category";
        if (h.Contains("barkod") || h.Contains("barcode")) return "Barcode";
        return string.Empty;
    }

    private static string GetColumnLetter(int col) => col switch
    {
        <= 26 => ((char)('A' + col - 1)).ToString(),
        _ => $"{(char)('A' + col / 26 - 1)}{(char)('A' + col % 26 - 1)}"
    };

    private int FindColumn(IXLWorksheet ws, string fieldName, int lastCol)
    {
        var mapping = ColumnMappings.FirstOrDefault(m => m.MesTechField == fieldName);
        if (mapping is not null)
        {
            var idx = ColumnMappings.IndexOf(mapping);
            if (idx >= 0 && idx < lastCol) return idx + 1;
        }
        return 1;
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (!CanImport || string.IsNullOrEmpty(_selectedFilePath)) return;
        IsImporting = true;
        CanImport = false;
        ImportProgress = 0;

        try
        {
            await using var fileStream = File.OpenRead(_selectedFilePath);
            var result = await _mediator.Send(new ExecuteBulkImportCommand(
                fileStream,
                ImportFilePath,
                UpdateExisting: UpdateExistingProducts,
                SkipErrors: SkipErrors));

            ImportProgress = 100;
            ImportStatusText = $"Import tamamlandi: {result.ImportedCount} eklendi, {result.UpdatedCount} guncellendi, {result.SkippedCount} atlanmis, {result.ErrorCount} hata ({result.Duration.TotalSeconds:F1}s)";

            if (result.ImportedCount + result.UpdatedCount > 0)
                await LoadAsync();
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
    private Task Refresh() => LoadAsync();
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
