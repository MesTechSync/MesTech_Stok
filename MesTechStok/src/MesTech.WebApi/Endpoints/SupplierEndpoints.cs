using MesTech.Application.DTOs;
using MesTech.Application.Commands.CreateSupplier;
using MesTech.Application.Commands.UpdateSupplier;
using MesTech.Application.Queries.GetSuppliers;
using MediatR;

namespace MesTech.WebApi.Endpoints;

public static class SupplierEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/suppliers").WithTags("Suppliers").RequireRateLimiting("PerApiKey");

        // GET /api/v1/suppliers — tedarikçi listesi
        group.MapGet("/", async (
            bool? activeOnly,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSuppliersQuery(activeOnly ?? true), ct);
            return Results.Ok(result);
        })
        .WithName("GetSuppliers")
        .WithSummary("Tedarikçi listesi (aktif/tümü filtresi)")
        .Produces(200);

        // POST /api/v1/suppliers — yeni tedarikçi oluştur
        group.MapPost("/", async (
            CreateSupplierCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/suppliers/{result.SupplierId}", new CreatedResponse(result.SupplierId))
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("CreateSupplier")
        .WithSummary("Yeni tedarikçi oluştur")
        .Produces(201)
        .Produces(400);

        // PUT /api/v1/suppliers/{id} — tedarikçi güncelle
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateSupplierCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            if (id != command.Id)
                return Results.BadRequest(ApiResponse<object>.Fail("Route ID and body ID mismatch"));

            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new StatusResponse("updated", $"Supplier {id} updated"))
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("UpdateSupplier")
        .WithSummary("Tedarikçi bilgilerini güncelle")
        .Produces(200)
        .Produces(400);
    }
}
