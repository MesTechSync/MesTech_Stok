namespace MesTechStok.Core.Integrations.Barcode.Models
{
    public class BarcodeValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public BarcodeFormat DetectedFormat { get; set; }
        public string ProcessedValue { get; set; } = string.Empty;
        public string BarcodeData { get; set; } = string.Empty; // Required by service
        public List<string> ValidationErrors { get; set; } = new(); // Required by service

        public static BarcodeValidationResult Success(string value, BarcodeFormat format)
        {
            return new BarcodeValidationResult
            {
                IsValid = true,
                Message = "Barkod başarıyla doğrulandı",
                DetectedFormat = format,
                ProcessedValue = value,
                BarcodeData = value,
                ValidationErrors = new List<string>()
            };
        }

        public static BarcodeValidationResult Failure(string message)
        {
            return new BarcodeValidationResult
            {
                IsValid = false,
                Message = message,
                DetectedFormat = BarcodeFormat.Unknown,
                ProcessedValue = string.Empty,
                BarcodeData = string.Empty,
                ValidationErrors = new List<string> { message }
            };
        }
    }
}
