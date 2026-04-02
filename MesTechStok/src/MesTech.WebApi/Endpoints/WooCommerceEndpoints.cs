namespace MesTech.WebApi.Endpoints;

/// <summary>WooCommerce Swagger endpoint'leri (G10821-DEV6).</summary>
public static class WooCommerceEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "woocommerce", "woocommerce", "WooCommerce");
}
