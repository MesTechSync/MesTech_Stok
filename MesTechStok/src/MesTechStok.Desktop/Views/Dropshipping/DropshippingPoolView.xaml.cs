using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Features.Dropshipping.Queries;
using MesTech.Application.Interfaces;

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
                await LoadDataAsync();
        };
    }

    private async Task LoadDataAsync()
    {
        try
        {
            ShowLoading();

            using var scope = App.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            ReliabilityColor? colorEnum = _colorFilter switch
            {
                "GREEN"  => ReliabilityColor.Green,
                "YELLOW" => ReliabilityColor.Yellow,
                "ORANGE" => ReliabilityColor.Orange,
                "RED"    => ReliabilityColor.Red,
                _        => null
            };

            var searchText = TxtSearch?.Text?.Trim();

            var result = await mediator.Send(new GetPoolProductsQuery(
                PoolId: null,
                ColorFilter: colorEnum,
                Search: string.IsNullOrEmpty(searchText) ? null : searchText,
                Page: _currentPage,
                PageSize: PageSize));

            Dispatcher.Invoke(() =>
            {
                HideAllStates();
                _items.Clear();
                foreach (var dto in result.Items)
                {
                    _items.Add(new PoolProductViewModel(
                        dto.ProductName, dto.Sku, dto.PoolPrice,
                        0, 0m, dto.SupplierInfo)
                    { CqrsId = dto.Id });
                }

                _totalItems = result.TotalCount;
                UpdatePaginationBar();
                BtnAll.Content = $"Tumu {_totalItems}";

                if (_totalItems == 0)
                    ShowEmpty();
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => ShowError($"Havuz verileri yuklenemedi: {ex.Message}"));
        }
    }

    private void UpdatePaginationBar()
    {
        var start = (_currentPage - 1) * PageSize + 1;
        var end   = Math.Min(_currentPage * PageSize, _totalItems);
        TxtPageInfo.Text = $"{start}-{end} / {_totalItems} urun";
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
            await LoadDataAsync();
        }
    }

    private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        _currentPage = 1;
        await LoadDataAsync();
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        => await LoadDataAsync();

    private async void BtnPrev_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1) { _currentPage--; await LoadDataAsync(); }
    }

    private async void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        _currentPage++; await LoadDataAsync();
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
            MessageBox.Show($"Urun {id} stogunuza ekleniyor...", "Bilgi");
    }

    private void CtxPull_Click(object sender, RoutedEventArgs e)
    {
        if (PoolGrid.SelectedItem is PoolProductViewModel item)
            MessageBox.Show($"'{item.ProductName}' stogunuza ekleniyor...", "Bilgi");
    }

    private void CtxExport_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Ihracat ekrani aciliyor...", "Bilgi");

    private void CtxSupplier_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Tedarikci profili aciliyor...", "Bilgi");

    private void CtxRemove_Click(object sender, RoutedEventArgs e)
    {
        if (PoolGrid.SelectedItem is not PoolProductViewModel item) return;
        var confirm = MessageBox.Show(
            $"'{item.ProductName}' havuzdan kaldirilsin mi?",
            "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm == MessageBoxResult.Yes)
            _items.Remove(item);
    }

    private void BtnBulkExport_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show($"{PoolGrid.SelectedItems.Count} urun ihracat kuyruguna alindi.", "Bilgi");

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

public class PoolProductViewModel
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid CqrsId { get; set; }
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
