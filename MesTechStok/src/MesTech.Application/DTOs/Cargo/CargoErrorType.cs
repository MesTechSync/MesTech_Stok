namespace MesTech.Application.DTOs.Cargo;

/// <summary>
/// Kargo API hata türleri — her tür için farklı UX aksiyonu.
/// </summary>
public enum CargoErrorType
{
    /// <summary>Hata yok — başarılı operasyon.</summary>
    None = 0,

    /// <summary>API yanıt vermedi → OTOMATİK RETRY.</summary>
    Timeout = 1,

    /// <summary>API key hatalı → AYARLARA YÖNLENDİR.</summary>
    AuthenticationFailed = 2,

    /// <summary>Adres/telefon eksik → FORMU DÜZELT.</summary>
    ValidationError = 3,

    /// <summary>Günlük limit aşıldı → YARIN TEKRAR DENE.</summary>
    QuotaExceeded = 4,

    /// <summary>Kargo firması bakımda → BEKLE.</summary>
    ServiceUnavailable = 5,

    /// <summary>İnternet bağlantısı yok → BAĞLANTIYI KONTROL ET.</summary>
    NetworkError = 6,

    /// <summary>Bilinmeyen hata → LOG + DESTEK.</summary>
    Unknown = 99
}
