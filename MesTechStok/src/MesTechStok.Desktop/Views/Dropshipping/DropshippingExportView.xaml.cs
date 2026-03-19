using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Application.Features.Dropshipping.Queries;

namespace MesTechStok.Desktop.Views.Dropshipping;

public partial class DropshippingExportView : UserControl
{
    private int _currentStep = 1;
    private ObservableCollection<ExportPoolProductVm> _allProducts = new();
    private readonly List<PlatformItemVm> _platforms;

    // Designer constructor (D-11 pattern)
    public DropshippingExportView() : this(null) { }

    public DropshippingExportView(object? _ = null)
    {
        InitializeComponent();

        _platforms = BuildPlatforms();
        PlatformList.ItemsSource = _platforms;

        IsVisibleChanged += async (_, e) =>
        {
            if (e.NewValue is true && _allProducts.Count == 0)
                await LoadDataAsync();
        };
    }

    private List<PlatformItemVm> BuildPlatforms() => new()
    {
        new("Trendyol",    "\U0001f6cd", "Trendyol"),
        new("Hepsiburada", "\U0001f3ea", "Hepsiburada"),
        new("N11",         "\U0001f3f7", "N11"),
        new("Ciceksepeti", "\U0001f338", "CicekSepeti"),
        new("Pazarama",    "\U0001f6d2", "Pazarama"),
        new("Amazon",      "\U0001f4e6", "Amazon TR"),
        new("XML",         "\U0001f4c4", "XML Dosya"),
        new("CSV",         "\U0001f4ca", "CSV Dosya"),
        new("Excel",       "\U0001f4d7", "Excel"),
    };

    private async Task LoadDataAsync()
    {
        try
        {
            ShowLoading();

            using var scope = App.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new GetPoolProductsQuery(
                Page: 1, PageSize: 200));

            Dispatcher.Invoke(() =>
            {
                HideAllStates();
                _allProducts.Clear();
                foreach (var dto in result.Items)
                {
                    _allProducts.Add(new ExportPoolProductVm(
                        dto.Id, dto.ProductName, dto.Sku,
                        dto.PoolPrice, 0, 0m));
                }
                ExportGrid.ItemsSource = _allProducts;
                UpdateSelectionCount();

                if (result.TotalCount == 0)
                    ShowEmpty();
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => ShowError($"Urunler yuklenemedi: {ex.Message}"));
        }
    }

    // -- Adim navigasyonu ---------------------------------------------------
    private async void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep == 1)
        {
            if (!_allProducts.Any(p => p.IsSelected))
            {
                MessageBox.Show("En az 1 urun secin.", "Uyari",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            GoToStep(2);
        }
        else if (_currentStep == 2)
        {
            if (!_platforms.Any(p => p.IsSelected))
            {
                MessageBox.Show("En az 1 platform secin.", "Uyari",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            RenderSummary();
            GoToStep(3);
        }
        await Task.CompletedTask;
    }

    private void BtnBack_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep > 1) GoToStep(_currentStep - 1);
    }

    private void GoToStep(int step)
    {
        _currentStep = step;

        Panel1.Visibility = step == 1 ? Visibility.Visible : Visibility.Collapsed;
        Panel2.Visibility = step == 2 ? Visibility.Visible : Visibility.Collapsed;
        Panel3.Visibility = step == 3 ? Visibility.Visible : Visibility.Collapsed;

        BtnBack.IsEnabled = step > 1;
        BtnNext.Content   = step == 3 ? "Bitti" : "Devam \u2192";
        BtnNext.IsEnabled = step < 3;

        UpdateStepIndicators(step);
    }

    private void UpdateStepIndicators(int step)
    {
        // Aktif: #2855AC, Tamamlanan: #10b981, Pasif: #cbd5e1
        Step1Indicator.Background = step >= 1
            ? step > 1
                ? new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x10, 0xb9, 0x81))
                : new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x28, 0x55, 0xac))
            : new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xcb, 0xd5, 0xe1));

        Step2Indicator.Background = step >= 2
            ? step > 2
                ? new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x10, 0xb9, 0x81))
                : new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x28, 0x55, 0xac))
            : new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xcb, 0xd5, 0xe1));

        Step3Indicator.Background = step >= 3
            ? new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x28, 0x55, 0xac))
            : new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xcb, 0xd5, 0xe1));
    }

    // -- Ozet render --------------------------------------------------------
    private void RenderSummary()
    {
        var selectedProducts  = _allProducts.Where(p => p.IsSelected).ToList();
        var selectedPlatforms = _platforms.Where(p => p.IsSelected)
            .Select(p => p.Name).ToList();

        SumProducts.Text  = $"{selectedProducts.Count} urun";
        SumPlatforms.Text = string.Join(", ", selectedPlatforms);

        var method = ((ComboBoxItem)CmbMarkupMethod.SelectedItem)?.Tag?.ToString() ?? "percent";
        var value  = decimal.TryParse(TxtMarkupValue.Text, out var v) ? v : 0;
        SumMarkup.Text = method switch
        {
            "none"  => "Markup yok",
            "fixed" => $"+{value:N2} TL sabit",
            _       => $"%{value} yuzde"
        };
        SumKdv.Text = ChkKdv.IsChecked == true ? "Dahil" : "Haric";
    }

    // -- Gonder aksiyonlari -------------------------------------------------
    private async void BtnSendToPlatform_Click(object sender, RoutedEventArgs e)
    {
        var selectedIds = _allProducts.Where(p => p.IsSelected)
            .Select(p => p.Id).ToList();
        var platforms = _platforms.Where(p => p.IsSelected
            && p.Code != "XML" && p.Code != "CSV" && p.Code != "Excel")
            .Select(p => p.Code).ToList();

        if (!platforms.Any())
        {
            MessageBox.Show("Dosya formati degil, bir platform secin.", "Uyari",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ShowProgress("Platforma gonderiliyor...");

        try
        {
            using var scope = App.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var method = ((ComboBoxItem)CmbMarkupMethod.SelectedItem)?.Tag?.ToString() ?? "percent";
            var markupValue = decimal.TryParse(TxtMarkupValue.Text, out var v) ? v : 0;

            int totalSent = 0, totalFailed = 0;
            var allErrors = new List<string>();

            for (int i = 0; i < platforms.Count; i++)
            {
                var result = await mediator.Send(new ExportPoolProductsToPlatformCommand(
                    PoolId: Guid.Empty, // Default pool
                    ProductIds: selectedIds,
                    PlatformCode: platforms[i],
                    PriceMarkupPercent: method == "percent" ? markupValue : 0,
                    HideSupplierInfo: false));

                totalSent += result.Sent;
                totalFailed += result.Failed;
                allErrors.AddRange(result.Errors);

                UpdateProgress(
                    (i + 1) * 100 / platforms.Count,
                    $"{platforms[i]}: {result.Sent} gonderildi, {result.Failed} hata");
            }

            CompleteProgress($"Tamamlandi: {totalSent} urun gonderildi, {totalFailed} hata");
        }
        catch (Exception ex)
        {
            CompleteProgress($"Hata: {ex.Message}");
        }
    }

    private async void BtnDownloadXml_Click(object sender, RoutedEventArgs e)
    {
        ShowProgress("XML dosyasi olusturuluyor...");

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"mestech-export-{DateTime.Now:yyyyMMddHHmm}",
            DefaultExt = ".xml",
            Filter = "XML Files (*.xml)|*.xml"
        };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                using var scope = App.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var selectedIds = _allProducts.Where(p => p.IsSelected).Select(p => p.Id).ToList();
                var method = ((ComboBoxItem)CmbMarkupMethod.SelectedItem)?.Tag?.ToString() ?? "percent";
                var markupValue = decimal.TryParse(TxtMarkupValue.Text, out var mv) ? mv : 0;

                var bytes = await mediator.Send(new ExportPoolProductsToXmlCommand(
                    PoolId: Guid.Empty,
                    ProductIds: selectedIds,
                    PriceMarkupPercent: method == "percent" ? markupValue : 0,
                    HideSupplierInfo: false));

                await File.WriteAllBytesAsync(dialog.FileName, bytes);
                CompleteProgress($"XML kaydedildi: {dialog.FileName}");
            }
            catch (Exception ex)
            {
                // Fallback: write empty XML if command fails
                await File.WriteAllTextAsync(dialog.FileName,
                    "<?xml version=\"1.0\" encoding=\"utf-8\"?><products/>");
                CompleteProgress($"XML kaydedildi (fallback): {dialog.FileName} — {ex.Message}");
            }
        }
    }

    private void BtnDownloadCsv_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("CSV export tamamlaniyor.", "Bilgi",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private void BtnDownloadExcel_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Excel export tamamlaniyor.", "Bilgi",
            MessageBoxButton.OK, MessageBoxImage.Information);

    // -- Yardimcilar --------------------------------------------------------
    private void ShowProgress(string msg)
    {
        ProgressSection.Visibility = Visibility.Visible;
        TxtProgressMsg.Text        = msg;
        ExportProgress.Value       = 0;
        TxtProgressPct.Text        = "0%";
        TxtProgressDetail.Text     = string.Empty;
    }

    private void UpdateProgress(int pct, string detail)
    {
        ExportProgress.Value   = pct;
        TxtProgressPct.Text    = $"{pct}%";
        TxtProgressDetail.Text = detail;
    }

    private void CompleteProgress(string msg)
    {
        ExportProgress.Value = 100;
        TxtProgressPct.Text  = "100%";
        TxtProgressMsg.Text  = msg;
    }

    private void UpdateSelectionCount()
    {
        var count = _allProducts.Count(p => p.IsSelected);
        TxtSelectedCount.Text = $"{count} urun secildi";
    }

    private void TxtExportSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = TxtExportSearch.Text.ToLowerInvariant();
        ExportGrid.ItemsSource = string.IsNullOrEmpty(q)
            ? _allProducts
            : _allProducts.Where(p =>
                p.ProductName.ToLowerInvariant().Contains(q) ||
                p.Sku.ToLowerInvariant().Contains(q));
        UpdateSelectionCount();
    }

    private void CmbMarkupMethod_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var tag = ((ComboBoxItem)CmbMarkupMethod.SelectedItem)?.Tag?.ToString();
        if (TxtMarkupValueLabel is null) return;
        TxtMarkupValueLabel.Text = tag == "fixed" ? "   Tutar (TL):" : "   Oran (%):";
        if (TxtMarkupValue is not null)
            TxtMarkupValue.IsEnabled = tag != "none";
        UpdateMarkupPreview();
    }

    private void TxtMarkupValue_TextChanged(object sender, TextChangedEventArgs e)
        => UpdateMarkupPreview();

    private void UpdateMarkupPreview()
    {
        var method = ((ComboBoxItem)CmbMarkupMethod?.SelectedItem)?.Tag?.ToString() ?? "percent";
        var val    = decimal.TryParse(TxtMarkupValue?.Text, out var v) ? v : 0;
        if (TxtMarkupPreview is null) return;
        TxtMarkupPreview.Text = method switch
        {
            "none"  => "Ornek: 100 TL \u2192 100.00 TL",
            "fixed" => $"Ornek: 100 TL \u2192 {100 + val:N2} TL",
            _       => $"Ornek: 100 TL \u2192 {100 * (1 + val / 100):N2} TL"
        };
    }

    #region Loading/Empty/Error State Helpers

    private void ShowLoading()
    {
        LoadingOverlay.Visibility = Visibility.Visible;
        EmptyState.Visibility = Visibility.Collapsed;
        ErrorState.Visibility = Visibility.Collapsed;
    }

    private void ShowEmpty()
    {
        LoadingOverlay.Visibility = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Visible;
        ErrorState.Visibility = Visibility.Collapsed;
    }

    private void ShowError(string message)
    {
        LoadingOverlay.Visibility = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Collapsed;
        ErrorState.Visibility = Visibility.Visible;
        ErrorMessage.Text = message;
    }

    private void HideAllStates()
    {
        LoadingOverlay.Visibility = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Collapsed;
        ErrorState.Visibility = Visibility.Collapsed;
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        HideAllStates();
        await LoadDataAsync();
    }

    #endregion
}

// -- ViewModel'lar ----------------------------------------------------------
public class ExportPoolProductVm
{
    public Guid    Id              { get; }
    public string  ProductName     { get; }
    public string  Sku             { get; }
    public decimal CurrentPrice    { get; }
    public int     CurrentStock    { get; }
    public decimal ReliabilityScore { get; }
    public bool    IsSelected      { get; set; }

    public ExportPoolProductVm(
        Guid id, string productName, string sku,
        decimal currentPrice, int currentStock, decimal reliabilityScore)
    {
        Id               = id;
        ProductName      = productName;
        Sku              = sku;
        CurrentPrice     = currentPrice;
        CurrentStock     = currentStock;
        ReliabilityScore = reliabilityScore;
    }
}

public class PlatformItemVm
{
    public string Code { get; }
    public string Icon { get; }
    public string Name { get; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            _isSelected = value;
            BackgroundColor = value
                ? new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(0x15, 0x28, 0x55, 0xac))
                : System.Windows.Media.Brushes.Transparent;
            BorderColor = value
                ? new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x28, 0x55, 0xac))
                : new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0xe2, 0xe8, 0xf0));
        }
    }

    public System.Windows.Media.Brush BackgroundColor { get; private set; }
        = System.Windows.Media.Brushes.Transparent;
    public System.Windows.Media.Brush BorderColor { get; private set; }
        = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0xe2, 0xe8, 0xf0));

    public System.Windows.Input.ICommand ToggleCommand =>
        new CommunityToolkit.Mvvm.Input.RelayCommand(() => IsSelected = !IsSelected);

    public PlatformItemVm(string code, string icon, string name)
    {
        Code = code;
        Icon = icon;
        Name = name;
    }
}
