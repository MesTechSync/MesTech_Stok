using MesTech.Application.DTOs.Platform;

namespace MesTech.Application.Interfaces;

/// <summary>
/// Urun degerlendirme (review) yonetimi destekleyen platform adaptörleri icin interface.
/// </summary>
public interface IReviewCapableAdapter
{
    /// <summary>Platformdan urun degerlendirmelerini ceker.</summary>
    Task<IReadOnlyList<TrendyolProductReviewDto>> GetProductReviewsAsync(
        int page = 0, int size = 20, CancellationToken ct = default);
}
