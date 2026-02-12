using System;
using System.Windows;
using System.Windows.Controls;
using MesTechStok.Desktop.ViewModels;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// STOK YERLEŞİM SİSTEMİ ana view'ı
    /// </summary>
    public partial class StockPlacementView : UserControl
    {
        public StockPlacementView()
        {
            InitializeComponent();
        }

        #region Quick Access Button Click Handlers

        /// <summary>
        /// Depo Yönetimi butonuna tıklandığında
        /// </summary>
        private void OpenWarehouseManagement_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: MainViewModel'deki ShowWarehouseManagementCommand'ı çağır
                if (DataContext is MainViewModel viewModel)
                {
                    // viewModel.ShowWarehouseManagementCommand.Execute(null);
                    MessageBox.Show("Depo Yönetimi açılıyor...", "MesTech", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Depo Yönetimi açılırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Konum Takibi butonuna tıklandığında
        /// </summary>
        private void OpenLocationTracking_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: MainViewModel'deki ShowLocationTrackingCommand'ı çağır
                if (DataContext is MainViewModel viewModel)
                {
                    // viewModel.ShowLocationTrackingCommand.Execute(null);
                    MessageBox.Show("Konum Takibi açılıyor...", "MesTech", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Konum Takibi açılırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Depo Haritası butonuna tıklandığında
        /// </summary>
        private void OpenWarehouseMap_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: MainViewModel'deki ShowWarehouseMapCommand'ı çağır
                if (DataContext is MainViewModel viewModel)
                {
                    // viewModel.ShowWarehouseMapCommand.Execute(null);
                    MessageBox.Show("Depo Haritası açılıyor...", "MesTech", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Depo Haritası açılırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Mobil Depo butonuna tıklandığında
        /// </summary>
        private void OpenMobileWarehouse_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: MainViewModel'deki ShowMobileWarehouseCommand'ı çağır
                if (DataContext is MainViewModel viewModel)
                {
                    // viewModel.ShowMobileWarehouseCommand.Execute(null);
                    MessageBox.Show("Mobil Depo açılıyor...", "MesTech", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mobil Depo açılırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Konum Raporları butonuna tıklandığında
        /// </summary>
        private void OpenLocationReports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // TODO: MainViewModel'deki ShowLocationReportsCommand'ı çağır
                if (DataContext is MainViewModel viewModel)
                {
                    // viewModel.ShowLocationReportsCommand.Execute(null);
                    MessageBox.Show("Konum Raporları açılıyor...", "MesTech", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Konum Raporları açılırken hata oluştu: {ex.Message}", "Hata",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region UserControl Events

        /// <summary>
        /// View yüklendiğinde
        /// </summary>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // View yüklendiğinde yapılacak işlemler
                // Örneğin: İstatistikleri güncelle, son aktiviteleri yükle vb.
            }
            catch (Exception ex)
            {
                // Hata durumunda loglama yapılabilir
                System.Diagnostics.Debug.WriteLine($"StockPlacementView yüklenirken hata: {ex.Message}");
            }
        }

        #endregion
    }
}
