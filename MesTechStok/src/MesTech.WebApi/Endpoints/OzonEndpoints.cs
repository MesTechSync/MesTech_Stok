namespace MesTech.WebApi.Endpoints;

/// <summary>Ozon Swagger endpoint'leri (G10821-DEV6).</summary>
public static class OzonEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "ozon", "ozon", "Ozon");
}
