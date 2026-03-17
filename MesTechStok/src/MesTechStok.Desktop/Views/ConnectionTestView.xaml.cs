using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views
{
    public partial class ConnectionTestView : UserControl
    {
        private readonly ObservableCollection<ConnectionTestRow> _connectionRows = new();

        public ConnectionTestView()
        {
            InitializeComponent();
            ConnectionGrid.ItemsSource = _connectionRows;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _connectionRows.Clear();

            // Infrastructure services
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "PostgreSQL", ServiceType = "Veritabani", Endpoint = "localhost", Port = "5432", ResponseTime = "12 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "Redis", ServiceType = "Onbellek", Endpoint = "localhost", Port = "6379", ResponseTime = "3 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "RabbitMQ", ServiceType = "Mesajlasma", Endpoint = "localhost", Port = "5672", ResponseTime = "18 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "MySQL (OpenCart)", ServiceType = "Veritabani", Endpoint = "localhost", Port = "3306", ResponseTime = "15 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "Seq (Logging)", ServiceType = "Log Sunucu", Endpoint = "localhost", Port = "5341", ResponseTime = "8 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "MinIO", ServiceType = "Dosya Depo", Endpoint = "localhost", Port = "9000", ResponseTime = "22 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });

            // Platform APIs
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "Trendyol API", ServiceType = "Platform API", Endpoint = "api.trendyol.com", Port = "443", ResponseTime = "145 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "Hepsiburada API", ServiceType = "Platform API", Endpoint = "mpop-sit.hepsiburada.com", Port = "443", ResponseTime = "210 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "N11 API", ServiceType = "Platform API", Endpoint = "api.n11.com", Port = "443", ResponseTime = "-", LastTested = DateTime.Now, Status = "Basarisiz", ErrorMessage = "Connection timeout (30s)" });
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "Ciceksepeti API", ServiceType = "Platform API", Endpoint = "apis.ciceksepeti.com", Port = "443", ResponseTime = "178 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "Pazarama API", ServiceType = "Platform API", Endpoint = "isortagim.pazarama.com", Port = "443", ResponseTime = "-", LastTested = DateTime.MinValue, Status = "Test Edilmedi", ErrorMessage = "" });

            // Internal services
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "Stok Health Check", ServiceType = "Dahili Servis", Endpoint = "localhost", Port = "5100", ResponseTime = "5 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "MESA Status", ServiceType = "Dahili Servis", Endpoint = "localhost", Port = "5101", ResponseTime = "7 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "Prometheus", ServiceType = "Monitoring", Endpoint = "localhost", Port = "9090", ResponseTime = "11 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });
            _connectionRows.Add(new ConnectionTestRow { ServiceName = "Grafana", ServiceType = "Monitoring", Endpoint = "localhost", Port = "3000", ResponseTime = "14 ms", LastTested = DateTime.Now, Status = "Basarili", ErrorMessage = "" });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            TotalServicesText.Text = _connectionRows.Count.ToString();
            SuccessCountText.Text = _connectionRows.Count(r => r.Status == "Basarili").ToString();
            FailCountText.Text = _connectionRows.Count(r => r.Status == "Basarisiz").ToString();
            UntestedCountText.Text = _connectionRows.Count(r => r.Status == "Test Edilmedi").ToString();
        }

        private void TestAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Tum baglantilar test ediliyor...\n(Gercek baglanti testi modulu yakin zamanda aktif olacak.)",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadMockData();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadMockData();

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class ConnectionTestRow
    {
        public string ServiceName { get; set; } = "";
        public string ServiceType { get; set; } = "";
        public string Endpoint { get; set; } = "";
        public string Port { get; set; } = "";
        public string ResponseTime { get; set; } = "";
        public DateTime LastTested { get; set; }
        public string Status { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }
}
