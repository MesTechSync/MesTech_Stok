using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Application.Interfaces;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// API Health Dashboard — TCP probe + adapter availability check.
    /// D-11 pattern: optional ctor params (null default) for WPF designer compat.
    /// </summary>
    public partial class ApiHealthDashboardView : UserControl
    {
        private readonly DispatcherTimer _refreshTimer;

        // D-11: injected dependencies (nullable for designer compat)
        private readonly TrendyolAdapter? _trendyolAdapter;
        private readonly OpenCartAdapter? _openCartAdapter;
        private readonly IInvoiceProvider? _invoiceProvider;

        /// <summary>
        /// WPF designer / parameter-less constructor — services resolved lazily via App.Services.
        /// Use the DI-injected overload in production.
        /// </summary>
        public ApiHealthDashboardView()
            : this(null, null, null)
        {
        }

        /// <summary>
        /// DI constructor — called when view is registered via services.AddTransient.
        /// </summary>
        public ApiHealthDashboardView(
            TrendyolAdapter? trendyolAdapter,
            OpenCartAdapter? openCartAdapter,
            IInvoiceProvider? invoiceProvider)
        {
            _trendyolAdapter = trendyolAdapter;
            _openCartAdapter = openCartAdapter;
            _invoiceProvider = invoiceProvider;

            InitializeComponent();

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            _refreshTimer.Tick += async (s, e) =>
            {
                if (AutoRefreshCheck.IsChecked == true)
                    await RefreshAllHealthAsync();
            };
            _refreshTimer.Start();

            Loaded += async (s, e) => await RefreshAllHealthAsync();
            Unloaded += (s, e) => _refreshTimer.Stop();
        }

        private void RefreshHealth_Click(object sender, RoutedEventArgs e)
        {
            _ = RefreshAllHealthAsync();
        }

        private async Task RefreshAllHealthAsync()
        {
            LastCheckText.Text = $"Son kontrol: {DateTime.Now:HH:mm:ss}";
            AddHealthEvent("Saglik kontrolu baslatildi...");

            int upCount = 0, totalCount = 6;

            // PostgreSQL (port 5432)
            var pgResult = await CheckTcpAsync("localhost", 5432);
            PostgresStatusText.Text = pgResult.Up ? "Aktif" : "Kapalı";
            PostgresConnectionsText.Text = pgResult.Up ? $"Gecikme: {pgResult.LatencyMs}ms" : "Baglanti yok";
            PostgresDbSizeText.Text = pgResult.Up ? "PostgreSQL 17" : "Erisilemiyor";
            if (pgResult.Up) upCount++;
            AddHealthEvent($"PostgreSQL: {(pgResult.Up ? "UP" : "DOWN")} ({pgResult.LatencyMs}ms)");

            // Redis (port 6379)
            var redisResult = await CheckTcpAsync("localhost", 6379);
            RedisStatusText.Text = redisResult.Up ? "Aktif" : "Kapali";
            RedisMemoryText.Text = redisResult.Up ? $"Gecikme: {redisResult.LatencyMs}ms" : "Baglanti yok";
            RedisHitRateText.Text = redisResult.Up ? "Redis Cache" : "Erisilemiyor";
            if (redisResult.Up) upCount++;
            AddHealthEvent($"Redis: {(redisResult.Up ? "UP" : "DOWN")} ({redisResult.LatencyMs}ms)");

            // RabbitMQ (port 5672)
            var rmqResult = await CheckTcpAsync("localhost", 5672);
            RabbitMqStatusText.Text = rmqResult.Up ? "Aktif" : "Kapali";
            RabbitMqQueuesText.Text = rmqResult.Up ? $"Gecikme: {rmqResult.LatencyMs}ms" : "Baglanti yok";
            RabbitMqMessagesText.Text = rmqResult.Up ? "AMQP" : "Erisilemiyor";
            if (rmqResult.Up) upCount++;
            AddHealthEvent($"RabbitMQ: {(rmqResult.Up ? "UP" : "DOWN")} ({rmqResult.LatencyMs}ms)");

            // Trendyol Adapter (D-11: use injected field, not ServiceLocator)
            TrendyolStatusText.Text = _trendyolAdapter != null ? "Hazir" : "Kayitli Degil";
            TrendyolLatencyText.Text = _trendyolAdapter != null ? "Adapter aktif" : "DI'da yok";
            TrendyolLastSyncText.Text = "Baglanti testi icin Trendyol ekranini kullanin";
            TrendyolRateLimitText.Text = _trendyolAdapter != null ? "Rate limit: 100 RPS" : "--";
            if (_trendyolAdapter != null) upCount++;
            AddHealthEvent($"Trendyol Adapter: {(_trendyolAdapter != null ? "REGISTERED" : "NOT FOUND")}");

            // OpenCart Adapter (D-11: use injected field, not ServiceLocator)
            OpenCartStatusText.Text = _openCartAdapter != null ? "Hazir" : "Kayitli Degil";
            OpenCartLatencyText.Text = _openCartAdapter != null ? "Adapter aktif" : "DI'da yok";
            OpenCartLastSyncText.Text = "--";
            OpenCartProductCountText.Text = _openCartAdapter != null ? "Yapilandirilmadi" : "--";
            if (_openCartAdapter != null) upCount++;
            AddHealthEvent($"OpenCart Adapter: {(_openCartAdapter != null ? "REGISTERED" : "NOT FOUND")}");

            // Invoice Service (D-11: use injected field, not ServiceLocator)
            InvoiceServiceStatusText.Text = _invoiceProvider != null ? "Aktif" : "Kayitli Degil";
            InvoiceServiceLatencyText.Text = _invoiceProvider != null ? $"Provider: {_invoiceProvider.ProviderName}" : "DI'da yok";
            InvoiceTodayCountText.Text = "--";
            InvoiceErrorCountText.Text = "--";
            if (_invoiceProvider != null) upCount++;
            AddHealthEvent($"Invoice Service: {(_invoiceProvider != null ? _invoiceProvider.ProviderName : "NOT FOUND")}");

            // Overall status
            if (upCount == totalCount)
            {
                OverallStatusBorder.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                OverallStatusIcon.Text = "OK";
                OverallStatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                OverallStatusText.Text = "Tum sistemler calisiyor";
                OverallStatusText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            }
            else
            {
                OverallStatusBorder.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                OverallStatusIcon.Text = "!";
                OverallStatusIcon.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                OverallStatusText.Text = $"{upCount}/{totalCount} servis aktif";
                OverallStatusText.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
            }

            AddHealthEvent($"Saglik kontrolu tamamlandi: {upCount}/{totalCount} aktif");
        }

        private static async Task<(bool Up, int LatencyMs)> CheckTcpAsync(string host, int port)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);
                var completed = await Task.WhenAny(connectTask, Task.Delay(3000));
                sw.Stop();

                if (completed == connectTask && client.Connected)
                    return (true, (int)sw.ElapsedMilliseconds);

                return (false, (int)sw.ElapsedMilliseconds);
            }
            catch
            {
                sw.Stop();
                return (false, (int)sw.ElapsedMilliseconds);
            }
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
