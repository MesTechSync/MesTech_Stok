using System;
using System.Threading.Tasks;
using System.Threading;
using MesTechStok.Desktop.Models;
using System.Collections.Generic;

namespace MesTechStok.Desktop.Services
{
    public class MockBarcodeService : IBarcodeService
    {
        public event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;
        public event EventHandler<string>? DeviceStatusChanged;

        private bool _isConnected = false;
        private bool _isScanning = false;
        private Timer? _simulationTimer;

        public bool IsConnected => _isConnected;

        public Task<bool> ConnectAsync()
        {
            _isConnected = true;
            DeviceStatusChanged?.Invoke(this, "Bağlandı");
            return Task.FromResult(true);
        }

        public Task<bool> DisconnectAsync()
        {
            StopScanningAsync();
            _isConnected = false;
            DeviceStatusChanged?.Invoke(this, "Bağlantı kesildi");
            return Task.FromResult(true);
        }

        public Task<bool> StartScanningAsync()
        {
            if (!_isConnected) return Task.FromResult(false);

            _isScanning = true;
            DeviceStatusChanged?.Invoke(this, "Tarama başlatıldı");

            // Demo için rastgele barkod simülasyonu
            _simulationTimer = new Timer(SimulateBarcodeScanning, null, 3000, 8000);

            return Task.FromResult(true);
        }

        public Task<bool> StopScanningAsync()
        {
            _isScanning = false;
            _simulationTimer?.Dispose();
            _simulationTimer = null;
            DeviceStatusChanged?.Invoke(this, "Tarama durduruldu");
            return Task.FromResult(true);
        }

        private void SimulateBarcodeScanning(object? state)
        {
            if (!_isScanning) return;

            // Demo barkodları
            var demoBarcodes = new[]
            {
                "1234567890123",
                "2345678901234",
                "3456789012345",
                "4567890123456",
                "5678901234567"
            };

            var random = new Random();
            var randomBarcode = demoBarcodes[random.Next(demoBarcodes.Length)];

            BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(randomBarcode));
        }

        // ========== YENİ ENHANCED BARCODE METHODLARİ ==========
        public async Task<string?> ScanBarcodeAsync(int timeoutMs = 10000)
        {
            await Task.Delay(500); // Simülasyon gecikmesi

            var demoBarcodes = new[]
            {
                "1234567890123",
                "2345678901234",
                "3456789012345",
                "4567890123456",
                "5678901234567"
            };

            var random = new Random();
            return demoBarcodes[random.Next(demoBarcodes.Length)];
        }

        public async Task<BarcodeValidationResult> ValidateBarcodeAsync(string barcode)
        {
            await Task.Delay(200); // Simülasyon gecikmesi

            return new BarcodeValidationResult
            {
                IsValid = !string.IsNullOrEmpty(barcode) && barcode.Length >= 10,
                Message = barcode.Length >= 10 ? "Geçerli barkod" : "Barkod çok kısa",
                Format = barcode.Length == 13 ? ServiceBarcodeFormat.EAN13 : ServiceBarcodeFormat.Code128,
                ConfidenceLevel = 0.95,
                Metadata = new Dictionary<string, object>
                {
                    { "scanned_at", DateTime.Now },
                    { "device", "MockScanner" }
                }
            };
        }

        public async Task<ProductSuggestion?> GetProductSuggestionFromBarcodeAsync(string barcode)
        {
            await Task.Delay(300); // AI işlem simülasyonu

            var suggestions = new List<ProductSuggestion>
            {
                new ProductSuggestion
                {
                    ProductName = "Akıllı Telefon Galaxy S24",
                    Category = "Elektronik",
                    SuggestedPrice = 25000,
                    ConfidenceScore = 0.92,
                    Description = "Son model akıllı telefon, 256GB, Siyah",
                    AlternativeNames = new[] { "Samsung S24", "Galaxy S24" }
                },
                new ProductSuggestion
                {
                    ProductName = "Dizüstü Bilgisayar ThinkPad",
                    Category = "Bilgisayar",
                    SuggestedPrice = 35000,
                    ConfidenceScore = 0.88,
                    Description = "İş kullanımı için dizüstü bilgisayar",
                    AlternativeNames = new[] { "ThinkPad E15", "Lenovo E15" }
                }
            };

            var random = new Random();
            return suggestions[random.Next(suggestions.Count)];
        }
    }
}
