namespace MesTech.Domain.Common;

/// <summary>
/// Sayfalanmış sorgu sonuçları — Domain katmanı.
/// 1000+ ürün senaryosunda bellekte tüm listeyi tutmak yerine sayfalı erişim sağlar.
/// </summary>
public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }

    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static PagedResult<T> Empty(int page = 1, int pageSize = 50) => new()
    {
        Items = Array.Empty<T>(),
        TotalCount = 0,
        Page = page,
        PageSize = pageSize
    };

    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize) => new()
    {
        Items = items,
        TotalCount = totalCount,
        Page = page,
        PageSize = pageSize
    };
}
