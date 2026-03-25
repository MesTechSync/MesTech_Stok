using MediatR;
using MesTech.Application.Features.Reports.CargoPerformanceReport;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using MesTech.Application.Features.Shipping.Commands.BatchShipOrders;
using MesTech.Application.Features.Shipping.Queries.GetShipmentStatus;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.WebApi.Endpoints;

public static class ShippingEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/shipping")
            .WithTags("Shipping")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/shipping/auto-ship — otomatik kargolama
        group.MapPost("/auto-ship", async (
            AutoShipOrderCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("AutoShipOrder")
        .WithSummary("Siparişi otomatik kargola (kargo firması seçimi dahil)");

        // POST /api/v1/shipping/batch-ship — toplu kargolama
        group.MapPost("/batch-ship", async (
            BatchShipOrdersCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("BatchShipOrders")
        .WithSummary("Birden fazla siparişi toplu kargola");

        // GET /api/v1/shipping/{trackingNumber}/status — kargo takip durumu
        group.MapGet("/{trackingNumber}/status", async (
            string trackingNumber, Guid tenantId, CargoProvider provider,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetShipmentStatusQuery(tenantId, trackingNumber, provider), ct);
            return Results.Ok(result);
        })
        .WithName("GetShipmentStatus")
        .WithSummary("Kargo takip numarasıyla gönderi durumu sorgula");

        // GET /api/v1/shipping/comparison — kargo firma karşılaştırma (son 90 gün)
        group.MapGet("/comparison", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-90);
            var result = await mediator.Send(
                new CargoPerformanceReportQuery(tenantId, startDate, endDate), ct);
            return Results.Ok(result);
        })
        .WithName("GetCargoComparison")
        .WithSummary("Kargo firma karşılaştırma raporu (son 90 gün performans)");

        // GET /api/v1/shipping/track/{trackingNumber} — basit kargo takip (tüm sağlayıcıları dener)
        group.MapGet("/track/{trackingNumber}", async (
            string trackingNumber,
            Guid tenantId,
            ICargoProviderFactory cargoProviderFactory,
            ISender mediator, CancellationToken ct) =>
        {
            var allAdapters = cargoProviderFactory.GetAll();
            foreach (var adapter in allAdapters)
            {
                try
                {
                    var result = await mediator.Send(
                        new GetShipmentStatusQuery(tenantId, trackingNumber, adapter.Provider), ct);
                    if (result is not null && result.Events.Count > 0)
                        return Results.Ok(result);
                }
                catch (InvalidOperationException)
                {
                    // Provider not available, try next
                }
            }

            return Results.NotFound(new { error = $"Tracking number '{trackingNumber}' not found in any carrier" });
        })
        .WithName("TrackShipment")
        .WithSummary("Kargo takip numarasıyla gönderi ara (tüm sağlayıcıları dener)");
    }
}
