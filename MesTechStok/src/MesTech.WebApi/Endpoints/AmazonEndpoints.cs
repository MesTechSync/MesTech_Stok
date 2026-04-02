namespace MesTech.WebApi.Endpoints;

/// <summary>Amazon Swagger endpoint'leri (G10821-DEV6).</summary>
public static class AmazonEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "amazon", "amazon", "Amazon");
}
