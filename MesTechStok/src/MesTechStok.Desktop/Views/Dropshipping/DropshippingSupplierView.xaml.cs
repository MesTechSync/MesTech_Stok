using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Features.Dropshipping.Queries;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipSuppliers;

namespace MesTechStok.Desktop.Views.Dropshipping;

public partial class DropshippingSupplierView : UserControl
{
    private readonly ObservableCollection<SupplierProductVm> _allProducts = new();
    private string? _currentSearch;
    private Guid _supplierFeedId = Guid.Empty;

    // Designer constructor (D-11 pattern)
    public DropshippingSupplierView() : this(null) { }

    public DropshippingSupplierView(object? _ = null)
    {
        InitializeComponent();
        ProductGrid.ItemsSource = _allProducts;

        IsVisibleChanged += async (_, e) =>
        {
            if (e.NewValue is true && _allProducts.Count == 0)
                await LoadDataAsync();
        };
    }

    /// <summary>
    /// Tedarikci adi ile acma — PoolView sag-tik context menusunden cagrilir.
    /// </summary>
    public DropshippingSupplierView(string supplierName) : this((object?)null)
    {
        TxtSupplierName.Text = supplierName;
        TxtSupplierCode.Text = $"KOD-{supplierName.GetHashCode() & 0xFFFF:X4}";
    }

    private async Task LoadDataAsync()
    {
        try
        {
            ShowLoading();

            using var scope = App.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // -- Skor Breakdown (reliability query) --
            if (_supplierFeedId != Guid.Empty)
            {
                var reliability = await mediator.Send(
                    new GetSupplierReliabilityQuery(_supplierFeedId));

                if (reliability != null)
                {
                    var breakdown = new[]
                    {
                        new ScoreBreakdownVm("Stok Dogrulugu",      reliability.StockAccuracy),
                        new ScoreBreakdownVm("Guncelleme Sikligi",  reliability.UpdateFrequency),
                        new ScoreBreakdownVm("Feed Erisilebilirlik", reliability.FeedAvailability),
                        new ScoreBreakdownVm("Urun Istikrari",      reliability.ProductStability),
                        new ScoreBreakdownVm("Yanit Suresi",        reliability.ResponseTime),
                    };
                    ScoreBreakdownList.ItemsSource = breakdown;
                    UpdateScoreBadge(reliability.Score);
                }
            }
            else
            {
                // No specific supplier — show placeholder scores
                ScoreBreakdownList.ItemsSource = Array.Empty<ScoreBreakdownVm>();
                UpdateScoreBadge(0);
            }

            // -- Import Gecmisi --
            if (_supplierFeedId != Guid.Empty)
            {
                var history = await mediator.Send(
                    new GetFeedImportHistoryQuery(_supplierFeedId, Page: 1, PageSize: 10));

                HistoryGrid.ItemsSource = history.Items.Select(h => new ImportHistoryVm(
                    h.StartedAt,
                    h.TotalProducts,
                    h.TotalProducts - (h.TotalProducts > 0 ? h.UpdatedProducts : 0),
                    h.TotalProducts > 0 ? h.UpdatedProducts : 0));
            }
            else
            {
                HistoryGrid.ItemsSource = Array.Empty<ImportHistoryVm>();
            }

            // -- Urunler (from pool products) --
            var poolProducts = await mediator.Send(new GetPoolProductsQuery(
                Page: 1, PageSize: 100));

            Dispatcher.Invoke(() =>
            {
                HideAllStates();
                _allProducts.Clear();

                foreach (var p in poolProducts.Items)
                {
                    _allProducts.Add(new SupplierProductVm(
                        p.ProductName, p.Sku, p.PoolPrice,
                        0m, p.LastUpdated));
                }

                UpdateProductCount();

                if (_allProducts.Count == 0)
                    ShowEmpty();
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => ShowError($"Tedarikci verileri yuklenemedi: {ex.Message}"));
        }
    }

    private void UpdateScoreBadge(decimal score)
    {
        TxtOverallScore.Text = $" {score:F0}";
        if (score >= 90)
        {
            TxtOverallScoreLabel.Text  = "Yesil";
            ScoreBadge.Background      = new SolidColorBrush(Color.FromRgb(0x10, 0xb9, 0x81));
        }
        else if (score >= 70)
        {
            TxtOverallScoreLabel.Text  = "Sari";
            ScoreBadge.Background      = new SolidColorBrush(Color.FromRgb(0xf5, 0x9e, 0x0b));
        }
        else if (score >= 50)
        {
            TxtOverallScoreLabel.Text  = "Turuncu";
            ScoreBadge.Background      = new SolidColorBrush(Color.FromRgb(0xf9, 0x73, 0x16));
        }
        else
        {
            TxtOverallScoreLabel.Text  = "Kirmizi";
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
        TxtProductCount.Text = $"{visible} urun";
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        => await LoadDataAsync();

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
        await LoadDataAsync();
    }

    #endregion
}

// -- ViewModel'lar ----------------------------------------------------------
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
    public string   StatusText  => Failed == 0 ? "Basarili" : $"{Failed} hata";

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
