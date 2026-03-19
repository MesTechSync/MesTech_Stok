using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Crm;

public partial class CrmDashboardView : UserControl
{
    public CrmDashboardView()
    {
        InitializeComponent();
    }

    #region Loading/Error State Helpers

    private void ShowLoading()
    {
        LoadingOverlay.Visibility = Visibility.Visible;
        ErrorState.Visibility = Visibility.Collapsed;
    }

    private void ShowError(string message)
    {
        LoadingOverlay.Visibility = Visibility.Collapsed;
        ErrorState.Visibility = Visibility.Visible;
        ErrorMessage.Text = message;
    }

    private void HideAllStates()
    {
        LoadingOverlay.Visibility = Visibility.Collapsed;
        ErrorState.Visibility = Visibility.Collapsed;
    }

    private void RetryButton_Click(object sender, RoutedEventArgs e)
    {
        HideAllStates();
    }

    #endregion
}
