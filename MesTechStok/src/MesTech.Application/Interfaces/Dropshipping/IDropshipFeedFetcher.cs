namespace MesTech.Application.Interfaces.Dropshipping;

/// <summary>
/// Dropship tedarikci feed'inden urun verisi ceken servis arayuzu.
/// Implementasyon Infrastructure katmaninda (HTTP client ile).
/// K1d-05: SyncDropshipProductsHandler icin clean architecture boundary.
/// </summary>
public interface IDropshipFeedFetcher
{
    /// <summary>
    /// Tedarikci API endpoint'inden urun listesi ceker.
    /// </summary>
    /// <param name="endpoint">Tedarikci API URL'i.</param>
    /// <param name="apiKey">Opsiyonel Bearer token / API key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parse edilmis urun listesi.</returns>
    Task<IReadOnlyList<DropshipFeedItem>> FetchAsync(
        string endpoint, string? apiKey, CancellationToken ct = default);
}

/// <summary>
/// Tedarikci feed'inden parse edilen tek urun kaydi.
/// </summary>
public record DropshipFeedItem(
    string? ExternalId,
    string? Title,
    decimal? Price,
    int? Stock);
