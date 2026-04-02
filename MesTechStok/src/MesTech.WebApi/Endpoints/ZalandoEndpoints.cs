namespace MesTech.WebApi.Endpoints;

/// <summary>Zalando Swagger endpoint'leri (G10821-DEV6).</summary>
public static class ZalandoEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "zalando", "zalando", "Zalando");
}
