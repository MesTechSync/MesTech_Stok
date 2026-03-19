namespace MesTech.Application.DTOs.Cargo;

/// <summary>
/// Her kargo operasyonunun sonucu — başarı/hata + hata detayı + retry bilgisi.
/// UI bu sonuca göre kullanıcıya uygun mesaj ve aksiyon gösterir.
/// </summary>
public record CargoOperationResult
{
    /// <summary>Operasyon başarılı mı?</summary>
    public bool Success { get; init; }

    /// <summary>Kargo takip numarası (başarılı ise).</summary>
    public string? TrackingNumber { get; init; }

    /// <summary>Hata mesajı (başarısız ise).</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Hata türü — UI aksiyonu belirler.</summary>
    public CargoErrorType ErrorType { get; init; }

    /// <summary>Yeniden denenebilir mi?</summary>
    public bool IsRetryable { get; init; }

    /// <summary>Kaçıncı deneme.</summary>
    public int RetryCount { get; init; }

    /// <summary>Önerilen yeniden deneme bekleme süresi.</summary>
    public TimeSpan? SuggestedRetryDelay { get; init; }

    /// <summary>Kargo firmasının ham yanıtı (debug için).</summary>
    public string? ProviderRawResponse { get; init; }

    /// <summary>Başarılı sonuç oluşturur.</summary>
    public static CargoOperationResult Ok(string trackingNumber) => new()
    {
        Success = true,
        TrackingNumber = trackingNumber,
        ErrorType = CargoErrorType.None
    };

    /// <summary>Hatalı sonuç oluşturur.</summary>
    public static CargoOperationResult Fail(
        CargoErrorType errorType,
        string message,
        bool isRetryable = false,
        int retryCount = 0) => new()
    {
        Success = false,
        ErrorMessage = message,
        ErrorType = errorType,
        IsRetryable = isRetryable,
        RetryCount = retryCount,
        SuggestedRetryDelay = errorType == CargoErrorType.Timeout
            ? TimeSpan.FromSeconds(5)
            : null
    };
}
