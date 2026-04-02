namespace MesTech.WebApi.Endpoints;

/// <summary>Etsy Swagger endpoint'leri (G10821-DEV6).</summary>
public static class EtsyEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "etsy", "etsy", "Etsy");
}
