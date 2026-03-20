using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Features.Platform.Commands.CreateStore;
using MesTech.Domain.Enums;

namespace MesTechStok.Desktop.Views.Store
{
    public partial class StoreCreateView : UserControl
    {
        public StoreCreateView()
        {
            InitializeComponent();
        }

        // Wire BtnSave_Click from XAML to this handler when form fields are available
        private async Task SaveStoreAsync(string storeName, PlatformType platformType, Dictionary<string, string> credentials)
        {
            try
            {
                ShowLoading();

                using var scope = App.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var tenantProvider = scope.ServiceProvider.GetRequiredService<MesTech.Domain.Interfaces.ITenantProvider>();
                var tenantId = tenantProvider.GetCurrentTenantId();

                var result = await mediator.Send(new CreateStoreCommand(
                    tenantId, storeName, platformType, credentials));

                Dispatcher.Invoke(() =>
                {
                    HideAllStates();
                    if (result.IsSuccess)
                    {
                        MessageBox.Show($"Magaza olusturuldu! (ID: {result.StoreId})", "Basari",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        ShowError(result.ErrorMessage ?? "Magaza olusturulamadi.");
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError($"Magaza olusturulamadi: {ex.Message}"));
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
        }

        #endregion
    }
}
