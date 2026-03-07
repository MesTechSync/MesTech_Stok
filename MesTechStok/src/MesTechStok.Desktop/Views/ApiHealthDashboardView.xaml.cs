using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MesTechStok.Desktop.Views
{
    public partial class ApiHealthDashboardView : UserControl
    {
        private readonly DispatcherTimer _refreshTimer;

        public ApiHealthDashboardView()
        {
            InitializeComponent();

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            _refreshTimer.Tick += (s, e) =>
            {
                if (AutoRefreshCheck.IsChecked == true)
                    RefreshAllHealth();
            };
            _refreshTimer.Start();

            Loaded += (s, e) => RefreshAllHealth();
            Unloaded += (s, e) => _refreshTimer.Stop();
        }

        private void RefreshHealth_Click(object sender, RoutedEventArgs e)
        {
            RefreshAllHealth();
        }

        private void RefreshAllHealth()
        {
            LastCheckText.Text = $"Son kontrol: {DateTime.Now:HH:mm:ss}";
            AddHealthEvent("Saglik kontrolu baslatildi...");

            // TODO: Gercek health check implementasyonu (DEV4 altyapi hazir oldugunda)
            // TrendyolAdapter.CheckHealthAsync()
            // OpenCartClient.CheckHealthAsync()
            // Redis ping
            // RabbitMQ connection check
            // PostgreSQL connection check
            // IInvoiceProvider health check

            // Placeholder status
            TrendyolStatusText.Text = "Bekleniyor";
            TrendyolLatencyText.Text = "Gecikme: -- ms";
            TrendyolLastSyncText.Text = "Son sync: --";

            OpenCartStatusText.Text = "Bekleniyor";
            OpenCartLatencyText.Text = "Gecikme: -- ms";
            OpenCartLastSyncText.Text = "Son sync: --";

            InvoiceServiceStatusText.Text = "Bekleniyor";
            InvoiceServiceLatencyText.Text = "Gecikme: -- ms";

            PostgresStatusText.Text = "Bekleniyor";
            RedisStatusText.Text = "Bekleniyor";
            RabbitMqStatusText.Text = "Bekleniyor";

            AddHealthEvent("Saglik kontrolu tamamlandi. Gercek servisler icin DEV3/DEV4 implementasyonu bekleniyor.");
        }

        private void AddHealthEvent(string message)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            HealthEventsList.Items.Insert(0, entry);
            if (HealthEventsList.Items.Count > 100)
                HealthEventsList.Items.RemoveAt(HealthEventsList.Items.Count - 1);
        }
    }
}
