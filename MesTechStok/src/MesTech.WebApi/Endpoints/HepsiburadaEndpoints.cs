namespace MesTech.WebApi.Endpoints;

/// <summary>Hepsiburada Swagger endpoint'leri (G10821-DEV6).</summary>
public static class HepsiburadaEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "hepsiburada", "hepsiburada", "Hepsiburada");
}
