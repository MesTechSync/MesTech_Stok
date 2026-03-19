using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Application.Features.Platform.Queries.GetPlatformList;
using MesTech.Application.DTOs.Platform;

namespace MesTechStok.Desktop.Views.Platform
{
    public partial class PlatformOverviewView : UserControl
    {
        private readonly ObservableCollection<PlatformCardDto> _platforms = new();

        public PlatformOverviewView()
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

                var result = await mediator.Send(new GetPlatformListQuery(tenantId));

                Dispatcher.Invoke(() =>
                {
                    HideAllStates();
                    _platforms.Clear();
                    foreach (var item in result)
                        _platforms.Add(item);

                    if (_platforms.Count == 0)
                        ShowEmpty();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => ShowError($"Platform verileri yuklenemedi: {ex.Message}"));
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
