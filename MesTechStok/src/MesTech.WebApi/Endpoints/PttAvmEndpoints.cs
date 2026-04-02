namespace MesTech.WebApi.Endpoints;

/// <summary>PttAVM Swagger endpoint'leri (G10821-DEV6).</summary>
public static class PttAvmEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "pttavm", "pttavm", "PttAVM");
}
