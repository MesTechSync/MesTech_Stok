using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.DTOs;
using MesTech.Application.Queries.GetStoresByTenant;
using MesTech.Domain.Enums;

namespace MesTechStok.Desktop.Views.OpenCart
{
    public partial class OpenCartSitesView : UserControl
    {
        private readonly ObservableCollection<StoreDto> _stores = new();

        public OpenCartSitesView()
        {
            InitializeComponent();
            Loaded += async (_, _) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                ShowLoading();

                using var scope = App.Services.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var tenantProvider = scope.ServiceProvider.GetRequiredService<MesTech.Domain.Interfaces.ITenantProvider>();
                var tenantId = tenantProvider.GetCurrentTenantId();

                var allStores = await mediator.Send(new GetStoresByTenantQuery(tenantId));
                var ocStores = allStores.Where(s => s.PlatformType == PlatformType.OpenCart).ToList();

                Dispatcher.Invoke(() =>
                {
                    HideAllStates();
                    _stores.Clear();
                    foreach (var store in ocStores)
                        _stores.Add(store);

                    if (_stores.Count == 0)
                        ShowEmpty();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError($"OpenCart siteleri yuklenemedi: {ex.Message}"));
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
