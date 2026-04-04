using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Cargo.Queries.GetCargoTrackingList;
using MesTech.Application.Features.Cargo.Queries.GetShipmentLabel;
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

        // GET /api/v1/cargo/tracking — kargo takip listesi
        group.MapGet("/tracking", async (
            Guid tenantId, int? count,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCargoTrackingListQuery(tenantId, count ?? 100), ct);
            return Results.Ok(result);
        })
        .WithName("GetCargoTrackingList")
        .WithSummary("Kargo takip listesi — gönderim durumları")
        .Produces<IReadOnlyList<CargoTrackingItemDto>>(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/cargo/label/{shipmentId} — kargo etiketi
        group.MapGet("/label/{shipmentId}", async (
            string shipmentId, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetShipmentLabelQuery(tenantId, shipmentId), ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 404);
        })
        .WithName("GetShipmentLabel")
        .WithSummary("Kargo etiketi (ZPL/PDF)")
        .Produces<ShipmentLabelResult>(200).Produces(404);
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
