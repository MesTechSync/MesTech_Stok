using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesTechStok.Desktop.Views.Dropshipping;

public partial class DropshippingSupplierView : UserControl
{
    private readonly ObservableCollection<SupplierProductVm> _allProducts = new();
    private string? _currentSearch;

    // Designer constructor (D-11 pattern)
    public DropshippingSupplierView() : this(null) { }

    public DropshippingSupplierView(object? _ = null)
    {
        InitializeComponent();
        ProductGrid.ItemsSource = _allProducts;

        IsVisibleChanged += async (_, e) =>
        {
            if (e.NewValue is true && _allProducts.Count == 0)
                await LoadMockDataAsync();
        };
    }

    /// <summary>
    /// Tedarikçi adı ile açma — PoolView sağ-tık context menüsünden çağrılır.
    /// </summary>
    public DropshippingSupplierView(string supplierName) : this((object?)null)
    {
        TxtSupplierName.Text = supplierName;
        TxtSupplierCode.Text = $"KOD-{supplierName.GetHashCode() & 0xFFFF:X4}";
    }

    private async Task LoadMockDataAsync()
    {
        await Task.Delay(10); // UI thread yield

        // ── Skor Breakdown (mock) ──────────────────────────────────────
        var breakdown = new[]
        {
            new ScoreBreakdownVm("Stok Doğruluğu",      95m),
            new ScoreBreakdownVm("Güncelleme Sıklığı",  88m),
            new ScoreBreakdownVm("Fiyat İstikrarı",      75m),
            new ScoreBreakdownVm("Görsel Kalitesi",      82m),
            new ScoreBreakdownVm("Teslimat Hızı",        91m),
            new ScoreBreakdownVm("İade Oranı (düşük)",  70m),
        };
        ScoreBreakdownList.ItemsSource = breakdown;

        // ── Genel skor hesaplama ──────────────────────────────────────
        var overallScore = breakdown.Average(b => b.Score);
        UpdateScoreBadge(overallScore);

        // ── Import Geçmişi (mock) ─────────────────────────────────────
        var history = new[]
        {
            new ImportHistoryVm(DateTime.Today.AddDays(-1),  340, 338, 2),
            new ImportHistoryVm(DateTime.Today.AddDays(-4),  280, 280, 0),
            new ImportHistoryVm(DateTime.Today.AddDays(-8),  412, 405, 7),
            new ImportHistoryVm(DateTime.Today.AddDays(-12), 190, 188, 2),
            new ImportHistoryVm(DateTime.Today.AddDays(-17), 320, 320, 0),
            new ImportHistoryVm(DateTime.Today.AddDays(-22), 255, 248, 7),
            new ImportHistoryVm(DateTime.Today.AddDays(-28), 400, 395, 5),
        };
        HistoryGrid.ItemsSource = history;

        // ── Ürünler (mock) ────────────────────────────────────────────
        _allProducts.Clear();
        var products = new[]
        {
            new SupplierProductVm("Akıllı Saat X200",         "SAT-001", 1299.90m, 95m, DateTime.Today.AddDays(-1)),
            new SupplierProductVm("Bluetooth Kulaklık Pro",   "KLK-002",  449.00m, 82m, DateTime.Today.AddDays(-1)),
            new SupplierProductVm("USB-C Hub 7in1",           "USB-003",  299.90m, 67m, DateTime.Today.AddDays(-4)),
            new SupplierProductVm("Wireless Şarj Pad",        "SRJ-004",  189.00m, 38m, DateTime.Today.AddDays(-4)),
            new SupplierProductVm("Mekanik Klavye RGB",       "KLV-005",  699.00m, 91m, DateTime.Today.AddDays(-8)),
            new SupplierProductVm("Oyuncu Mouse 6400DPI",     "MOU-006",  349.00m, 85m, DateTime.Today.AddDays(-8)),
            new SupplierProductVm("USB Webcam 1080p",         "CAM-007",  599.00m, 78m, DateTime.Today.AddDays(-12)),
        };
        foreach (var p in products) _allProducts.Add(p);
        UpdateProductCount();
    }

    private void UpdateScoreBadge(decimal score)
    {
        TxtOverallScore.Text = $" {score:F0}";
        if (score >= 90)
        {
            TxtOverallScoreLabel.Text  = "Yeşil";
            ScoreBadge.Background      = new SolidColorBrush(Color.FromRgb(0x10, 0xb9, 0x81));
        }
        else if (score >= 70)
        {
            TxtOverallScoreLabel.Text  = "Sarı";
            ScoreBadge.Background      = new SolidColorBrush(Color.FromRgb(0xf5, 0x9e, 0x0b));
        }
        else if (score >= 50)
        {
            TxtOverallScoreLabel.Text  = "Turuncu";
            ScoreBadge.Background      = new SolidColorBrush(Color.FromRgb(0xf9, 0x73, 0x16));
        }
        else
        {
            TxtOverallScoreLabel.Text  = "Kırmızı";
            ScoreBadge.Background      = new SolidColorBrush(Color.FromRgb(0xef, 0x44, 0x44));
        }
    }

    private void UpdateProductCount()
    {
        var visible = string.IsNullOrEmpty(_currentSearch)
            ? _allProducts.Count
            : _allProducts.Count(p =>
                p.ProductName.ToLowerInvariant().Contains(_currentSearch) ||
                p.Sku.ToLowerInvariant().Contains(_currentSearch));
        TxtProductCount.Text = $"{visible} ürün";
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        => await LoadMockDataAsync();

    private void TxtProductSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentSearch = TxtProductSearch.Text.ToLowerInvariant();
        if (string.IsNullOrEmpty(_currentSearch))
        {
            ProductGrid.ItemsSource = _allProducts;
        }
        else
        {
            ProductGrid.ItemsSource = _allProducts.Where(p =>
                p.ProductName.ToLowerInvariant().Contains(_currentSearch) ||
                p.Sku.ToLowerInvariant().Contains(_currentSearch));
        }
        UpdateProductCount();
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
        await LoadMockDataAsync();
    }

    #endregion
}

// ── ViewModel'lar ─────────────────────────────────────────────────────────
public class ScoreBreakdownVm
{
    public string  Label { get; }
    public decimal Score { get; }

    public ScoreBreakdownVm(string label, decimal score)
    {
        Label = label;
        Score = score;
    }
}

public class ImportHistoryVm
{
    public DateTime ImportDate  { get; }
    public int      Processed   { get; }
    public int      Successful  { get; }
    public int      Failed      { get; }
    public bool     HasErrors   => Failed > 0;
    public string   StatusText  => Failed == 0 ? "Başarılı" : $"{Failed} hata";

    public ImportHistoryVm(DateTime date, int processed, int successful, int failed)
    {
        ImportDate = date;
        Processed  = processed;
        Successful = successful;
        Failed     = failed;
    }
}

public class SupplierProductVm
{
    public string   ProductName     { get; }
    public string   Sku             { get; }
    public decimal  PoolPrice       { get; }
    public decimal  ReliabilityScore { get; }
    public DateTime LastUpdated     { get; }

    public SupplierProductVm(
        string productName, string sku,
        decimal poolPrice, decimal reliabilityScore,
        DateTime lastUpdated)
    {
        ProductName      = productName;
        Sku              = sku;
        PoolPrice        = poolPrice;
        ReliabilityScore = reliabilityScore;
        LastUpdated      = lastUpdated;
    }
}
