using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.DTOs;
using MesTech.Application.Queries.GetStoresByTenant;

namespace MesTechStok.Desktop.Views.Store
{
    public partial class StoreDetailView : UserControl
    {
        private Guid _storeId = Guid.Empty;

        public StoreDetailView()
        {
            InitializeComponent();
            Loaded += async (_, _) => await LoadDataAsync();
        }

        /// <summary>
        /// Set the store ID externally before navigation, then call refresh.
        /// </summary>
        public void SetStoreId(Guid storeId)
        {
            _storeId = storeId;
            _ = LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            if (_storeId == Guid.Empty) return;

            try
            {
                ShowLoading();

                using var scope = App.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var tenantProvider = scope.ServiceProvider.GetRequiredService<MesTech.Domain.Interfaces.ITenantProvider>();
                var tenantId = tenantProvider.GetCurrentTenantId();

                // Get all stores and find the one matching our ID
                var stores = await mediator.Send(new GetStoresByTenantQuery(tenantId));
                var store = stores.FirstOrDefault(s => s.Id == _storeId);

                Dispatcher.Invoke(() =>
                {
                    HideAllStates();
                    if (store == null)
                    {
                        ShowEmpty();
                        return;
                    }
                    DataContext = store;
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError($"Magaza detayi yuklenemedi: {ex.Message}"));
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
            await LoadDataAsync();
        }

        #endregion
    }
}
