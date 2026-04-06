namespace MesTech.WebApi.Endpoints;

/// <summary>Amazon EU Swagger endpoint'leri — AmazonEuAdapter (PlatformCode=AmazonEu) icin ayri route.</summary>
public static class AmazonEuEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "amazon-eu", "AmazonEu", "Amazon EU");
}
