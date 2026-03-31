using MediatR;
using MesTech.Application.Features.Shipping.Commands.AutoShipOrder;
using MesTech.Application.Features.Shipping.Queries.DownloadShipmentLabel;

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

        // GET /api/v1/shipments/{id}/label — kargo etiketi indir (G564)
        group.MapGet("/{id:guid}/label", async (
            Guid id,
            Guid tenantId,
            string? format,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new DownloadShipmentLabelQuery(tenantId, id, Format: format ?? "PDF"), ct);
            if (result.LabelData.Length == 0)
                return Results.Problem(detail: "Label data empty", statusCode: 400);
            return Results.File(result.LabelData, result.ContentType, result.FileName);
        })
        .WithName("DownloadShipmentLabel")
        .WithSummary("Kargo etiketi indir — PDF veya PNG formatında")
        .Produces(200).Produces(400);
    }
}
