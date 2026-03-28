using MediatR;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;

namespace MesTech.WebApi.Endpoints;

public static class ShipmentEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/shipments")
            .WithTags("Shipments")
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/shipments — yeni gönderi oluştur (otomatik kargo seçimi)
        group.MapPost("/", async (
            AutoShipOrderCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.Success
                ? Results.Created($"/api/v1/shipments/{result.ShipmentId}", result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("CreateShipment")
        .WithSummary("Yeni gönderi oluştur (otomatik kargo sağlayıcı seçimi)").Produces(200).Produces(400);
    }
}
