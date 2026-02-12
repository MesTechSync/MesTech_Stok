using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MesTechStok.Desktop.Data;
using MesTechStok.Desktop.Services;

namespace MesTechStok.Desktop.Views
{
    public partial class SimpleTestView : UserControl
    {
        private readonly IDatabaseService? _databaseService;
        private readonly ILogger<SimpleTestView>? _logger;
        private readonly DesktopDbContext? _context;

        public SimpleTestView()
        {
            InitializeComponent();

            // Get services from DI container
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                _databaseService = serviceProvider.GetService<IDatabaseService>();
                _logger = serviceProvider.GetService<ILogger<SimpleTestView>>();
                _context = serviceProvider.GetService<DesktopDbContext>();
            }
        }

        private async void TestDbButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Veritabanı bağlantısı test ediliyor...";

                if (_databaseService == null)
                {
                    ResultsText.Text = "❌ DatabaseService bulunamadı!";
                    return;
                }

                var isConnected = await _databaseService.IsDatabaseConnectedAsync();
                var info = await _databaseService.GetDatabaseInfoAsync();

                var result = new StringBuilder();
                result.AppendLine($"🔌 Bağlantı Durumu: {(isConnected ? "✅ BAŞARILI" : "❌ BAŞARISIZ")}");
                result.AppendLine($"📊 Veritabanı Bilgisi: {info}");
                result.AppendLine($"🕐 Test Zamanı: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                ResultsText.Text = result.ToString();
                StatusText.Text = isConnected ? "✅ Veritabanı bağlantısı başarılı" : "❌ Veritabanı bağlantısı başarısız";

                _logger?.LogInformation("Database connection test completed: {IsConnected}", isConnected);
            }
            catch (Exception ex)
            {
                ResultsText.Text = $"❌ HATA: {ex.Message}\n\nDetay:\n{ex}";
                StatusText.Text = "❌ Test başarısız";
                _logger?.LogError(ex, "Database connection test failed");
            }
        }

        private async void CreateDbButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Veritabanı oluşturuluyor...";

                if (_databaseService == null)
                {
                    ResultsText.Text = "❌ DatabaseService bulunamadı!";
                    return;
                }

                await _databaseService.InitializeDatabaseAsync();

                var result = new StringBuilder();
                result.AppendLine("✅ Veritabanı başarıyla oluşturuldu!");
                result.AppendLine($"🕐 Oluşturma Zamanı: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                // Test connection after creation
                var isConnected = await _databaseService.IsDatabaseConnectedAsync();
                var info = await _databaseService.GetDatabaseInfoAsync();

                result.AppendLine($"🔌 Bağlantı: {(isConnected ? "✅ BAŞARILI" : "❌ BAŞARISIZ")}");
                result.AppendLine($"📊 Bilgi: {info}");

                ResultsText.Text = result.ToString();
                StatusText.Text = "✅ Veritabanı hazır";

                _logger?.LogInformation("Database created successfully");
            }
            catch (Exception ex)
            {
                ResultsText.Text = $"❌ VERITABANI OLUŞTURMA HATASI: {ex.Message}\n\nDetay:\n{ex}";
                StatusText.Text = "❌ Veritabanı oluşturulamadı";
                _logger?.LogError(ex, "Database creation failed");
            }
        }

        private async void SeedDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Örnek veriler ekleniyor...";

                if (_context == null)
                {
                    ResultsText.Text = "❌ Database context bulunamadı!";
                    return;
                }

                // Add a simple test product
                var testProduct = new Product
                {
                    Name = "Test Ürün",
                    SKU = "TEST-001",
                    Barcode = "1111111111111",
                    Description = "Test için oluşturulan ürün",
                    CategoryId = 1,
                    PurchasePrice = 100m,
                    SalePrice = 150m,
                    Stock = 50,
                    IsActive = true,
                    CreatedDate = DateTime.Now
                };

                _context.Products.Add(testProduct);
                await _context.SaveChangesAsync();

                var result = new StringBuilder();
                result.AppendLine("✅ Test ürün başarıyla eklendi!");
                result.AppendLine($"📦 Ürün: {testProduct.Name}");
                result.AppendLine($"🔢 SKU: {testProduct.SKU}");
                result.AppendLine($"📊 Barkod: {testProduct.Barcode}");
                result.AppendLine($"💰 Fiyat: {testProduct.SalePrice:C}");
                result.AppendLine($"📦 Stok: {testProduct.Stock}");
                result.AppendLine($"🕐 Ekleme Zamanı: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                // Get updated database info
                if (_databaseService != null)
                {
                    var info = await _databaseService.GetDatabaseInfoAsync();
                    result.AppendLine($"📊 Güncel DB Bilgisi: {info}");
                }

                ResultsText.Text = result.ToString();
                StatusText.Text = "✅ Örnek veri eklendi";

                _logger?.LogInformation("Test product added successfully");
            }
            catch (Exception ex)
            {
                ResultsText.Text = $"❌ VERİ EKLEME HATASI: {ex.Message}\n\nDetay:\n{ex}";
                StatusText.Text = "❌ Veri eklenemedi";
                _logger?.LogError(ex, "Data seeding failed");
            }
        }
    }
}