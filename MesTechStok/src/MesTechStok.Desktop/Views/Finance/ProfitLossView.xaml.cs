using System.Windows;
using System.Windows.Controls;
using MesTechStok.Desktop.ViewModels.Finance;

namespace MesTechStok.Desktop.Views.Finance;

public partial class ProfitLossView : UserControl
{
    public ProfitLossView()
    {
        InitializeComponent();
        Loaded += ProfitLossView_Loaded;
    }

    private async void ProfitLossView_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            ShowLoading();

            if (DataContext is ProfitLossViewModel vm)
            {
                await vm.LoadAsync();

                if (vm.TotalRevenue == 0 && vm.TotalExpenses == 0)
                {
                    ShowEmpty();
                }
                else
                {
                    HideAllStates();
                }
            }
            else
            {
                // No ViewModel bound yet — show empty state
                ShowEmpty();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[ProfitLossView] LoadDataAsync error: {ex.Message}");
            ShowError($"Kar/zarar verisi yuklenirken hata olustu: {ex.Message}");
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
        await LoadDataAsync();
    }
    #endregion
}
