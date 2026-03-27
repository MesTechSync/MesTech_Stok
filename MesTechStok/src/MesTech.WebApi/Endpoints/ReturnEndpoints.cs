using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Commands.ApproveReturn;
using MesTech.Application.Commands.RejectReturn;

namespace MesTech.WebApi.Endpoints;

public static class ReturnEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/returns")
            .WithTags("Returns")
            .RequireRateLimiting("PerApiKey");

        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ISender mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new ApproveReturnCommand(id), ct);
            return Results.Ok(new
            {
                message = "Iade onaylandi — stok geri eklendi",
                returnId = id,
                processedAt = DateTime.UtcNow
            });
        })
        .WithName("ApproveReturn")
        .WithSummary("Iade onay — stok geri + muhasebe ters kayit").Produces(200).Produces(400);

        group.MapPost("/{id:guid}/reject", async (
            Guid id,
            RejectReturnBody body,
            ISender mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new RejectReturnCommand(id, body.Reason), ct);
            return Results.Ok(new StatusResponse("Rejected", body.Reason));
        })
        .WithName("RejectReturn")
        .WithSummary("Iade red — sebep zorunlu").Produces(200).Produces(400);
    }

    public record RejectReturnBody(string Reason);
}
