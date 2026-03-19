using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Features.Dropshipping.Queries;

namespace MesTechStok.Desktop.Views.Dropshipping;

public partial class DropshippingDashboardView : UserControl
{
    public DropshippingDashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            ShowLoading();

            using var scope = App.Services.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var stats = await mediator.Send(new GetPoolDashboardStatsQuery());

            Dispatcher.Invoke(() =>
            {
                HideAllStates();
                TxtPoolTotal.Text = stats.TotalPoolProducts.ToString("N0");
                TxtFeeds.Text     = stats.ActiveFeedCount.ToString();
                TxtAvgScore.Text  = stats.AverageReliabilityScore.ToString("F0");
                TxtLastSync.Text  = stats.LastSyncAt.HasValue
                    ? $"Son sync: {stats.LastSyncAt.Value:dd.MM.yyyy HH:mm}"
                    : "Son sync: henuz yok";
                TxtGreen.Text  = stats.GreenCount.ToString();
                TxtYellow.Text = stats.YellowCount.ToString();
                TxtRed.Text    = stats.RedCount.ToString();
                TxtScoreColor.Text = stats.AverageReliabilityColor switch
                {
                    "Green"  => "Yesil",
                    "Yellow" => "Sari",
                    "Orange" => "Turuncu",
                    "Red"    => "Kirmizi",
                    _        => stats.AverageReliabilityColor
                };
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => ShowError($"Dashboard verileri yuklenemedi: {ex.Message}"));
        }
    }

    private void BtnPool_Click(object sender, RoutedEventArgs e)   { /* Stub: Havuz navigation */ }
    private void BtnFeeds_Click(object sender, RoutedEventArgs e)  { /* Stub: Feed navigation */ }
    private void BtnExport_Click(object sender, RoutedEventArgs e) { /* Stub: Ihracat navigation */ }

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
