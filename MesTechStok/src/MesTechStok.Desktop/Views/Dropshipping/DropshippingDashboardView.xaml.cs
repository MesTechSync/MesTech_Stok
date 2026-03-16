using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Dropshipping;

public partial class DropshippingDashboardView : UserControl
{
    public DropshippingDashboardView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Placeholder — wire to GetDropshippingDashboardQuery when available
        TxtPoolTotal.Text = "—";
        TxtFeeds.Text     = "—";
        TxtAvgScore.Text  = "—";
        TxtLastSync.Text  = "Son sync: bilinmiyor";
        TxtGreen.Text = TxtYellow.Text = TxtRed.Text = "—";
        TxtScoreColor.Text = "Veri bekleniyor";
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

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        HideAllStates();
        OnLoaded(this, new RoutedEventArgs());
    }

    #endregion
}
