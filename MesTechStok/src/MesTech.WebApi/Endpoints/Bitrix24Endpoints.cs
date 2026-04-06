namespace MesTech.WebApi.Endpoints;

/// <summary>Bitrix24 Swagger endpoint'leri — Bitrix24Adapter (PlatformCode=Bitrix24) icin standart route.</summary>
public static class Bitrix24Endpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "bitrix24", "Bitrix24", "Bitrix24");
}
