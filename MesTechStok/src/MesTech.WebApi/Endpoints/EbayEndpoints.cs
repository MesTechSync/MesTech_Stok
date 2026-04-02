namespace MesTech.WebApi.Endpoints;

/// <summary>eBay Swagger endpoint'leri (G10821-DEV6).</summary>
public static class EbayEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "ebay", "ebay", "eBay");
}
