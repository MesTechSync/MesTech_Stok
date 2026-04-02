namespace MesTech.Application.Interfaces;

/// <summary>
/// Feed XML dosyalarını object storage'a (MinIO) yükler.
/// Feed adapter'ları GenerateFeedAsync sonrası XML'i bu servis ile persist eder.
/// Bucket: mestech-feeds, path: {platform}/{storeId}.xml
/// </summary>
public interface IFeedStorageService
{
    /// <summary>
    /// Feed XML içeriğini MinIO'ya yükler ve public URL döner.
    /// </summary>
    /// <param name="platform">Platform adı (google-merchant, facebook-shop, instagram-shop)</param>
    /// <param name="storeId">Mağaza ID</param>
    /// <param name="xmlContent">Serialized XML string</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Feed'in erişilebilir URL'i</returns>
    Task<string> UploadFeedAsync(string platform, Guid storeId, string xmlContent, CancellationToken ct = default);

    /// <summary>
    /// Mevcut feed XML'ini indirir.
    /// </summary>
    Task<string?> DownloadFeedAsync(string platform, Guid storeId, CancellationToken ct = default);

    /// <summary>
    /// Feed dosyasını siler.
    /// </summary>
    Task DeleteFeedAsync(string platform, Guid storeId, CancellationToken ct = default);
}
