namespace MesTech.WebApi.Endpoints;

/// <summary>Ciceksepeti Swagger endpoint'leri (G10821-DEV6).</summary>
public static class CiceksepetiEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "ciceksepeti", "ciceksepeti", "Ciceksepeti");
}
