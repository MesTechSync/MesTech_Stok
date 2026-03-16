using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Desktop.ViewModels.Documents;

namespace MesTechStok.Desktop.Views.Documents;

public partial class DocumentManagerView : UserControl
{
    public DocumentManagerView()
    {
        InitializeComponent();
        if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
        {
            DataContext = App.ServiceProvider!.GetRequiredService<DocumentManagerViewModel>();
            Loaded += async (_, _) => await ((DocumentManagerViewModel)DataContext).LoadAsync();
        }
    }

    #region Loading/Empty/Error State Helpers
    private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
    private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
    private void ShowError(string message) { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = message; }
    private void HideAllStates() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
    private void RetryButton_Click(object sender, RoutedEventArgs e) { HideAllStates(); }
    #endregion
}
