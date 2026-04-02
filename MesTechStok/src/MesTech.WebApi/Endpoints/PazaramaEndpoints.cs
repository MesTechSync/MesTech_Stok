namespace MesTech.WebApi.Endpoints;

/// <summary>Pazarama Swagger endpoint'leri (G10821-DEV6).</summary>
public static class PazaramaEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "pazarama", "pazarama", "Pazarama");
}
