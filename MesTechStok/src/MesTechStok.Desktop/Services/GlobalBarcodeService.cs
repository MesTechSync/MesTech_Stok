using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Integrations.Barcode;
using CoreBarcodeEventArgs = MesTechStok.Core.Integrations.Barcode.Models.BarcodeScannedEventArgs;
using MesTechStok.Desktop.Components;
using MesTechStok.Desktop.Utils;
using MesTechStok.Desktop.Views;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// Barcode event args sınıfı - Interface seviyesinde erişilebilir
    /// </summary>
    public class BarcodeEventArgs : EventArgs
    {
        public string Barcode { get; }
        public DateTime Timestamp { get; }

        public BarcodeEventArgs(string barcode)
        {
            Barcode = barcode;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Global barcode servis - Uygulamanın herhangi bir yerinde barcode okutulduğunda ürün popup'ı gösterir
    /// </summary>
    public class GlobalBarcodeService : IGlobalBarcodeService, IDisposable
    {
        private readonly ILogger<GlobalBarcodeService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private IBarcodeScannerService? _barcodeScannerService;
        private bool _isListening;
        private bool _isEnabled = true; // Servis aktif/pasif durumu

        public event EventHandler<BarcodeEventArgs>? BarcodeReceived;

        public bool IsListening => _isListening;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    _logger.LogInformation("Global barcode service {State}", value ? "enabled" : "disabled");

                    // Eğer disable edildiyse ve dinliyorsak, dinlemeyi durdur
                    if (!value && _isListening)
                    {
                        StopListeningAsync().Wait();
                    }
                }
            }
        }

        public GlobalBarcodeService(ILogger<GlobalBarcodeService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task StartListeningAsync()
        {
            System.Diagnostics.Debug.WriteLine("[GlobalBarcodeService] StartListeningAsync called");

            if (!_isEnabled)
            {
                _logger.LogWarning("Global barcode service is disabled, cannot start listening");
                System.Diagnostics.Debug.WriteLine("[GlobalBarcodeService] Service disabled, cannot start");
                return;
            }

            if (_isListening)
            {
                _logger.LogWarning("Global barcode service is already listening");
                System.Diagnostics.Debug.WriteLine("[GlobalBarcodeService] Already listening");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("[GlobalBarcodeService] Creating barcode scanner service...");

                // Barcode scanner service'i al veya oluştur
                _barcodeScannerService = _serviceProvider.GetService<IBarcodeScannerService>()
                                      ?? new BarcodeScannerService();

                System.Diagnostics.Debug.WriteLine($"[GlobalBarcodeService] Scanner service created: {_barcodeScannerService?.GetType().Name}");

                // Event handler'ı bağla
                _barcodeScannerService.BarcodeScanned += OnBarcodeScanned;
                System.Diagnostics.Debug.WriteLine("[GlobalBarcodeService] Event handler attached");

                // Hardware scanner'ı başlat
                var scannerStarted = await _barcodeScannerService.StartScanningAsync();
                System.Diagnostics.Debug.WriteLine($"[GlobalBarcodeService] Scanner StartScanningAsync result: {scannerStarted}");

                _isListening = true;
                _logger.LogInformation("Global barcode service started listening");
                System.Diagnostics.Debug.WriteLine("[GlobalBarcodeService] ✅ Service started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start global barcode listening");
                throw;
            }
        }

        public async Task StopListeningAsync()
        {
            if (!_isListening)
            {
                _logger.LogWarning("Global barcode service is not listening");
                return;
            }

            try
            {
                if (_barcodeScannerService != null)
                {
                    // Event handler'ı kaldır
                    _barcodeScannerService.BarcodeScanned -= OnBarcodeScanned;

                    // Scanner'ı durdur
                    await _barcodeScannerService.StopScanningAsync();
                    // Dispose is not available on interface, just set to null
                    _barcodeScannerService = null;
                }

                _isListening = false;
                _logger.LogInformation("Global barcode service stopped listening");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop global barcode listening");
                throw;
            }
        }

        private async void OnBarcodeScanned(object? sender, MesTechStok.Core.Integrations.Barcode.BarcodeScannedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[GlobalBarcodeService] OnBarcodeScanned called with: '{e.Barcode}'");

                if (!_isEnabled)
                {
                    _logger.LogDebug("Barcode received but service is disabled: {Barcode}", e.Barcode);
                    System.Diagnostics.Debug.WriteLine($"[GlobalBarcodeService] Service disabled, ignoring barcode: {e.Barcode}");
                    return;
                }

                _logger.LogInformation("Global barcode received: {Barcode}", e.Barcode);
                System.Diagnostics.Debug.WriteLine($"[GlobalBarcodeService] ✅ Processing barcode: {e.Barcode}");

                // Yazılım içi log sistemine kayıt ekle
                try
                {
                    var globalLogger = MesTechStok.Desktop.Utils.GlobalLogger.Instance;
                    if (globalLogger != null)
                    {
                        globalLogger.LogInfo($"Global barkod okundu: {e.Barcode}", "GlobalBarcodeService");
                        globalLogger.LogEvent("BARCODE_SCAN", $"Barkod: {e.Barcode} | Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", "Barcode");
                    }
                }
                catch (Exception logEx)
                {
                    _logger.LogWarning(logEx, "Failed to log to internal log system");
                }

                // Event'i tetikle
                BarcodeReceived?.Invoke(this, new BarcodeEventArgs(e.Barcode));

                // UI thread'de çalıştır
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await ShowProductPopupAsync(e.Barcode);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing global barcode: {Barcode}", e.Barcode);
            }
        }

        private async Task ShowProductPopupAsync(string barcode)
        {
            try
            {
                // Veritabanından ürünü ara
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var product = await Task.Run(() =>
                    dbContext.Products.FirstOrDefault(p =>
                        p.Barcode == barcode ||
                        p.GTIN == barcode ||
                        p.UPC == barcode ||
                        p.EAN == barcode));

                if (product != null)
                {
                    _logger.LogInformation("Product found for barcode {Barcode}: {ProductName}", barcode, product.Name);

                    // Ürün popup'ını göster
                    var popup = new BarcodeProductPopup(product);

                    // Ana pencereyi owner olarak ayarla
                    if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                    {
                        popup.Owner = Application.Current.MainWindow;
                    }

                    popup.ShowDialog();

                    ToastManager.ShowSuccess($"Ürün bulundu: {product.Name}", "Barkod");
                }
                else
                {
                    _logger.LogWarning("Product not found for barcode: {Barcode}", barcode);

                    // Ürün bulunamadı mesajı
                    ToastManager.ShowWarning($"Barkod '{barcode}' için ürün bulunamadı", "Barkod");

                    // İsteğe bağlı: Yeni ürün ekleme popup'ı açılabilir
                    var result = MessageBox.Show(
                        $"'{barcode}' barkodu için ürün bulunamadı.\n\nYeni ürün eklemek ister misiniz?",
                        "Ürün Bulunamadı",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        var newProduct = new Product
                        {
                            Barcode = barcode,
                            Name = "",
                            SKU = "",
                            Stock = 0,
                            MinimumStock = 5,
                            PurchasePrice = 0,
                            SalePrice = 0
                        };

                        // ProductItem'a dönüştür
                        var productItem = new ProductItem
                        {
                            Id = 0, // Yeni ürün
                            Name = "",
                            Barcode = barcode,
                            Category = "",
                            Sku = "",
                            Price = 0,
                            Stock = 0,
                            ImageUrl = ""
                        };

                        var addPopup = new ProductEditDialog(productItem);
                        if (Application.Current.MainWindow != null && Application.Current.MainWindow.IsLoaded)
                        {
                            addPopup.Owner = Application.Current.MainWindow;
                        }
                        addPopup.ShowDialog();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing product popup for barcode: {Barcode}", barcode);
                ToastManager.ShowError("Ürün gösterilirken hata oluştu", "Hata");
            }
        }

        public void Dispose()
        {
            StopListeningAsync().Wait();
        }
    }

    public interface IGlobalBarcodeService : IDisposable
    {
        bool IsListening { get; }
        bool IsEnabled { get; set; }
        event EventHandler<BarcodeEventArgs>? BarcodeReceived;
        Task StartListeningAsync();
        Task StopListeningAsync();
    }


}
