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
        // TODO: DEV1 GetDropshippingDashboardQuery gelince inject et
        TxtPoolTotal.Text = "—";
        TxtFeeds.Text     = "—";
        TxtAvgScore.Text  = "—";
        TxtLastSync.Text  = "Son sync: bilinmiyor";
        TxtGreen.Text = TxtYellow.Text = TxtRed.Text = "—";
        TxtScoreColor.Text = "Veri bekleniyor";
    }

    private void BtnPool_Click(object sender, RoutedEventArgs e)   { /* TODO: Havuz navigation */ }
    private void BtnFeeds_Click(object sender, RoutedEventArgs e)  { /* TODO: Feed navigation */ }
    private void BtnExport_Click(object sender, RoutedEventArgs e) { /* TODO: İhracat navigation */ }
}
