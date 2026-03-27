using MesTech.Application.DTOs;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Domain.Enums;

namespace MesTech.WebApi.Endpoints;

public static class CargoEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/cargo")
            .WithTags("Cargo")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/cargo/providers — kargo sağlayıcı listesi
        group.MapGet("/providers", () =>
        {
            var providers = Enum.GetValues<CargoProvider>()
                .Where(p => p != CargoProvider.None)
                .Select(p => new
                {
                    Id = (int)p,
                    Name = p.ToString(),
                    DisplayName = GetDisplayName(p)
                })
                .ToList();

            return Results.Ok(providers);
        })
        .WithName("GetCargoProviders")
        .WithSummary("Desteklenen kargo sağlayıcı listesi")
        .Produces(200)
        .CacheOutput("Lookup60s");
    }

    private static string GetDisplayName(CargoProvider provider)
        => provider switch
        {
            CargoProvider.YurticiKargo => "Yurtiçi Kargo",
            CargoProvider.ArasKargo => "Aras Kargo",
            CargoProvider.SuratKargo => "Sürat Kargo",
            CargoProvider.MngKargo => "MNG Kargo",
            CargoProvider.PttKargo => "PTT Kargo",
            CargoProvider.Hepsijet => "Hepsijet",
            CargoProvider.UPS => "UPS",
            CargoProvider.Sendeo => "Sendeo",
            _ => provider.ToString()
        };
}
