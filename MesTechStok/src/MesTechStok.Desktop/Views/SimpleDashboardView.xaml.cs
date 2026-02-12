using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MesTechStok.Desktop.Utils;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// SimpleDashboardView.xaml için interaction logic
    /// </summary>
    public partial class SimpleDashboardView : UserControl
    {
        private readonly DispatcherTimer _updateTimer;
        private readonly Random _random;

        // Events for navigation
        public event EventHandler? NavigateToProducts;
        public event EventHandler? NavigateToBarcode;
        public event EventHandler? NavigateToReports;
        public event EventHandler? NavigateToOpenCart;
        public event EventHandler? NavigateToSettings;

        public SimpleDashboardView()
        {
            InitializeComponent();
            _random = new Random();

            // Timer ile metrikleri güncelle
            _updateTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(8) // Daha az sık güncelleme
            };
            _updateTimer.Tick += UpdateMetrics;
            _updateTimer.Start();

            // İlk güncelleme
            UpdateMetrics(null, null);
        }

        private void UpdateMetrics(object? sender, EventArgs? e)
        {
            try
            {
                // Demo verilerle metrikleri güncelle
                var products = 150 + _random.Next(-5, 5);
                var lowStock = 12 + _random.Next(-2, 3);
                var totalValue = 25000 + _random.Next(-500, 1000);

                TotalProductsText.Text = products.ToString();
                LowStockText.Text = lowStock.ToString();
                TotalValueText.Text = $"₺{totalValue:N0}";

                // Aktivite paneline zaman damgası ekle
                AddActivity($"📊 Veriler güncellendi - {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                // Hata durumunda sessizce devam et
                System.Diagnostics.Debug.WriteLine($"Metric update error: {ex.Message}");
            }
        }

        private void AddActivity(string activity)
        {
            try
            {
                // Yeni aktivite ekle
                var activityText = new TextBlock
                {
                    Text = activity,
                    FontSize = 12,
                    Margin = new Thickness(0, 2, 0, 0),
                    Foreground = System.Windows.Media.Brushes.Gray
                };

                ActivityPanel.Children.Insert(0, activityText);

                // Maksimum 8 aktivite tutulsun
                while (ActivityPanel.Children.Count > 8)
                {
                    ActivityPanel.Children.RemoveAt(ActivityPanel.Children.Count - 1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Activity add error: {ex.Message}");
            }
        }

        // Button Event Handlers - Now functional, no popups
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            NavigateToProducts?.Invoke(this, EventArgs.Empty);
        }

        private void ScanBarcode_Click(object sender, RoutedEventArgs e)
        {
            NavigateToBarcode?.Invoke(this, EventArgs.Empty);
        }

        private void ShowReports_Click(object sender, RoutedEventArgs e)
        {
            NavigateToReports?.Invoke(this, EventArgs.Empty);
        }

        private void RefreshStock_Click(object sender, RoutedEventArgs e)
        {
            // Bu event için ana viewModel'de bir command yok, şimdilik toast gösterelim
            ToastManager.ShowInfo("Stok yenileme işlemi tetiklendi (demo)", "Dashboard");
            UpdateMetrics(null, null);
        }

        private void OpenCartSync_Click(object sender, RoutedEventArgs e)
        {
            NavigateToOpenCart?.Invoke(this, EventArgs.Empty);
        }

        private void ShowSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigateToSettings?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _updateTimer?.Stop();
        }
    }
}