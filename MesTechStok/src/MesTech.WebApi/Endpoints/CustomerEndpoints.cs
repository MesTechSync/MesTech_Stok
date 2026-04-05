using MediatR;
using MesTech.Application.Commands.CreateCustomer;
using MesTech.Application.Commands.UpdateCustomer;
using MesTech.Application.Features.Crm.Commands.ExportCustomers;
using MesTech.Application.Queries.GetCustomersPaged;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class CustomerEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/customers")
            .WithTags("Customers")
            .RequireRateLimiting("PerApiKey")
            .AddEndpointFilter<Filters.NullResultFilter>();

        // GET /api/v1/customers — paginated customer list with search
        group.MapGet("/", async (
            string? search,
            int? page,
            int? pageSize,
            ISender mediator,
            CancellationToken ct) =>
        {
            var safeSearch = search is { Length: > 500 } ? search[..500] : search;
            var result = await mediator.Send(
                new GetCustomersPagedQuery(safeSearch, page ?? 1, Math.Clamp(pageSize ?? 50, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetCustomersPaged")
        .WithSummary("Sayfalanmış müşteri listesi (arama destekli)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/customers — create a new customer
        group.MapPost("/", async (
            CreateCustomerCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/customers/{result.CustomerId}", result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("CreateCustomer")
        .WithSummary("Yeni müşteri oluştur")
        .Produces(201)
        .Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // PUT /api/v1/customers/{id} — update an existing customer
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCustomerCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var adjusted = command with { Id = id };
            var result = await mediator.Send(adjusted, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("UpdateCustomer")
        .WithSummary("Müşteri bilgilerini güncelle")
        .Produces(200)
        .Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/customers/export — müşteri listesi dışa aktarım (G564)
        group.MapPost("/export", async (
            ExportCustomersCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            if (result.FileData.Length == 0)
                return Results.Problem(detail: "Export produced no data", statusCode: 400);
            return Results.File(result.FileData.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.FileName);
        })
        .WithName("ExportCustomers")
        .WithSummary("Müşteri listesini Excel'e aktar (xlsx/csv)")
        .Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }
}
