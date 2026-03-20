using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Features.Platform.Commands.TestStoreConnection;

namespace MesTechStok.Desktop.Views.Store
{
    public partial class StoreTestView : UserControl
    {
        private Guid _storeId = Guid.Empty;

        public StoreTestView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set the store ID externally, then call TestConnectionAsync.
        /// </summary>
        public void SetStoreId(Guid storeId)
        {
            _storeId = storeId;
        }

        // Wire BtnTest_Click from XAML to this handler
        private async Task TestConnectionAsync()
        {
            if (_storeId == Guid.Empty) return;

            try
            {
                ShowLoading();

                using var scope = App.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var result = await mediator.Send(new TestStoreConnectionCommand(_storeId));

                Dispatcher.Invoke(() =>
                {
                    HideAllStates();
                    if (result.IsSuccess)
                    {
                        MessageBox.Show(
                            $"Baglanti basarili!\nPlatform: {result.PlatformCode}\nMagaza: {result.StoreName}\nUrun: {result.ProductCount}\nSure: {result.ResponseTime.TotalMilliseconds:F0}ms",
                            "Test Sonucu", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ShowError($"Baglanti basarisiz: {result.ErrorMessage} (HTTP {result.HttpStatusCode})");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError($"Test hatasi: {ex.Message}"));
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
            await TestConnectionAsync();
        }

        #endregion
    }
}
