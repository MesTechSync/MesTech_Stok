using System.Collections.Generic;

namespace MesTechStok.Core.Data.Models;

/// <summary>
/// Sayfalanmış sorgu sonuçları için generic wrapper sınıfı
/// Server-side pagination ve büyük veri setleri için optimize edilmiştir
/// </summary>
/// <typeparam name="T">Sonuç verisi türü</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Mevcut sayfa verileri
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Toplam kayıt sayısı (tüm sayfalardaki)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Mevcut sayfa numarası (1-tabanlı)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Sayfa başına kayıt sayısı
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Toplam sayfa sayısı
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Önceki sayfa var mı?
    /// </summary>
    public bool HasPrevious => Page > 1;

    /// <summary>
    /// Sonraki sayfa var mı?
    /// </summary>
    public bool HasNext => Page < TotalPages;

    /// <summary>
    /// Bu sayfadaki kayıt sayısı
    /// </summary>
    public int CurrentPageItemCount => Items?.Count() ?? 0;

    /// <summary>
    /// Boş sonuç oluşturur
    /// </summary>
    public static PagedResult<T> Empty(int page = 1, int pageSize = 20) => new()
    {
        Items = new List<T>(),
        TotalCount = 0,
        Page = page,
        PageSize = pageSize
    };

    /// <summary>
    /// Başarılı sayfalanmış sonuç oluşturur
    /// </summary>
    public static PagedResult<T> Success(IEnumerable<T> items, int totalCount, int page, int pageSize) => new()
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
