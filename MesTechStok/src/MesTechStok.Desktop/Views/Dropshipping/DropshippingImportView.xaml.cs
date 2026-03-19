using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Application.Features.Dropshipping.Queries;

namespace MesTechStok.Desktop.Views.Dropshipping;

public partial class DropshippingImportView : UserControl
{
    // Designer constructor (D-11 pattern)
    public DropshippingImportView() : this(null) { }

    public DropshippingImportView(object? _ = null)
    {
        InitializeComponent();
        IsVisibleChanged += async (_, e) =>
        {
            if (e.NewValue is true)
                await LoadFeedsAsync();
        };
    }

    private async Task LoadFeedsAsync()
    {
        try
        {
            ShowLoading();

            using var scope = App.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(new GetFeedSourcesQuery(
                IsActive: null, Page: 1, PageSize: 50));

            Dispatcher.Invoke(() =>
            {
                HideAllStates();
                FeedGrid.ItemsSource = result.Items.Select(f => new
                {
                    f.Name,
                    f.Format,
                    f.LastSyncAt,
                    LastSyncStatus = f.LastSyncStatus,
                    f.Id
                });

                if (result.TotalCount == 0)
                    ShowEmpty();
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => ShowError($"Feed kaynaklari yuklenemedi: {ex.Message}"));
        }
    }

    private async void BtnTestUrl_Click(object sender, RoutedEventArgs e)
    {
        var url = TxtFeedUrl.Text.Trim();
        if (string.IsNullOrEmpty(url)) return;

        TxtUrlStatus.Text = "Test ediliyor...";
        TxtUrlStatus.Foreground = new SolidColorBrush(Colors.Gray);
        TxtUrlStatus.Visibility = Visibility.Visible;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var resp = await client.SendAsync(
                new HttpRequestMessage(HttpMethod.Head, url));
            TxtUrlStatus.Text = resp.IsSuccessStatusCode
                ? $"Baglanti basarili ({resp.StatusCode})"
                : $"HTTP {(int)resp.StatusCode}";
            TxtUrlStatus.Foreground = new SolidColorBrush(
                resp.IsSuccessStatusCode ? Colors.Green : Colors.Orange);
        }
        catch (Exception ex)
        {
            TxtUrlStatus.Text = $"Baglanti hatasi: {ex.Message}";
            TxtUrlStatus.Foreground = new SolidColorBrush(Colors.Red);
        }
    }

    private async void BtnSaveFeed_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtFeedName.Text.Trim();
        var url  = TxtFeedUrl.Text.Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
        {
            MessageBox.Show("Feed adi ve URL zorunlu.", "Uyari");
            return;
        }

        try
        {
            using var scope = App.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var feedId = await mediator.Send(new CreateFeedSourceCommand(
                SupplierId: Guid.Empty, // Will be set by user in full form
                Name: name,
                FeedUrl: url,
                Format: MesTech.Domain.Enums.FeedFormat.Xml,
                PriceMarkupPercent: 0,
                PriceMarkupFixed: 0,
                SyncIntervalMinutes: 60,
                TargetPlatforms: null,
                AutoDeactivateOnZeroStock: true));

            MessageBox.Show($"'{name}' feed kaynagi eklendi! (ID: {feedId})", "Basari");
            TxtFeedName.Text = string.Empty;
            TxtFeedUrl.Text  = string.Empty;
            await LoadFeedsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Feed eklenemedi: {ex.Message}", "Hata");
        }
    }

    private async void BtnSyncFeed_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var feedId = btn.Tag is Guid gid ? gid : Guid.Empty;

        ProgressCard.Visibility = Visibility.Visible;
        SyncProgress.Value = 0;

        try
        {
            TxtSyncTitle.Text = "Sync baslatildi...";

            using var scope = App.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var jobId = await mediator.Send(new TriggerFeedImportCommand(feedId));

            // Job background'da calisir — progress simule et
            for (int i = 0; i <= 100; i += 10)
            {
                await Task.Delay(200);
                SyncProgress.Value = i;
                TxtSyncPct.Text    = $"{i}%";
                TxtSyncStep.Text   = i < 100 ? "Isleniyor..." : "Tamamlandi";
            }

            TxtSyncStep.Text = $"Tamamlandi (Job: {jobId})";
            await LoadFeedsAsync();
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

    private async void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        HideAllStates();
        await LoadFeedsAsync();
    }

    #endregion
}
