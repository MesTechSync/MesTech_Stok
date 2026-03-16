using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Dropshipping;

public partial class DropshippingPoolView : UserControl
{
    private const decimal GREEN_THRESHOLD  = 90m;
    private const decimal YELLOW_THRESHOLD = 70m;
    private const decimal ORANGE_THRESHOLD = 50m;

    private readonly ObservableCollection<PoolProductViewModel> _items = new();
    private string? _colorFilter;
    private int _currentPage = 1;
    private const int PageSize = 50;
    private int _totalItems;

    // Designer constructor (D-11 pattern)
    public DropshippingPoolView() : this(null) { }

    public DropshippingPoolView(object? _ = null)
    {
        InitializeComponent();
        PoolGrid.ItemsSource = _items;
        // Load data when shown (not in constructor to avoid designer issues)
        IsVisibleChanged += async (_, e) =>
        {
            if (e.NewValue is true && _items.Count == 0)
                await LoadMockDataAsync();
        };
    }

    private async Task LoadMockDataAsync()
    {
        await Task.Delay(10); // UI thread yield
        _items.Clear();

        // Mock data — API entegrasyonu C-01 sonrası
        var mock = new[]
        {
            new PoolProductViewModel("Akıllı Saat X200", "SAT-001", 1299.90m, 45, 95m, "TechStore"),
            new PoolProductViewModel("Bluetooth Kulaklık Pro", "KLK-002", 449.00m, 120, 82m, "ElekSepet"),
            new PoolProductViewModel("USB-C Hub 7in1", "USB-003", 299.90m, 8, 67m, "TabletDünyası"),
            new PoolProductViewModel("Wireless Şarj Pad", "SRJ-004", 189.00m, 0, 38m, "GadgetStore"),
        };

        foreach (var item in mock) _items.Add(item);
        _totalItems = mock.Length;
        UpdatePaginationBar();
        BtnAll.Content = $"Tümü {_totalItems}";
    }

    private void UpdatePaginationBar()
    {
        var start = (_currentPage - 1) * PageSize + 1;
        var end   = Math.Min(_currentPage * PageSize, _totalItems);
        TxtPageInfo.Text = $"{start}–{end} / {_totalItems} ürün";
        TxtPage.Text     = $"Sayfa {_currentPage}";
        BtnPrev.IsEnabled = _currentPage > 1;
        BtnNext.IsEnabled = end < _totalItems;
    }

    private async void FilterChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            _colorFilter = btn.Tag?.ToString() == "ALL" ? null : btn.Tag?.ToString();
            _currentPage = 1;
            await LoadMockDataAsync();
        }
    }

    private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentPage = 1;
        await LoadMockDataAsync();
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        => await LoadMockDataAsync();

    private async void BtnPrev_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1) { _currentPage--; await LoadMockDataAsync(); }
    }

    private async void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        _currentPage++; await LoadMockDataAsync();
    }

    private void PoolGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => BtnBulkExport.IsEnabled = PoolGrid.SelectedItems.Count > 0;

    private void PoolGrid_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (PoolGrid.SelectedItem is PoolProductViewModel item)
            if (PoolGrid.ContextMenu != null)
                PoolGrid.ContextMenu.DataContext = item;
    }

    private void BtnPull_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid id)
            MessageBox.Show($"Ürün {id} stoğunuza ekleniyor… (API C-01 sonrası)", "Bilgi");
    }

    private void CtxPull_Click(object sender, RoutedEventArgs e)
    {
        if (PoolGrid.SelectedItem is PoolProductViewModel item)
            MessageBox.Show($"'{item.ProductName}' stoğunuza ekleniyor…", "Bilgi");
    }

    private void CtxExport_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("İhracat ekranı açılıyor… (Sprint D)", "Bilgi");

    private void CtxSupplier_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Tedarikçi profili açılıyor… (Sprint D)", "Bilgi");

    private void CtxRemove_Click(object sender, RoutedEventArgs e)
    {
        if (PoolGrid.SelectedItem is not PoolProductViewModel item) return;
        var confirm = MessageBox.Show(
            $"'{item.ProductName}' havuzdan kaldırılsın mı?",
            "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
            _items.Remove(item);
    }

    private void BtnBulkExport_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show($"{PoolGrid.SelectedItems.Count} ürün ihracat kuyruğuna alındı.", "Bilgi");

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

public class PoolProductViewModel
{
    public Guid Id { get; } = Guid.NewGuid();
    public string ProductName { get; }
    public string Sku { get; }
    public decimal CurrentPrice { get; }
    public int CurrentStock { get; }
    public decimal ReliabilityScore { get; }
    public string? SupplierName { get; }
    public string ReliabilityTooltip =>
        $"Skor: {ReliabilityScore:F0} | Stok: {CurrentStock}";

    public PoolProductViewModel(
        string productName, string sku, decimal price,
        int stock, decimal reliabilityScore, string? supplierName = null)
    {
        ProductName      = productName;
        Sku              = sku;
        CurrentPrice     = price;
        CurrentStock     = stock;
        ReliabilityScore = reliabilityScore;
        SupplierName     = supplierName;
    }
}
