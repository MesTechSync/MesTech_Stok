using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MesTechStok.Core.Services.Abstract
{
    /// <summary>
    /// QR kod entegrasyonu için servis interface'i
    /// </summary>
    public interface IQRCodeService
    {
        // QR Kod Oluşturma
        Task<byte[]> GenerateLocationQRCodeAsync(string binCode);
        Task<byte[]> GenerateProductQRCodeAsync(int productId);
        Task<byte[]> GenerateMovementQRCodeAsync(int movementId);

        // QR Kod Okuma
        Task<LocationInfo> ReadLocationQRCodeAsync(byte[] qrCodeImage);
        Task<ProductInfo> ReadProductQRCodeAsync(byte[] qrCodeImage);

        // QR Kod Yönetimi
        Task<string> GetQRCodeContentAsync(string binCode);
        Task<bool> ValidateQRCodeAsync(string qrCodeContent);

        // Gelişmiş QR Kod Özellikleri
        Task<byte[]> GenerateBulkQRCodeAsync(List<string> binCodes);
        Task<byte[]> GenerateDynamicQRCodeAsync(DynamicQRCodeRequest request);
        Task<QRCodeValidationResult> ValidateQRCodeWithDetailsAsync(string qrCodeContent);

        // QR Kod Şablonları
        Task<byte[]> GenerateQRCodeWithTemplateAsync(string content, QRCodeTemplate template);
        Task<List<QRCodeTemplate>> GetAvailableTemplatesAsync();

        // QR Kod Analizi
        Task<QRCodeAnalytics> GetQRCodeAnalyticsAsync(string binCode, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<QRCodeScanHistory>> GetQRCodeScanHistoryAsync(string binCode, int limit = 100);
    }

    /// <summary>
    /// Konum bilgisi modeli
    /// </summary>
    public class LocationInfo
    {
        public string Type { get; set; } = "LOCATION";
        public string BinCode { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public string ShelfName { get; set; } = string.Empty;
        public string Coordinates { get; set; } = string.Empty; // "X:120,Y:80,Z:150"
        public string QRCodeVersion { get; set; } = "1.0";
        public DateTime GeneratedDate { get; set; }
    }

    /// <summary>
    /// Ürün bilgisi modeli
    /// </summary>
    public class ProductInfo
    {
        public string Type { get; set; } = "PRODUCT";
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string QRCodeVersion { get; set; } = "1.0";
        public DateTime GeneratedDate { get; set; }
    }

    /// <summary>
    /// Dinamik QR kod isteği
    /// </summary>
    public class DynamicQRCodeRequest
    {
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = "LOCATION"; // LOCATION, PRODUCT, MOVEMENT, CUSTOM
        public Dictionary<string, string> CustomData { get; set; } = new();
        public int Size { get; set; } = 256; // QR kod boyutu (pixel)
        public string ErrorCorrectionLevel { get; set; } = "M"; // L, M, Q, H
        public bool IncludeLogo { get; set; } = false;
        public string LogoPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// QR kod doğrulama sonucu
    /// </summary>
    public class QRCodeValidationResult
    {
        public bool IsValid { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime? GeneratedDate { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, string> ParsedData { get; set; } = new();
    }

    /// <summary>
    /// QR kod şablonu
    /// </summary>
    public class QRCodeTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Size { get; set; } = 256;
        public string ErrorCorrectionLevel { get; set; } = "M";
        public string ForegroundColor { get; set; } = "#000000";
        public string BackgroundColor { get; set; } = "#FFFFFF";
        public bool IncludeBorder { get; set; } = true;
        public int BorderWidth { get; set; } = 2;
        public string BorderColor { get; set; } = "#000000";
        public bool IncludeLogo { get; set; } = false;
        public string LogoPath { get; set; } = string.Empty;
        public int LogoSize { get; set; } = 64;
    }

    /// <summary>
    /// QR kod analitikleri
    /// </summary>
    public class QRCodeAnalytics
    {
        public string BinCode { get; set; } = string.Empty;
        public int TotalScans { get; set; }
        public int UniqueScanners { get; set; }
        public DateTime FirstScan { get; set; }
        public DateTime LastScan { get; set; }
        public TimeSpan AverageScanInterval { get; set; }
        public List<ScanTimeDistribution> ScanTimeDistribution { get; set; } = new();
        public List<ScannerDeviceInfo> TopScanners { get; set; } = new();
    }

    /// <summary>
    /// Tarama zaman dağılımı
    /// </summary>
    public class ScanTimeDistribution
    {
        public string TimeSlot { get; set; } = string.Empty; // "09:00-12:00", "12:00-15:00"
        public int ScanCount { get; set; }
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Tarayıcı cihaz bilgisi
    /// </summary>
    public class ScannerDeviceInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty; // "Mobile", "Desktop", "Scanner"
        public int ScanCount { get; set; }
        public DateTime LastScan { get; set; }
    }

    /// <summary>
    /// QR kod tarama geçmişi
    /// </summary>
    public class QRCodeScanHistory
    {
        public int Id { get; set; }
        public string BinCode { get; set; } = string.Empty;
        public string ScannerId { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public DateTime ScanTime { get; set; }
        public string ScanResult { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
    }
}
