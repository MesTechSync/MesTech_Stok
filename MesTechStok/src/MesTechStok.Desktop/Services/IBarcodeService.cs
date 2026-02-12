using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// 🚀 YENİLİKÇİ GELİŞTİRME: Enhanced IBarcodeService with AI-powered features
    /// Barcode okuma servisi - gelecek teknolojileri destekleyecek şekilde tasarlandı
    /// </summary>
    public interface IBarcodeService
    {
        event EventHandler<BarcodeScannedEventArgs>? BarcodeScanned;
        event EventHandler<string>? DeviceStatusChanged;

        bool IsConnected { get; }
        Task<bool> ConnectAsync();
        Task<bool> DisconnectAsync();
        Task<bool> StartScanningAsync();
        Task<bool> StopScanningAsync();

        // 🚀 YENİLİKÇİ GELİŞTİRME: Direct scan method for popup integration
        Task<string?> ScanBarcodeAsync(int timeoutMs = 10000);

        // 🚀 GELECEĞİ HAZIRLIK: AI-powered barcode validation & product matching
        Task<BarcodeValidationResult> ValidateBarcodeAsync(string barcode);
        Task<ProductSuggestion?> GetProductSuggestionFromBarcodeAsync(string barcode);
    }

    /// <summary>
    /// Barcode tarama event argument sınıfı - ZXing bağımsız
    /// </summary>
    public class BarcodeScannedEventArgs : EventArgs
    {
        public string Barcode { get; set; } = string.Empty;
        public DateTime ScanTime { get; set; } = DateTime.Now;
        public ServiceBarcodeFormat Format { get; set; } = ServiceBarcodeFormat.Code128;

        public BarcodeScannedEventArgs() { }

        public BarcodeScannedEventArgs(string barcode)
        {
            Barcode = barcode;
        }

        public BarcodeScannedEventArgs(string barcode, ServiceBarcodeFormat format)
        {
            Barcode = barcode;
            Format = format;
        }
    }

    /// <summary>
    /// 🚀 GELECEĞİ HAZIRLIK: Service-level BarcodeFormat enum to avoid ZXing conflicts  
    /// ZXing kütüphanesi ile çakışmaması için ServiceBarcodeFormat kullanıyoruz
    /// </summary>
    public enum ServiceBarcodeFormat
    {
        Code128,
        Code39,
        EAN13,
        EAN8,
        UPCA,
        UPCE,
        QRCode,
        DataMatrix,
        PDF417,
        Aztec,
        ITF
    }

    /// <summary>
    /// 🚀 YENİLİKÇİ GELİŞTİRME: Enhanced validation result with AI confidence
    /// AI tabanlı barcode doğrulama sonucu - gelecekte machine learning entegrasyonu için hazır
    /// </summary>
    public class BarcodeValidationResult
    {
        public bool IsValid { get; set; }
        public ServiceBarcodeFormat Format { get; set; }
        public string? Message { get; set; }
        public double ConfidenceLevel { get; set; } = 1.0;
        public Dictionary<string, object>? Metadata { get; set; }
        public string[]? SuggestedCorrections { get; set; }
    }

    /// <summary>
    /// 🚀 YENİLİKÇİ GELİŞTİRME: AI-powered product suggestion from barcode
    /// Barcode'dan ürün önerisi - gelecekte AI/ML algoritmaları entegre edilecek
    /// </summary>
    public class ProductSuggestion
    {
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal SuggestedPrice { get; set; }
        public string? Manufacturer { get; set; }
        public string Description { get; set; } = string.Empty;
        public string[]? AlternativeNames { get; set; }
        public double ConfidenceScore { get; set; } = 1.0;
    }
}
