namespace MesTech.Infrastructure.Caching;

public static class CacheKeys
{
    public const string ProductPrefix = "product:";
    public const string CategoryPrefix = "category:";
    public const string BrandPrefix = "brand:";
    public const string HealthPrefix = "health:";

    public static readonly TimeSpan ProductTTL = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan CategoryTTL = TimeSpan.FromHours(24);
    public static readonly TimeSpan BrandTTL = TimeSpan.FromHours(1);
    public static readonly TimeSpan HealthTTL = TimeSpan.FromMinutes(1);

    public static string Product(Guid id) => $"{ProductPrefix}{id}";
    public static string Product(string sku) => $"{ProductPrefix}sku:{sku}";
    public static string Categories(string platform) => $"{CategoryPrefix}{platform}:all";
    public static string Brands(string platform, string prefix) => $"{BrandPrefix}{platform}:{prefix}";
    public static string Health(string platform) => $"{HealthPrefix}{platform}";

    // Dropshipping cache keys — ENT-DROP-SENTEZ-001 Sprint A
    public const string DropshippingPoolList = "dropshipping:pools:{tenantId}";
    public const string DropshippingPoolProducts = "dropshipping:pool:{poolId}:products";
    public const string SupplierFeedList = "dropshipping:feeds:{tenantId}";
    public const string SupplierFeedHealth = "dropshipping:feed:{feedId}:health";
    public const string SupplierReliabilityScore = "dropshipping:feed:{feedId}:reliability";
    public const string FeedImportHistory = "dropshipping:feed:{feedId}:import-history";

    public static readonly TimeSpan DropshippingPoolTTL = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan DropshippingFeedListTTL = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan DropshippingFeedHealthTTL = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan DropshippingReliabilityTTL = TimeSpan.FromHours(1);

    public static string DropshippingPools(string tenantId) => $"dropshipping:pools:{tenantId}";
    public static string DropshippingPoolProductList(string poolId) => $"dropshipping:pool:{poolId}:products";
    public static string DropshippingFeeds(string tenantId) => $"dropshipping:feeds:{tenantId}";
    public static string DropshippingFeedHealth(string feedId) => $"dropshipping:feed:{feedId}:health";
    public static string DropshippingReliability(string feedId) => $"dropshipping:feed:{feedId}:reliability";
    public static string DropshippingImportHistory(string feedId) => $"dropshipping:feed:{feedId}:import-history";
}
