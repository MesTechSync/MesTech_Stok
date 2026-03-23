using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Text.Json;

namespace MesTechStok.Core.Services.Barcode
{
    /// <summary>
    /// Barkod doğrulama sonucu
    /// </summary>
    public class BarcodeValidationResult
    {
        public bool IsValid { get; set; }
        public string BarcodeType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string? CountryCode { get; set; }
        public string? CompanyPrefix { get; set; }
        public string? ProductCode { get; set; }
        public string? CheckDigit { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
        public DateTime ValidationTime { get; set; } = DateTime.UtcNow;
        public bool IsGS1Compliant { get; set; }
        public string ValidationProvider { get; set; } = string.Empty;
    }

    /// <summary>
    /// Barkod bilgi modeli
    /// </summary>
    public class BarcodeInfo
    {
        public string Barcode { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public string? Manufacturer { get; set; }
        public string? Category { get; set; }
        public decimal? Price { get; set; }
        public string? ImageUrl { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// GS1 barkod doğrulama ayarları
    /// </summary>
    public class BarcodeValidationSettings
    {
        public bool EnableGS1Validation { get; set; } = true;
        public bool EnableOnlineLookup { get; set; } = true;
        public string? UpcDatabaseApiKey { get; set; }
        public string? BarcodeLookupApiKey { get; set; }
        public int ValidationTimeoutSeconds { get; set; } = 10;
        public bool StrictValidation { get; set; } = false;
        public bool CacheResults { get; set; } = true;
        public int CacheExpirationHours { get; set; } = 24;
        public List<string> AllowedCountryCodes { get; set; } = new();
        public List<string> RestrictedPrefixes { get; set; } = new();
    }

    /// <summary>
    /// Barkod doğrulama servis interface
    /// </summary>
    public interface IBarcodeValidationService
    {
        Task<BarcodeValidationResult> ValidateBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
        Task<BarcodeInfo?> LookupBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
        Task<bool> IsGS1CompliantAsync(string barcode, CancellationToken cancellationToken = default);
        Task<string> GenerateCheckDigitAsync(string partialBarcode, CancellationToken cancellationToken = default);
        Task<bool> TestValidationServiceAsync(CancellationToken cancellationToken = default);
        BarcodeValidationResult ValidateFormat(string barcode);
        string GetBarcodeType(string barcode);
    }

    /// <summary>
    /// GS1 standart barkod doğrulama servisi
    /// </summary>
    public class GS1BarcodeValidationService : IBarcodeValidationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GS1BarcodeValidationService> _logger;
        private readonly BarcodeValidationSettings _settings;
        private readonly JsonSerializerOptions _jsonOptions;

        private static readonly Dictionary<string, BarcodeValidationResult> _validationCache = new();
        private static readonly Dictionary<string, BarcodeInfo> _lookupCache = new();
        private static readonly Dictionary<string, DateTime> _cacheExpiry = new();

        // GS1 prefix mapping for country codes
        private static readonly Dictionary<string, string> GS1CountryPrefixes = new()
        {
            {"00-19", "US/CA"}, {"20-29", "Store"}, {"30-37", "US"}, {"40-49", "DE"},
            {"50", "GB"}, {"54", "BE/LU"}, {"57", "DK"}, {"59", "FI"}, {"60-61", "MY"},
            {"62", "CN"}, {"64", "FI"}, {"69", "CN"}, {"70-71", "NO"}, {"72", "IS"},
            {"73", "SE"}, {"74", "GT"}, {"75", "MX"}, {"76", "CH"}, {"77", "AR"},
            {"78", "CL"}, {"79", "UY"}, {"80", "IT"}, {"81", "JP"}, {"82", "KR"},
            {"83", "PL"}, {"84", "ES"}, {"85", "CU"}, {"86", "TR"}, {"87", "NL"},
            {"88", "KR"}, {"89", "TR"}, {"90-91", "AT"}, {"92", "MT"}, {"93", "AU"},
            {"94", "NZ"}, {"95", "MY"}, {"96", "PK"}, {"977", "Serial"}, {"978-979", "ISBN"},
            {"980", "Refund"}, {"981-984", "Coupon"}, {"99", "Coupon"}
        };

        public GS1BarcodeValidationService(
            HttpClient httpClient,
            ILogger<GS1BarcodeValidationService> logger,
            IOptions<BarcodeValidationSettings> settings)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value;

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.ValidationTimeoutSeconds);
        }

        /// <summary>
        /// Barkod doğrulama (format + GS1 + online)
        /// </summary>
        public async Task<BarcodeValidationResult> ValidateBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            if (string.IsNullOrWhiteSpace(barcode))
            {
                return new BarcodeValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Barkod boş olamaz",
                    ValidationProvider = "GS1Validator"
                };
            }

            barcode = barcode.Trim();
            var cacheKey = $"validation_{barcode}";

            try
            {
                // Cache kontrolü
                if (_settings.CacheResults && IsCacheValid(cacheKey))
                {
                    _logger.LogDebug("[BarcodeValidation] Returning cached validation result for {Barcode}", barcode);
                    return _validationCache[cacheKey];
                }

                _logger.LogDebug("[BarcodeValidation] Validating barcode {Barcode}, CorrelationId={CorrelationId}",
                    barcode, MesTechStok.Core.Diagnostics.CorrelationContext.CurrentId);

                // 1. Format doğrulama
                var formatResult = ValidateFormat(barcode);
                if (!formatResult.IsValid)
                {
                    return CacheResult(cacheKey, formatResult);
                }

                // 2. GS1 uyumluluk kontrolü
                if (_settings.EnableGS1Validation)
                {
                    var gs1Result = await ValidateGS1ComplianceAsync(barcode, cancellationToken);
                    if (!gs1Result.IsValid && _settings.StrictValidation)
                    {
                        return CacheResult(cacheKey, gs1Result);
                    }

                    // GS1 metadata'sını format sonucuna ekle
                    foreach (var metadata in gs1Result.Metadata)
                    {
                        formatResult.Metadata[metadata.Key] = metadata.Value;
                    }
                    formatResult.IsGS1Compliant = gs1Result.IsGS1Compliant;
                }

                // 3. Online doğrulama (opsiyonel)
                if (_settings.EnableOnlineLookup)
                {
                    try
                    {
                        var onlineResult = await ValidateOnlineAsync(barcode, cancellationToken);
                        if (onlineResult != null)
                        {
                            formatResult.Metadata["OnlineValidation"] = onlineResult;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[BarcodeValidation] Online validation failed for {Barcode}", barcode);
                        formatResult.Metadata["OnlineValidationError"] = ex.Message;
                    }
                }

                formatResult.ValidationProvider = "GS1Validator";
                formatResult.ValidationTime = DateTime.UtcNow;

                _logger.LogInformation("[BarcodeValidation] Validation completed for {Barcode}: Valid={IsValid}, Type={Type}, GS1={IsGS1}",
                    barcode, formatResult.IsValid, formatResult.BarcodeType, formatResult.IsGS1Compliant);

                return CacheResult(cacheKey, formatResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BarcodeValidation] Validation error for {Barcode}", barcode);

                return new BarcodeValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Doğrulama hatası: {ex.Message}",
                    BarcodeType = GetBarcodeType(barcode),
                    ValidationProvider = "GS1Validator",
                    ValidationTime = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Format doğrulama (check digit, uzunluk, karakter kontrolü)
        /// </summary>
        public BarcodeValidationResult ValidateFormat(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return new BarcodeValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Barkod boş olamaz"
                };
            }

            barcode = barcode.Trim();
            var barcodeType = GetBarcodeType(barcode);
            var result = new BarcodeValidationResult
            {
                BarcodeType = barcodeType,
                ValidationTime = DateTime.UtcNow
            };

            try
            {
                switch (barcodeType)
                {
                    case "EAN-13":
                        return ValidateEAN13(barcode);

                    case "EAN-8":
                        return ValidateEAN8(barcode);

                    case "UPC-A":
                        return ValidateUPCA(barcode);

                    case "UPC-E":
                        return ValidateUPCE(barcode);

                    case "Code128":
                        return ValidateCode128(barcode);

                    case "Code39":
                        return ValidateCode39(barcode);

                    default:
                        result.IsValid = false;
                        result.ErrorMessage = $"Desteklenmeyen barkod türü: {barcodeType}";
                        return result;
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Format doğrulama hatası: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Barkod türünü belirler
        /// </summary>
        public string GetBarcodeType(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return "Unknown";

            barcode = barcode.Trim();

            // EAN-13 (13 rakam)
            if (Regex.IsMatch(barcode, @"^\d{13}$"))
                return "EAN-13";

            // EAN-8 (8 rakam)
            if (Regex.IsMatch(barcode, @"^\d{8}$"))
                return "EAN-8";

            // UPC-A (12 rakam)
            if (Regex.IsMatch(barcode, @"^\d{12}$"))
                return "UPC-A";

            // UPC-E (6-8 rakam, genelde 8)
            if (Regex.IsMatch(barcode, @"^\d{6,8}$"))
                return "UPC-E";

            // Code128 (değişken uzunluk, ASCII)
            if (barcode.Length >= 1 && barcode.Length <= 48 &&
                barcode.All(c => c >= 32 && c <= 126))
                return "Code128";

            // Code39 (değişken uzunluk, alfanumerik + özel karakterler)
            if (Regex.IsMatch(barcode, @"^[0-9A-Z\-\.\s\$\/\+\%\*]+$"))
                return "Code39";

            return "Unknown";
        }

        /// <summary>
        /// Online barkod bilgi arama
        /// </summary>
        public async Task<BarcodeInfo?> LookupBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
        {
            using var corr = MesTechStok.Core.Diagnostics.CorrelationContext.StartNew();

            if (!_settings.EnableOnlineLookup)
            {
                _logger.LogDebug("[BarcodeValidation] Online lookup is disabled");
                return null;
            }

            var cacheKey = $"lookup_{barcode}";

            try
            {
                // Cache kontrolü
                if (_settings.CacheResults && IsCacheValid(cacheKey))
                {
                    _logger.LogDebug("[BarcodeValidation] Returning cached lookup result for {Barcode}", barcode);
                    return _lookupCache[cacheKey];
                }

                BarcodeInfo? result = null;

                // UPC Database API kullanarak arama
                if (!string.IsNullOrWhiteSpace(_settings.UpcDatabaseApiKey))
                {
                    result = await LookupUpcDatabaseAsync(barcode, cancellationToken);
                }

                // Barcode Lookup API kullanarak arama (fallback)
                if (result == null && !string.IsNullOrWhiteSpace(_settings.BarcodeLookupApiKey))
                {
                    result = await LookupBarcodeLookupApiAsync(barcode, cancellationToken);
                }

                if (result != null && _settings.CacheResults)
                {
                    _lookupCache[cacheKey] = result;
                    _cacheExpiry[cacheKey] = DateTime.UtcNow.AddHours(_settings.CacheExpirationHours);
                }

                _logger.LogInformation("[BarcodeValidation] Lookup completed for {Barcode}: Found={Found}",
                    barcode, result != null);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BarcodeValidation] Lookup error for {Barcode}", barcode);
                return null;
            }
        }

        /// <summary>
        /// GS1 uyumluluk kontrolü
        /// </summary>
        public async Task<bool> IsGS1CompliantAsync(string barcode, CancellationToken cancellationToken = default)
        {
            var result = await ValidateGS1ComplianceAsync(barcode, cancellationToken);
            return result.IsGS1Compliant;
        }

        /// <summary>
        /// Check digit üretimi
        /// </summary>
        public async Task<string> GenerateCheckDigitAsync(string partialBarcode, CancellationToken cancellationToken = default)
        {
            await Task.Yield(); // Async method requirement

            if (string.IsNullOrWhiteSpace(partialBarcode))
                return "0";

            var barcodeType = GetBarcodeType(partialBarcode + "0"); // Temporary check digit

            switch (barcodeType)
            {
                case "EAN-13":
                case "UPC-A":
                    return CalculateEAN13CheckDigit(partialBarcode);

                case "EAN-8":
                    return CalculateEAN8CheckDigit(partialBarcode);

                default:
                    return "0";
            }
        }

        /// <summary>
        /// Doğrulama servisi test
        /// </summary>
        public async Task<bool> TestValidationServiceAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Test barkodları ile doğrulama testi
                var testBarcodes = new[]
                {
                    "8680632000014", // EAN-13 test
                    "12345678901",   // UPC-A test
                    "1234567"        // EAN-8 test
                };

                foreach (var testBarcode in testBarcodes)
                {
                    var result = await ValidateBarcodeAsync(testBarcode, cancellationToken);
                    if (result == null)
                        return false;
                }

                _logger.LogInformation("[BarcodeValidation] Service test completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BarcodeValidation] Service test failed");
                return false;
            }
        }

        #region Private Methods

        private async Task<BarcodeValidationResult> ValidateGS1ComplianceAsync(string barcode, CancellationToken cancellationToken)
        {
            var result = new BarcodeValidationResult
            {
                BarcodeType = GetBarcodeType(barcode),
                ValidationTime = DateTime.UtcNow
            };

            try
            {
                // GS1 prefix kontrolü
                if (barcode.Length >= 3)
                {
                    var prefix = barcode.Substring(0, 3);
                    var countryInfo = GetCountryFromPrefix(prefix);

                    if (!string.IsNullOrEmpty(countryInfo))
                    {
                        result.CountryCode = countryInfo;
                        result.Metadata["CountryInfo"] = countryInfo;
                        result.IsGS1Compliant = true;

                        // Restricted prefix kontrolü
                        if (_settings.RestrictedPrefixes.Contains(prefix))
                        {
                            result.IsValid = false;
                            result.ErrorMessage = $"Kısıtlı GS1 prefix: {prefix}";
                            return result;
                        }

                        // Allowed country kontrolü
                        if (_settings.AllowedCountryCodes.Any() &&
                            !_settings.AllowedCountryCodes.Contains(countryInfo))
                        {
                            result.IsValid = false;
                            result.ErrorMessage = $"İzin verilmeyen ülke kodu: {countryInfo}";
                            return result;
                        }
                    }
                    else
                    {
                        result.IsGS1Compliant = false;
                        result.Metadata["GS1Warning"] = $"Bilinmeyen GS1 prefix: {prefix}";
                    }
                }

                // Company prefix çıkarımı (EAN-13 için)
                if (barcode.Length == 13 && result.IsGS1Compliant)
                {
                    result.CompanyPrefix = barcode.Substring(0, 7); // İlk 7 hane
                    result.ProductCode = barcode.Substring(7, 5);   // Sonraki 5 hane
                    result.CheckDigit = barcode.Substring(12, 1);   // Son hane

                    result.Metadata["CompanyPrefix"] = result.CompanyPrefix;
                    result.Metadata["ProductCode"] = result.ProductCode;
                }

                result.IsValid = result.IsGS1Compliant || !_settings.StrictValidation;

                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.IsGS1Compliant = false;
                result.ErrorMessage = $"GS1 doğrulama hatası: {ex.Message}";
                return result;
            }
        }

        private async Task<object?> ValidateOnlineAsync(string barcode, CancellationToken cancellationToken)
        {
            // Online doğrulama için external API çağrısı
            // Bu örnekte basit bir mock response dönüyoruz
            await Task.Delay(100, cancellationToken);

            return new
            {
                IsValidOnline = true,
                Source = "OnlineValidation",
                Timestamp = DateTime.UtcNow,
                AdditionalInfo = "Online validation completed"
            };
        }

        private async Task<BarcodeInfo?> LookupUpcDatabaseAsync(string barcode, CancellationToken cancellationToken)
        {
            try
            {
                var requestUrl = $"https://api.upcitemdb.com/prod/trial/lookup?upc={barcode}";
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("key", _settings.UpcDatabaseApiKey);

                var response = await _httpClient.GetAsync(requestUrl, cancellationToken);

                if (!response.IsSuccessStatusCode)
                    return null;

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var upcResponse = JsonSerializer.Deserialize<UpcDatabaseResponse>(jsonContent, _jsonOptions);

                if (upcResponse?.Items?.Any() != true)
                    return null;

                var item = upcResponse.Items.First();

                return new BarcodeInfo
                {
                    Barcode = barcode,
                    Type = GetBarcodeType(barcode),
                    ProductName = item.Title,
                    Manufacturer = item.Brand,
                    Category = item.Category,
                    ImageUrl = item.Images?.FirstOrDefault(),
                    Source = "UpcDatabase",
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["Description"] = item.Description ?? string.Empty,
                        ["Size"] = item.Size ?? string.Empty,
                        ["Model"] = item.Model ?? string.Empty
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[BarcodeValidation] UPC Database lookup failed for {Barcode}", barcode);
                return null;
            }
        }

        private async Task<BarcodeInfo?> LookupBarcodeLookupApiAsync(string barcode, CancellationToken cancellationToken)
        {
            // BarcodeLookup API implementation would go here
            // This is a placeholder for the actual implementation
            await Task.Delay(100, cancellationToken);
            return null;
        }

        private BarcodeValidationResult ValidateEAN13(string barcode)
        {
            var result = new BarcodeValidationResult { BarcodeType = "EAN-13" };

            if (barcode.Length != 13 || !barcode.All(char.IsDigit))
            {
                result.IsValid = false;
                result.ErrorMessage = "EAN-13 13 rakamdan oluşmalıdır";
                return result;
            }

            var calculatedCheckDigit = CalculateEAN13CheckDigit(barcode.Substring(0, 12));
            var actualCheckDigit = barcode[12].ToString();

            result.IsValid = calculatedCheckDigit == actualCheckDigit;
            result.CheckDigit = actualCheckDigit;
            result.Metadata["CalculatedCheckDigit"] = calculatedCheckDigit;

            if (!result.IsValid)
            {
                result.ErrorMessage = $"Check digit uyumsuz. Beklenen: {calculatedCheckDigit}, Mevcut: {actualCheckDigit}";
            }

            return result;
        }

        private BarcodeValidationResult ValidateEAN8(string barcode)
        {
            var result = new BarcodeValidationResult { BarcodeType = "EAN-8" };

            if (barcode.Length != 8 || !barcode.All(char.IsDigit))
            {
                result.IsValid = false;
                result.ErrorMessage = "EAN-8 8 rakamdan oluşmalıdır";
                return result;
            }

            var calculatedCheckDigit = CalculateEAN8CheckDigit(barcode.Substring(0, 7));
            var actualCheckDigit = barcode[7].ToString();

            result.IsValid = calculatedCheckDigit == actualCheckDigit;
            result.CheckDigit = actualCheckDigit;
            result.Metadata["CalculatedCheckDigit"] = calculatedCheckDigit;

            if (!result.IsValid)
            {
                result.ErrorMessage = $"Check digit uyumsuz. Beklenen: {calculatedCheckDigit}, Mevcut: {actualCheckDigit}";
            }

            return result;
        }

        private BarcodeValidationResult ValidateUPCA(string barcode)
        {
            var result = new BarcodeValidationResult { BarcodeType = "UPC-A" };

            if (barcode.Length != 12 || !barcode.All(char.IsDigit))
            {
                result.IsValid = false;
                result.ErrorMessage = "UPC-A 12 rakamdan oluşmalıdır";
                return result;
            }

            // UPC-A check digit calculation is same as EAN-13
            var calculatedCheckDigit = CalculateEAN13CheckDigit(barcode.Substring(0, 11));
            var actualCheckDigit = barcode[11].ToString();

            result.IsValid = calculatedCheckDigit == actualCheckDigit;
            result.CheckDigit = actualCheckDigit;
            result.Metadata["CalculatedCheckDigit"] = calculatedCheckDigit;

            if (!result.IsValid)
            {
                result.ErrorMessage = $"Check digit uyumsuz. Beklenen: {calculatedCheckDigit}, Mevcut: {actualCheckDigit}";
            }

            return result;
        }

        private BarcodeValidationResult ValidateUPCE(string barcode)
        {
            var result = new BarcodeValidationResult { BarcodeType = "UPC-E" };

            if ((barcode.Length != 6 && barcode.Length != 8) || !barcode.All(char.IsDigit))
            {
                result.IsValid = false;
                result.ErrorMessage = "UPC-E 6 veya 8 rakamdan oluşmalıdır";
                return result;
            }

            // UPC-E validation logic would be more complex
            // For now, just validate format
            result.IsValid = true;

            return result;
        }

        private BarcodeValidationResult ValidateCode128(string barcode)
        {
            var result = new BarcodeValidationResult { BarcodeType = "Code128" };

            if (barcode.Length < 1 || barcode.Length > 48)
            {
                result.IsValid = false;
                result.ErrorMessage = "Code128 1-48 karakter arasında olmalıdır";
                return result;
            }

            // Check for valid ASCII characters
            if (!barcode.All(c => c >= 32 && c <= 126))
            {
                result.IsValid = false;
                result.ErrorMessage = "Code128 geçersiz karakter içeriyor";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        private BarcodeValidationResult ValidateCode39(string barcode)
        {
            var result = new BarcodeValidationResult { BarcodeType = "Code39" };

            if (!Regex.IsMatch(barcode, @"^[0-9A-Z\-\.\s\$\/\+\%\*]+$"))
            {
                result.IsValid = false;
                result.ErrorMessage = "Code39 geçersiz karakter içeriyor";
                return result;
            }

            result.IsValid = true;
            return result;
        }

        private string CalculateEAN13CheckDigit(string partialBarcode)
        {
            if (partialBarcode.Length != 12)
                return "0";

            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = int.Parse(partialBarcode[i].ToString());
                sum += (i % 2 == 0) ? digit : digit * 3;
            }

            int checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit.ToString();
        }

        private string CalculateEAN8CheckDigit(string partialBarcode)
        {
            if (partialBarcode.Length != 7)
                return "0";

            int sum = 0;
            for (int i = 0; i < 7; i++)
            {
                int digit = int.Parse(partialBarcode[i].ToString());
                sum += (i % 2 == 0) ? digit * 3 : digit;
            }

            int checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit.ToString();
        }

        private string GetCountryFromPrefix(string prefix)
        {
            foreach (var kvp in GS1CountryPrefixes)
            {
                var range = kvp.Key;
                if (range.Contains("-"))
                {
                    var parts = range.Split('-');
                    if (parts.Length == 2 &&
                        int.TryParse(parts[0], out int start) &&
                        int.TryParse(parts[1], out int end) &&
                        int.TryParse(prefix, out int prefixNum))
                    {
                        if (prefixNum >= start && prefixNum <= end)
                            return kvp.Value;
                    }
                }
                else if (range == prefix)
                {
                    return kvp.Value;
                }
            }
            return string.Empty;
        }

        private bool IsCacheValid(string cacheKey)
        {
            return _cacheExpiry.ContainsKey(cacheKey) && _cacheExpiry[cacheKey] > DateTime.UtcNow;
        }

        private BarcodeValidationResult CacheResult(string cacheKey, BarcodeValidationResult result)
        {
            if (_settings.CacheResults)
            {
                _validationCache[cacheKey] = result;
                _cacheExpiry[cacheKey] = DateTime.UtcNow.AddHours(_settings.CacheExpirationHours);
            }
            return result;
        }

        #endregion
    }

    #region External API Models

    public class UpcDatabaseResponse
    {
        public List<UpcDatabaseItem>? Items { get; set; }
    }

    public class UpcDatabaseItem
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? Category { get; set; }
        public string? Size { get; set; }
        public List<string>? Images { get; set; }
    }

    #endregion

    /// <summary>
    /// Mock barkod doğrulama servisi (geliştirme için)
    /// </summary>
    public class MockBarcodeValidationService : IBarcodeValidationService
    {
        private readonly ILogger<MockBarcodeValidationService> _logger;

        public MockBarcodeValidationService(ILogger<MockBarcodeValidationService> logger)
        {
            _logger = logger;
        }

        public async Task<BarcodeValidationResult> ValidateBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);

            var barcodeType = GetBarcodeType(barcode);
            var isValid = !string.IsNullOrWhiteSpace(barcode) && barcode.Length >= 6;

            _logger.LogDebug("[BarcodeValidation] Mock validation for {Barcode}: {IsValid}", barcode, isValid);

            return new BarcodeValidationResult
            {
                IsValid = isValid,
                BarcodeType = barcodeType,
                IsGS1Compliant = isValid && (barcodeType == "EAN-13" || barcodeType == "EAN-8"),
                ValidationProvider = "Mock",
                ValidationTime = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["MockValidation"] = true,
                    ["ValidationMode"] = "Development"
                }
            };
        }

        public async Task<BarcodeInfo?> LookupBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
        {
            await Task.Delay(100, cancellationToken);

            return new BarcodeInfo
            {
                Barcode = barcode,
                Type = GetBarcodeType(barcode),
                ProductName = "Mock Product",
                Manufacturer = "Mock Manufacturer",
                Category = "Mock Category",
                Source = "Mock",
                AdditionalData = new Dictionary<string, object>
                {
                    ["MockData"] = true
                }
            };
        }

        public async Task<bool> IsGS1CompliantAsync(string barcode, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            var type = GetBarcodeType(barcode);
            return type == "EAN-13" || type == "EAN-8";
        }

        public async Task<string> GenerateCheckDigitAsync(string partialBarcode, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            return "0"; // Mock check digit
        }

        public async Task<bool> TestValidationServiceAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            return true;
        }

        public BarcodeValidationResult ValidateFormat(string barcode)
        {
            return new BarcodeValidationResult
            {
                IsValid = !string.IsNullOrWhiteSpace(barcode),
                BarcodeType = GetBarcodeType(barcode),
                ValidationProvider = "Mock"
            };
        }

        public string GetBarcodeType(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return "Unknown";

            return barcode.Length switch
            {
                8 => "EAN-8",
                12 => "UPC-A",
                13 => "EAN-13",
                _ => "Code128"
            };
        }
    }
}
