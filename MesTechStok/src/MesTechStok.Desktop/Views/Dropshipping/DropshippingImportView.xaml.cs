using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesTechStok.Desktop.Views.Dropshipping;

public partial class DropshippingImportView : UserControl
{
    // Designer constructor (D-11 pattern)
    public DropshippingImportView() : this(null) { }

    public DropshippingImportView(object? _ = null)
    {
        InitializeComponent();
        IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true)
                LoadMockFeeds();
        };
    }

    private void LoadMockFeeds()
    {
        // Mock data — API entegrasyonu C-01 sonrası
        FeedGrid.ItemsSource = new[]
        {
            new { Name = "TechStore XML Feed", Format = "XML",
                  LastSyncAt = DateTime.Now.AddHours(-2),
                  LastSyncStatus = "Başarılı", Id = Guid.NewGuid() },
            new { Name = "ElekSepet CSV", Format = "CSV",
                  LastSyncAt = DateTime.Now.AddHours(-5),
                  LastSyncStatus = "Başarılı", Id = Guid.NewGuid() },
        };
    }

    private async void BtnTestUrl_Click(object sender, RoutedEventArgs e)
    {
        var url = TxtFeedUrl.Text.Trim();
        if (string.IsNullOrEmpty(url)) return;

        TxtUrlStatus.Text = "Test ediliyor…";
        TxtUrlStatus.Foreground = new SolidColorBrush(Colors.Gray);
        TxtUrlStatus.Visibility = Visibility.Visible;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var resp = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Head, url));
            TxtUrlStatus.Text = resp.IsSuccessStatusCode
                ? $"Bağlantı başarılı ({resp.StatusCode})"
                : $"HTTP {(int)resp.StatusCode}";
            TxtUrlStatus.Foreground = new SolidColorBrush(
                resp.IsSuccessStatusCode ? Colors.Green : Colors.Orange);
        }
        catch (Exception ex)
        {
            TxtUrlStatus.Text = $"Bağlantı hatası: {ex.Message}";
            TxtUrlStatus.Foreground = new SolidColorBrush(Colors.Red);
        }
    }

    private void BtnSaveFeed_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtFeedName.Text.Trim();
        var url  = TxtFeedUrl.Text.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Feed adı ve URL zorunlu.", "Uyarı");
            return;
        }

        MessageBox.Show($"'{name}' feed kaynağı eklendi! (API C-01 sonrası persist edilecek)", "Başarı");
        TxtFeedName.Text = string.Empty;
        TxtFeedUrl.Text  = string.Empty;
    }

    private async void BtnSyncFeed_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button) return;

        ProgressCard.Visibility = Visibility.Visible;
        SyncProgress.Value = 0;

        try
        {
            TxtSyncTitle.Text = "Sync başlatıldı…";

            for (int i = 0; i <= 100; i += 10)
            {
                await Task.Delay(200);
                SyncProgress.Value = i;
                TxtSyncPct.Text    = $"{i}%";
                TxtSyncStep.Text   = i < 100 ? "İşleniyor…" : "Tamamlandı";
            }
        }
        catch (Exception ex)
        {
            TxtSyncStep.Text = $"Hata: {ex.Message}";
        }
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

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        HideAllStates();
        LoadMockFeeds();
    }

    #endregion
}
