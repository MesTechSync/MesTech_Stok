namespace MesTech.Application.Interfaces;

/// <summary>
/// GIB mukellef sorgu servisi — VKN/TCKN ile e-Fatura mukellefiyeti kontrol eder.
/// Sonuclar IMemoryCache ile 24 saat cache'lenir.
/// </summary>
public interface IGibMukellefService
{
    /// <summary>VKN veya TCKN ile GIB e-Fatura mukellefiyeti sorgular (cache'li).</summary>
    Task<bool> IsEFaturaMukellefAsync(string vknOrTckn, CancellationToken ct = default);

    /// <summary>Cache'i temizler (test veya admin islemi icin).</summary>
    void ClearCache();
}
