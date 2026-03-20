using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// 4-step Import Wizard ViewModel for Avalonia.
/// Steps: 1) File Selection  2) Preview  3) Column Mapping  4) Import Progress
/// </summary>
public partial class ImportProductsAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    // Step tracking (1-4)
    [ObservableProperty] private int currentStep = 1;

    // Step visibility
    [ObservableProperty] private bool isStep1 = true;
    [ObservableProperty] private bool isStep2;
    [ObservableProperty] private bool isStep3;
    [ObservableProperty] private bool isStep4;

    // Navigation
    [ObservableProperty] private bool canGoBack;
    [ObservableProperty] private bool canGoNext;
    [ObservableProperty] private string nextButtonText = "Ileri";

    // Step indicator colors
    [ObservableProperty] private string step1Color = "#0078D4";
    [ObservableProperty] private string step1TextColor = "#0078D4";
    [ObservableProperty] private string step2Color = "#CBD5E1";
    [ObservableProperty] private string step2TextColor = "#94A3B8";
    [ObservableProperty] private string step3Color = "#CBD5E1";
    [ObservableProperty] private string step3TextColor = "#94A3B8";
    [ObservableProperty] private string step4Color = "#CBD5E1";
    [ObservableProperty] private string step4TextColor = "#94A3B8";

    // Step title
    [ObservableProperty] private string stepTitle = "Adim 1/4 — Dosya Secimi";

    // Loading
    [ObservableProperty] private bool isLoading;

    // Step 1: File Selection
    [ObservableProperty] private string selectedFilePath = string.Empty;
    [ObservableProperty] private string selectedFileName = string.Empty;
    [ObservableProperty] private string fileSizeText = string.Empty;
    [ObservableProperty] private bool hasFile;

    // Step 2: Preview
    [ObservableProperty] private int totalRowCount;
    [ObservableProperty] private int errorRowCount;
    [ObservableProperty] private bool hasPreviewErrors;
    [ObservableProperty] private string errorCountBackground = "#FEE2E2";
    [ObservableProperty] private string previewErrorSummary = string.Empty;

    public ObservableCollection<ImportPreviewRowDto> PreviewRows { get; } = [];

    // Step 3: Column Mapping
    public ObservableCollection<ImportColumnMappingDto> ColumnMappings { get; } = [];

    public ObservableCollection<string> TargetFields { get; } =
    [
        "(Atla)", "Name", "SKU", "Barcode", "Price", "Stock",
        "Category", "Brand", "Description", "Weight", "Color", "Size"
    ];

    // Step 4: Import Progress
    [ObservableProperty] private double importProgress;
    [ObservableProperty] private bool isImporting;
    [ObservableProperty] private bool importCompleted;
    [ObservableProperty] private int successCount;
    [ObservableProperty] private int skippedCount;
    [ObservableProperty] private int failedCount;
    [ObservableProperty] private string importDuration = string.Empty;
    [ObservableProperty] private bool hasImportErrors;

    public ObservableCollection<ImportErrorDto> ImportErrors { get; } = [];

    public ImportProductsAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task InitializeAsync()
    {
        UpdateStepState();
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SelectFileAsync()
    {
        // For now, simulate file selection with mock data.
        IsLoading = true;
        try
        {
            await Task.Delay(300);

            SelectedFilePath = "C:\\Data\\urun_listesi_2026.xlsx";
            SelectedFileName = "urun_listesi_2026.xlsx";
            FileSizeText = "(245 KB)";
            HasFile = true;

            // Auto-load preview data
            LoadPreviewData();
            LoadColumnMappings();

            UpdateStepState();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        if (CurrentStep > 1)
        {
            CurrentStep--;
            UpdateStepState();
        }
    }

    [RelayCommand]
    private async Task GoNextAsync()
    {
        if (CurrentStep == 4 && ImportCompleted)
        {
            // Reset wizard
            CurrentStep = 1;
            HasFile = false;
            SelectedFilePath = string.Empty;
            SelectedFileName = string.Empty;
            FileSizeText = string.Empty;
            ImportCompleted = false;
            IsImporting = false;
            ImportProgress = 0;
            PreviewRows.Clear();
            ColumnMappings.Clear();
            ImportErrors.Clear();
            UpdateStepState();
            return;
        }

        if (CurrentStep == 3)
        {
            // Start import on step 3 → 4 transition
            CurrentStep = 4;
            UpdateStepState();
            await RunImportAsync();
            return;
        }

        if (CurrentStep < 4)
        {
            CurrentStep++;
            UpdateStepState();
        }
    }

    private async Task RunImportAsync()
    {
        IsImporting = true;
        ImportCompleted = false;
        HasImportErrors = false;
        ImportProgress = 0;
        ImportErrors.Clear();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // await _mediator.Send(new CreateBulkProductsCommand { ... });

            // Simulate import progress
            var totalItems = TotalRowCount;
            for (int i = 1; i <= totalItems; i++)
            {
                await Task.Delay(50);
                ImportProgress = (double)i / totalItems * 100;
            }

            stopwatch.Stop();

            // Mock result summary
            SuccessCount = totalItems - 2;
            SkippedCount = 1;
            FailedCount = 1;
            ImportDuration = $"{stopwatch.Elapsed.TotalSeconds:F1} saniye";

            // Mock errors
            ImportErrors.Add(new ImportErrorDto { Message = "Satir 7: SKU 'TRY-ERR-001' zaten mevcut — atlandi" });
            ImportErrors.Add(new ImportErrorDto { Message = "Satir 9: Fiyat alani gecersiz format — '12.5abc'" });
            HasImportErrors = ImportErrors.Count > 0;
        }
        catch (Exception ex)
        {
            ImportErrors.Add(new ImportErrorDto { Message = $"Aktarma hatasi: {ex.Message}" });
            HasImportErrors = true;
            FailedCount = TotalRowCount;
        }
        finally
        {
            IsImporting = false;
            ImportCompleted = true;
            UpdateStepState();
        }
    }

    private void LoadPreviewData()
    {
        PreviewRows.Clear();
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 1, Status = "OK", ColumnA = "Samsung Galaxy S24", ColumnB = "TRY-ELK-001", ColumnC = "64999.00", ColumnD = "45", ColumnE = "Elektronik" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 2, Status = "OK", ColumnA = "Apple MacBook Air M3", ColumnB = "HB-BLG-002", ColumnC = "54999.00", ColumnD = "12", ColumnE = "Bilgisayar" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 3, Status = "OK", ColumnA = "Sony WH-1000XM5", ColumnB = "N11-AKS-003", ColumnC = "11499.00", ColumnD = "78", ColumnE = "Aksesuar" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 4, Status = "OK", ColumnA = "Logitech MX Master 3S", ColumnB = "TRY-AKS-004", ColumnC = "3299.00", ColumnD = "156", ColumnE = "Aksesuar" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 5, Status = "OK", ColumnA = "Dell U2723QE Monitor", ColumnB = "CS-MNT-005", ColumnC = "18799.00", ColumnD = "8", ColumnE = "Monitor" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 6, Status = "OK", ColumnA = "Dyson V15 Detect", ColumnB = "AMZ-GYM-006", ColumnC = "28990.00", ColumnD = "23", ColumnE = "Ev Aletleri" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 7, Status = "HATA", ColumnA = "Philips Airfryer XXL", ColumnB = "OC-EV-007", ColumnC = "", ColumnD = "0", ColumnE = "Ev Aletleri" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 8, Status = "OK", ColumnA = "Karaca Hatir Kahve", ColumnB = "TRY-GYM-008", ColumnC = "2199.00", ColumnD = "340", ColumnE = "Mutfak" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 9, Status = "HATA", ColumnA = "Vestel 55 Smart TV", ColumnB = "HB-KSA-009", ColumnC = "12.5abc", ColumnD = "5", ColumnE = "TV" });
        PreviewRows.Add(new ImportPreviewRowDto { RowNumber = 10, Status = "OK", ColumnA = "Nike Air Max 270", ColumnB = "N11-SPR-010", ColumnC = "4599.00", ColumnD = "67", ColumnE = "Spor" });

        TotalRowCount = 10;
        ErrorRowCount = 2;
        HasPreviewErrors = ErrorRowCount > 0;
        PreviewErrorSummary = "2 satirda hata tespit edildi: Satir 7 (bos fiyat), Satir 9 (gecersiz fiyat formati)";
    }

    private void LoadColumnMappings()
    {
        ColumnMappings.Clear();
        ColumnMappings.Add(new ImportColumnMappingDto { SourceColumn = "Kolon A (Urun Adi)", TargetField = "Name" });
        ColumnMappings.Add(new ImportColumnMappingDto { SourceColumn = "Kolon B (SKU)", TargetField = "SKU" });
        ColumnMappings.Add(new ImportColumnMappingDto { SourceColumn = "Kolon C (Fiyat)", TargetField = "Price" });
        ColumnMappings.Add(new ImportColumnMappingDto { SourceColumn = "Kolon D (Stok)", TargetField = "Stock" });
        ColumnMappings.Add(new ImportColumnMappingDto { SourceColumn = "Kolon E (Kategori)", TargetField = "Category" });
    }

    private void UpdateStepState()
    {
        IsStep1 = CurrentStep == 1;
        IsStep2 = CurrentStep == 2;
        IsStep3 = CurrentStep == 3;
        IsStep4 = CurrentStep == 4;

        CanGoBack = CurrentStep > 1 && !IsImporting;
        CanGoNext = CurrentStep switch
        {
            1 => HasFile,
            2 => true,
            3 => true,
            4 => ImportCompleted,
            _ => false
        };

        NextButtonText = CurrentStep switch
        {
            3 => "Aktarimi Baslat",
            4 when ImportCompleted => "Yeni Aktarma",
            _ => "Ileri"
        };

        StepTitle = CurrentStep switch
        {
            1 => "Adim 1/4 — Dosya Secimi",
            2 => "Adim 2/4 — On Izleme",
            3 => "Adim 3/4 — Kolon Eslestirme",
            4 => "Adim 4/4 — Aktarma",
            _ => string.Empty
        };

        // Update step indicator colors
        var active = "#0078D4";
        var done = "#16A34A";
        var inactive = "#CBD5E1";
        var activeText = "#0078D4";
        var doneText = "#16A34A";
        var inactiveText = "#94A3B8";

        Step1Color = CurrentStep > 1 ? done : active;
        Step1TextColor = CurrentStep > 1 ? doneText : activeText;
        Step2Color = CurrentStep > 2 ? done : CurrentStep == 2 ? active : inactive;
        Step2TextColor = CurrentStep > 2 ? doneText : CurrentStep == 2 ? activeText : inactiveText;
        Step3Color = CurrentStep > 3 ? done : CurrentStep == 3 ? active : inactive;
        Step3TextColor = CurrentStep > 3 ? doneText : CurrentStep == 3 ? activeText : inactiveText;
        Step4Color = CurrentStep == 4 ? active : inactive;
        Step4TextColor = CurrentStep == 4 ? activeText : inactiveText;
    }
}

public class ImportPreviewRowDto
{
    public int RowNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ColumnA { get; set; } = string.Empty;
    public string ColumnB { get; set; } = string.Empty;
    public string ColumnC { get; set; } = string.Empty;
    public string ColumnD { get; set; } = string.Empty;
    public string ColumnE { get; set; } = string.Empty;
}

public class ImportColumnMappingDto
{
    public string SourceColumn { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
}

public class ImportErrorDto
{
    public string Message { get; set; } = string.Empty;
}
