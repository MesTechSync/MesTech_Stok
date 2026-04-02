namespace MesTech.Infrastructure.Integration.Feed;

/// <summary>
/// Configuration options for social feed adapters (Google Merchant, Facebook Shop, Instagram).
/// Bind to "Feed" section in appsettings.json.
/// </summary>
public sealed class FeedOptions
{
    /// <summary>
    /// Base URL for published feed files (MinIO/S3 public endpoint).
    /// Example: "https://cdn.mestech.app/feeds" or "https://minio.mestech.local:9000/feeds"
    /// </summary>
    public string FeedBaseUrl { get; set; } = "https://feeds.mestech.app";
}
