using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MesTech.Avalonia.Views;

public partial class InventoryAvaloniaView : UserControl
{
    public InventoryAvaloniaView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        RetryButton.Click += OnRetryClick;
        RetryErrorButton.Click += OnRetryClick;
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }

    private async void OnRetryClick(object? sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        ShowLoading();
        try
        {
            await Task.Delay(100); // Simulate async load
            // TODO: Wire to MediatR query when full migration starts
            HideAllStates();
        }
        catch (Exception ex)
        {
            ShowError($"Envanter yuklenemedi: {ex.Message}");
        }
    }

    #region Loading/Empty/Error State Helpers

    private void ShowLoading()
    {
        LoadingOverlay.IsVisible = true;
        EmptyState.IsVisible = false;
        ErrorState.IsVisible = false;
    }

    private void ShowEmpty()
    {
        LoadingOverlay.IsVisible = false;
        EmptyState.IsVisible = true;
        ErrorState.IsVisible = false;
    }

    private void ShowError(string message)
    {
        LoadingOverlay.IsVisible = false;
        EmptyState.IsVisible = false;
        ErrorState.IsVisible = true;
        ErrorText.Text = message;
    }

    private void HideAllStates()
    {
        LoadingOverlay.IsVisible = false;
        EmptyState.IsVisible = false;
        ErrorState.IsVisible = false;
    }

    #endregion
}
