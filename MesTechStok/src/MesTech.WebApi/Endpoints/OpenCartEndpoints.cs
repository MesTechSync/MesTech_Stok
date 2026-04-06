namespace MesTech.WebApi.Endpoints;

/// <summary>
/// OpenCart standart platform endpoint'leri — categories, connection, sync.
/// D3-044: Mevcut OpenCartProductsEndpoint (custom /products) korunuyor,
/// burası PlatformEndpointHelper ile standart 4 endpoint ekler.
/// Route: /api/v1/opencart (OpenCartProductsEndpoint /api/v1/platforms/opencart kullanıyor — çakışma yok).
/// </summary>
public static class OpenCartEndpoints
{
    public static void Map(WebApplication app)
        => PlatformEndpointHelper.MapPlatformEndpoints(app, "opencart", "opencart", "OpenCart");
}
