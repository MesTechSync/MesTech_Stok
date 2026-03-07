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
}
