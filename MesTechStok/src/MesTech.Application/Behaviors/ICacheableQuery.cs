namespace MesTech.Application.Behaviors;

/// <summary>
/// Marker interface — bu interface'i implement eden Query'ler otomatik cache'lenir.
/// CacheBehavior pipeline'da bu interface'i kontrol eder.
/// Varsayılan cache süresi: 5 dakika. Override etmek için CacheDuration property'sini set edin.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Cache key — her query'nin unique key'i.
    /// Genelde "QueryName_TenantId_Param1_Param2" formatında.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Cache süresi. Varsayılan 5 dakika.
    /// </summary>
    TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
