namespace MesTechStok.Core.Models
{
    /// <summary>
    /// API yanıt sonucunu kapsayan generic wrapper sınıfı
    /// Success/failure durumları için standardize edilmiş yapı
    /// </summary>
    /// <typeparam name="T">Yanıt verisi türü</typeparam>
    public class ApiResult<T>
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;
        public string ErrorCategory { get; init; } = "None";

        /// <summary>
        /// Başarılı sonuç oluşturur
        /// </summary>
        public static ApiResult<T> Success(T data) => new()
        {
            IsSuccess = true,
            Data = data
        };

        /// <summary>
        /// Hatalı sonuç oluşturur
        /// </summary>
        public static ApiResult<T> Failure(string errorMessage, string category = "None") => new()
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            ErrorCategory = category
        };
    }

    /// <summary>
    /// Basit ürün modeli
    /// OpenCart entegrasyonu için gerekli alanları içerir
    /// </summary>
    public class ProductModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
