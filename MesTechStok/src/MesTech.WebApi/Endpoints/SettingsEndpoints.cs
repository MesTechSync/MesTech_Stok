namespace MesTech.WebApi.Endpoints;

public static class SettingsEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/settings")
            .WithTags("Settings")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/settings/profile
        group.MapGet("/profile", () => Results.Ok(new
        {
            username = "current_user",
            email = "user@mestech.com",
            language = "tr",
            timezone = "Europe/Istanbul",
            dateFormat = "dd.MM.yyyy",
            currency = "TRY"
        }))
        .WithName("GetSettingsProfile")
        .WithSummary("Kullanici profil ayarlari");

        // PUT /api/v1/settings/profile
        group.MapPut("/profile", (object profile) => Results.Ok(new
        {
            success = true,
            message = "Profil guncellendi"
        }))
        .WithName("UpdateSettingsProfile")
        .WithSummary("Kullanici profil ayarlarini guncelle");

        // GET /api/v1/settings/credentials
        group.MapGet("/credentials", () => Results.Ok(new List<object>()))
        .WithName("GetSettingsCredentials")
        .WithSummary("API kimlik bilgileri listesi");

        // GET /api/v1/settings/notifications
        group.MapGet("/notifications", () => Results.Ok(new
        {
            emailNotifications = true,
            pushNotifications = true,
            orderAlerts = true,
            stockAlerts = true,
            syncAlerts = false
        }))
        .WithName("GetSettingsNotifications")
        .WithSummary("Bildirim tercihleri");
    }
}
