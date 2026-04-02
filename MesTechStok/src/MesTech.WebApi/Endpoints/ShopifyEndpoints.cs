namespace MesTech.WebApi.Endpoints;

/// <summary>Shopify Swagger endpoint'leri (G10821-DEV6).</summary>
public static class ShopifyEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "shopify", "shopify", "Shopify");
}
