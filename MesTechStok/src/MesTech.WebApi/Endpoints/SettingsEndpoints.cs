using MediatR;
using MesTech.Application.Features.Settings.Queries.GetCredentialsSettings;
using MesTech.Application.Features.Settings.Queries.GetGeneralSettings;
using MesTech.Application.Features.Settings.Queries.GetProfileSettings;

namespace MesTech.WebApi.Endpoints;

public static class SettingsEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/settings")
            .WithTags("Settings")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/settings/profile
        group.MapGet("/profile", async (
            Guid tenantId,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetProfileSettingsQuery(tenantId), ct);
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(new { error = "Profile settings not found" });
        })
        .WithName("GetSettingsProfile")
        .WithSummary("Kullanici profil ayarlari");

        // PUT /api/v1/settings/profile
        // TODO: UpdateProfileSettingsCommand handler not yet available
        group.MapPut("/profile", (object profile) => Results.Ok(new
        {
            success = false,
            message = "UpdateProfileSettingsCommand handler not yet available",
            status = "not_implemented"
        }))
        .WithName("UpdateSettingsProfile")
        .WithSummary("Kullanici profil ayarlarini guncelle (TODO: handler gerekli)");

        // GET /api/v1/settings/credentials
        group.MapGet("/credentials", async (
            Guid tenantId,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCredentialsSettingsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetSettingsCredentials")
        .WithSummary("API kimlik bilgileri listesi");

        // GET /api/v1/settings/notifications
        group.MapGet("/notifications", async (
            Guid tenantId,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetGeneralSettingsQuery(tenantId), ct);
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(new { error = "General settings not found" });
        })
        .WithName("GetSettingsNotifications")
        .WithSummary("Bildirim tercihleri (general settings)");
    }
}
