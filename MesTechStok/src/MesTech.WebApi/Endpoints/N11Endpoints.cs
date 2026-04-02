namespace MesTech.WebApi.Endpoints;

/// <summary>N11 Swagger endpoint'leri (G10821-DEV6).</summary>
public static class N11Endpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "n11", "n11", "N11");
}
