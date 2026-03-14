using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

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
                await LoadMockDataAsync();
        };
    }

    private List<PlatformItemVm> BuildPlatforms() => new()
    {
        new("Trendyol",    "🛍", "Trendyol"),
        new("Hepsiburada", "🏪", "Hepsiburada"),
        new("N11",         "🏷", "N11"),
        new("Ciceksepeti", "🌸", "ÇiçekSepeti"),
        new("Pazarama",    "🛒", "Pazarama"),
        new("Amazon",      "📦", "Amazon TR"),
        new("XML",         "📄", "XML Dosya"),
        new("CSV",         "📊", "CSV Dosya"),
        new("Excel",       "📗", "Excel"),
    };

    private async Task LoadMockDataAsync()
    {
        await Task.Delay(10); // UI thread yield
        _allProducts.Clear();

        // Mock data — MediatR entegrasyonu C-01 sonrası
        var mock = new[]
        {
            new ExportPoolProductVm(Guid.NewGuid(), "Akıllı Saat X200",         "SAT-001", 1299.90m, 45, 95m),
            new ExportPoolProductVm(Guid.NewGuid(), "Bluetooth Kulaklık Pro",   "KLK-002",  449.00m, 120, 82m),
            new ExportPoolProductVm(Guid.NewGuid(), "USB-C Hub 7in1",           "USB-003",  299.90m,  8, 67m),
            new ExportPoolProductVm(Guid.NewGuid(), "Wireless Şarj Pad",        "SRJ-004",  189.00m,  0, 38m),
            new ExportPoolProductVm(Guid.NewGuid(), "Mekanik Klavye RGB",       "KLV-005",  699.00m, 30, 91m),
        };

        foreach (var item in mock) _allProducts.Add(item);
        ExportGrid.ItemsSource = _allProducts;
        UpdateSelectionCount();
    }

    // ── Adım navigasyonu ──────────────────────────────────────────────
    private async void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep == 1)
        {
            if (!_allProducts.Any(p => p.IsSelected))
            {
                MessageBox.Show("En az 1 ürün seçin.", "Uyarı",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            GoToStep(2);
        }
        else if (_currentStep == 2)
        {
            if (!_platforms.Any(p => p.IsSelected))
            {
                MessageBox.Show("En az 1 platform seçin.", "Uyarı",
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
        BtnNext.Content   = step == 3 ? "Bitti" : "Devam →";
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

    // ── Özet render ──────────────────────────────────────────────────
    private void RenderSummary()
    {
        var selectedProducts  = _allProducts.Where(p => p.IsSelected).ToList();
        var selectedPlatforms = _platforms.Where(p => p.IsSelected)
            .Select(p => p.Name).ToList();

        SumProducts.Text  = $"{selectedProducts.Count} ürün";
        SumPlatforms.Text = string.Join(", ", selectedPlatforms);

        var method = ((ComboBoxItem)CmbMarkupMethod.SelectedItem)?.Tag?.ToString() ?? "percent";
        var value  = decimal.TryParse(TxtMarkupValue.Text, out var v) ? v : 0;
        SumMarkup.Text = method switch
        {
            "none"  => "Markup yok",
            "fixed" => $"+{value:N2} ₺ sabit",
            _       => $"%{value} yüzde"
        };
        SumKdv.Text = ChkKdv.IsChecked == true ? "Dahil" : "Hariç";
    }

    // ── Gönder aksiyonları ───────────────────────────────────────────
    private async void BtnSendToPlatform_Click(object sender, RoutedEventArgs e)
    {
        var selectedIds = _allProducts.Where(p => p.IsSelected)
            .Select(p => p.Id).ToList();
        var platforms = _platforms.Where(p => p.IsSelected
            && p.Code != "XML" && p.Code != "CSV" && p.Code != "Excel")
            .Select(p => p.Code).ToList();

        if (!platforms.Any())
        {
            MessageBox.Show("Dosya formatı değil, bir platform seçin.", "Uyarı",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ShowProgress("Platforma gönderiliyor…");

        await Task.Delay(500); // UI yield — gerçek impl MediatR C-01 sonrası

        int totalSent = 0, totalFailed = 0;
        var allErrors = new List<string>();

        for (int i = 0; i < platforms.Count; i++)
        {
            // Placeholder: MediatR ExportPoolProductsToPlatformCommand C-01 sonrası
            totalSent += selectedIds.Count;
            UpdateProgress(
                (i + 1) * 100 / platforms.Count,
                $"{platforms[i]}: {selectedIds.Count} gönderildi (mock)");
            await Task.Delay(200);
        }

        CompleteProgress($"Tamamlandı: {totalSent} ürün gönderildi, {totalFailed} hata");
    }

    private async void BtnDownloadXml_Click(object sender, RoutedEventArgs e)
    {
        ShowProgress("XML dosyası oluşturuluyor…");
        await Task.Delay(300);

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"mestech-export-{DateTime.Now:yyyyMMddHHmm}",
            DefaultExt = ".xml",
            Filter = "XML Files (*.xml)|*.xml"
        };
        if (dialog.ShowDialog() == true)
        {
            // Placeholder: MediatR ExportPoolProductsToXmlCommand C-01 sonrası
            await File.WriteAllTextAsync(dialog.FileName,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><products/>");
            CompleteProgress($"XML kaydedildi: {dialog.FileName}");
        }
    }

    private void BtnDownloadCsv_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("CSV export DEV3 Sprint D'de tamamlanıyor.", "Bilgi",
            MessageBoxButton.OK, MessageBoxImage.Information);

    private void BtnDownloadExcel_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Excel export DEV3 Sprint D'de tamamlanıyor.", "Bilgi",
            MessageBoxButton.OK, MessageBoxImage.Information);

    // ── Yardımcılar ──────────────────────────────────────────────────
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
        TxtSelectedCount.Text = $"{count} ürün seçildi";
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
        TxtMarkupValueLabel.Text = tag == "fixed" ? "   Tutar (₺):" : "   Oran (%):";
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
            "none"  => "Örnek: 100 ₺ → 100.00 ₺",
            "fixed" => $"Örnek: 100 ₺ → {100 + val:N2} ₺",
            _       => $"Örnek: 100 ₺ → {100 * (1 + val / 100):N2} ₺"
        };
    }
}

// ── ViewModel'lar ─────────────────────────────────────────────────────────
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
