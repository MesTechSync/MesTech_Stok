using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MesTechStok.Core.Services.Abstract;
using QRCoder;

namespace MesTechStok.Core.Services.Concrete
{
    /// <summary>
    /// Gercek QR kod PNG uretimi — QRCoder (PngByteQRCode) kullanir.
    /// System.Drawing bagimliligi YOK — Linux/Docker uyumlu.
    /// </summary>
    public class QRCodeService : IQRCodeService
    {
        private readonly ILogger<QRCodeService> _logger;

        public QRCodeService(ILogger<QRCodeService> logger)
        {
            _logger = logger;
        }

        #region Core QR Generation

        private static byte[] GenerateQRPng(string content, int pixelPerModule = 10)
        {
            using var generator = new QRCodeGenerator();
            var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(data);
            return qrCode.GetGraphic(pixelPerModule);
        }

        #endregion

        #region QR Kod Olusturma

        public Task<byte[]> GenerateLocationQRCodeAsync(string binCode)
        {
            _logger.LogInformation("Generating location QR code for bin: {BinCode}", binCode);

            var content = $"MESTECH:LOC|BIN:{binCode}|T:{DateTime.UtcNow:yyyyMMddHHmm}";
            var png = GenerateQRPng(content, 8);

            _logger.LogInformation("Location QR PNG generated: {Bytes} bytes for {BinCode}", png.Length, binCode);
            return Task.FromResult(png);
        }

        public Task<byte[]> GenerateProductQRCodeAsync(Guid productId)
        {
            _logger.LogInformation("Generating product QR code for product: {ProductId}", productId);

            var content = $"MESTECH:PRD|ID:{productId}|T:{DateTime.UtcNow:yyyyMMddHHmm}";
            var png = GenerateQRPng(content, 8);

            _logger.LogInformation("Product QR PNG generated: {Bytes} bytes", png.Length);
            return Task.FromResult(png);
        }

        public Task<byte[]> GenerateMovementQRCodeAsync(int movementId)
        {
            _logger.LogInformation("Generating movement QR code for movement: {MovementId}", movementId);

            var content = $"MESTECH:MOV|ID:{movementId}|T:{DateTime.UtcNow:yyyyMMddHHmm}";
            var png = GenerateQRPng(content, 8);

            _logger.LogInformation("Movement QR PNG generated: {Bytes} bytes", png.Length);
            return Task.FromResult(png);
        }

        #endregion

        #region QR Kod Okuma

        public Task<LocationInfo> ReadLocationQRCodeAsync(byte[] qrCodeImage)
        {
            _logger.LogInformation("Reading location QR code ({Bytes} bytes)", qrCodeImage?.Length ?? 0);

            // QR okuma: ZXing.Net ile yapilabilir — su an content parse
            var content = qrCodeImage is not null ? Encoding.UTF8.GetString(qrCodeImage) : string.Empty;

            var locationInfo = new LocationInfo
            {
                Type = "LOCATION",
                BinCode = ExtractField(content, "BIN:"),
                QRCodeVersion = "2.0",
                GeneratedDate = DateTime.UtcNow
            };

            return Task.FromResult(locationInfo);
        }

        public Task<ProductInfo> ReadProductQRCodeAsync(byte[] qrCodeImage)
        {
            _logger.LogInformation("Reading product QR code ({Bytes} bytes)", qrCodeImage?.Length ?? 0);

            var content = qrCodeImage is not null ? Encoding.UTF8.GetString(qrCodeImage) : string.Empty;

            var productInfo = new ProductInfo
            {
                Type = "PRODUCT",
                QRCodeVersion = "2.0",
                GeneratedDate = DateTime.UtcNow
            };

            return Task.FromResult(productInfo);
        }

        #endregion

        #region QR Kod Yonetimi

        public Task<string> GetQRCodeContentAsync(string binCode)
        {
            var qrContent = new LocationInfo
            {
                Type = "LOCATION",
                BinCode = binCode,
                QRCodeVersion = "2.0",
                GeneratedDate = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(qrContent);
            return Task.FromResult(json);
        }

        public Task<bool> ValidateQRCodeAsync(string qrCodeContent)
        {
            if (string.IsNullOrEmpty(qrCodeContent))
                return Task.FromResult(false);

            var isValid = qrCodeContent.Contains("MESTECH:", StringComparison.Ordinal) ||
                         qrCodeContent.Contains("LOCATION", StringComparison.Ordinal) ||
                         qrCodeContent.Contains("PRODUCT", StringComparison.Ordinal) ||
                         qrCodeContent.Contains("MOVEMENT", StringComparison.Ordinal);

            return Task.FromResult(isValid);
        }

        #endregion

        #region Gelismis QR Kod Ozellikleri

        public Task<byte[]> GenerateBulkQRCodeAsync(List<string> binCodes)
        {
            if (binCodes is null || binCodes.Count == 0)
                return Task.FromResult(Array.Empty<byte>());

            // Tek PDF/ZIP icinde birden fazla QR — su an ilk kodu uret
            // Gercek bulk: her bin icin ayri QR, ZIP'le
            return GenerateLocationQRCodeAsync(binCodes.First());
        }

        public Task<byte[]> GenerateDynamicQRCodeAsync(DynamicQRCodeRequest request)
        {
            var content = request?.Content ?? "MESTECH:DYNAMIC";
            var size = request?.Size ?? 10;
            var pixelPerModule = Math.Clamp(size / 25, 3, 20);

            var png = GenerateQRPng(content, pixelPerModule);
            return Task.FromResult(png);
        }

        public async Task<QRCodeValidationResult> ValidateQRCodeWithDetailsAsync(string qrCodeContent)
        {
            var isValid = await ValidateQRCodeAsync(qrCodeContent).ConfigureAwait(false);

            var result = new QRCodeValidationResult
            {
                IsValid = isValid,
                Content = qrCodeContent ?? string.Empty,
                Type = DetectContentType(qrCodeContent),
                ErrorMessage = isValid ? string.Empty : "Gecersiz QR kod formati"
            };

            if (isValid && qrCodeContent is not null)
            {
                result.ParsedData["raw"] = qrCodeContent;
            }

            return result;
        }

        public Task<byte[]> GenerateQRCodeWithTemplateAsync(string content, QRCodeTemplate template)
        {
            var pixelPerModule = Math.Clamp((template?.Size ?? 256) / 25, 3, 20);
            var png = GenerateQRPng(content ?? "MESTECH:TPL", pixelPerModule);
            return Task.FromResult(png);
        }

        public Task<List<QRCodeTemplate>> GetAvailableTemplatesAsync()
        {
            var templates = new List<QRCodeTemplate>
            {
                new() { Name = "Default", Description = "Standart QR kod (256px)", Size = 256, ErrorCorrectionLevel = "M" },
                new() { Name = "Location", Description = "Depo lokasyon QR (200px)", Size = 200, ErrorCorrectionLevel = "H" },
                new() { Name = "Product", Description = "Urun etiketi QR (150px)", Size = 150, ErrorCorrectionLevel = "M" },
                new() { Name = "Cargo", Description = "Kargo takip QR (180px)", Size = 180, ErrorCorrectionLevel = "Q" },
                new() { Name = "Invoice", Description = "e-Fatura QR (120px)", Size = 120, ErrorCorrectionLevel = "H" }
            };

            return Task.FromResult(templates);
        }

        public Task<QRCodeAnalytics> GetQRCodeAnalyticsAsync(string binCode, DateTime? fromDate = null, DateTime? toDate = null)
        {
            // Gercek analitik: scan tracking tablosundan cekilecek
            return Task.FromResult(new QRCodeAnalytics
            {
                BinCode = binCode,
                TotalScans = 0,
                UniqueScanners = 0,
                FirstScan = DateTime.UtcNow,
                LastScan = DateTime.UtcNow
            });
        }

        public Task<List<QRCodeScanHistory>> GetQRCodeScanHistoryAsync(string binCode, int limit = 100)
        {
            // Gercek gecmis: scan tracking tablosundan cekilecek
            return Task.FromResult(new List<QRCodeScanHistory>());
        }

        #endregion

        #region Helpers

        private static string ExtractField(string content, string fieldPrefix)
        {
            if (string.IsNullOrEmpty(content) || !content.Contains(fieldPrefix, StringComparison.Ordinal))
                return string.Empty;

            var start = content.IndexOf(fieldPrefix, StringComparison.Ordinal) + fieldPrefix.Length;
            var end = content.IndexOf('|', start);
            return end > start ? content[start..end] : content[start..];
        }

        private static string DetectContentType(string? content)
        {
            if (string.IsNullOrEmpty(content)) return "UNKNOWN";
            if (content.Contains("LOC", StringComparison.Ordinal)) return "LOCATION";
            if (content.Contains("PRD", StringComparison.Ordinal)) return "PRODUCT";
            if (content.Contains("MOV", StringComparison.Ordinal)) return "MOVEMENT";
            return "CUSTOM";
        }

        #endregion
    }
}
