using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Commands.SaveCompanySettings;
using MesTech.Application.Features.Settings.Commands.UpdateProfileSettings;
using MesTech.Application.Features.Settings.Commands.UpdateStoreSettings;
using MesTech.Application.Features.Settings.Queries.GetCredentialsSettings;
using MesTech.Application.Features.Settings.Queries.GetGeneralSettings;
using MesTech.Application.Features.Settings.Queries.GetProfileSettings;
using MesTech.Application.Features.Settings.Queries.GetStoreSettings;
using Microsoft.AspNetCore.OutputCaching;

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
        .CacheOutput("Lookup60s")
        .WithName("GetSettingsProfile")
        .WithSummary("Kullanici profil ayarlari").Produces(200).Produces(400);

        // PUT /api/v1/settings/profile
        group.MapPut("/profile", async (
            UpdateProfileSettingsCommand command,
            ISender sender, CancellationToken ct) =>
        {
            var success = await sender.Send(command, ct);
            return success
                ? Results.NoContent()
                : Results.NotFound(new { error = "Tenant not found" });
        })
        .WithName("UpdateSettingsProfile")
        .WithSummary("Kullanici profil ayarlarini guncelle").Produces(200).Produces(400);

        // GET /api/v1/settings/credentials
        group.MapGet("/credentials", async (
            Guid tenantId,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCredentialsSettingsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetSettingsCredentials")
        .WithSummary("API kimlik bilgileri listesi").Produces(200).Produces(400);

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
        .CacheOutput("Lookup60s")
        .WithName("GetSettingsNotifications")
        .WithSummary("Bildirim tercihleri (general settings)").Produces(200).Produces(400);

        // GET /api/v1/settings/store — mağaza ayarları
        group.MapGet("/store", async (
            Guid tenantId,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetStoreSettingsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetStoreSettings")
        .WithSummary("Mağaza ayarları — şirket bilgileri, varsayılanlar")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // PUT /api/v1/settings/store — mağaza ayarlarını güncelle
        group.MapPut("/store", async (
            UpdateStoreSettingsCommand command,
            ISender sender, CancellationToken ct) =>
        {
            var success = await sender.Send(command, ct);
            return success
                ? Results.NoContent()
                : Results.Problem(detail: "Store settings update failed", statusCode: 400);
        })
        .WithName("UpdateStoreSettings")
        .WithSummary("Mağaza ayarlarını güncelle")
        .Produces(204).Produces(400);

        // POST /api/v1/settings/company — şirket ayarlarını kaydet (depo dahil)
        group.MapPost("/company", async (
            SaveCompanySettingsCommand command,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("SaveCompanySettings")
        .WithSummary("Şirket ayarlarını kaydet — depolar dahil")
        .Produces(200).Produces(400);
    }
}
