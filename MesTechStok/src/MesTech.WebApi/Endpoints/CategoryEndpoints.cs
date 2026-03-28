using MediatR;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.Commands.CreateCategory;
using MesTech.Application.Commands.DeleteCategory;
using MesTech.Application.Commands.UpdateCategory;
using MesTech.Application.Queries.GetCategories;
using MesTech.Application.Queries.GetCategoriesPaged;

namespace MesTech.WebApi.Endpoints;

public static class CategoryEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/categories").WithTags("Categories").RequireRateLimiting("PerApiKey");

        // GET /api/v1/categories — list categories (optional active-only filter, default true)
        group.MapGet("/", async (
            ISender mediator,
            CancellationToken ct,
            bool activeOnly = true) =>
        {
            var result = await mediator.Send(
                new GetCategoriesQuery(activeOnly), ct);
            return Results.Ok(result);
        })
        .WithName("GetCategories")
        .WithSummary("Kategori listesi (aktif/tümü filtresi)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/categories/paged — paginated category list with search
        group.MapGet("/paged", async (
            string? search,
            int? page,
            int? pageSize,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCategoriesPagedQuery(search, page ?? 1, Math.Clamp(pageSize ?? 50, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetCategoriesPaged")
        .WithSummary("Sayfalanmış kategori listesi (arama destekli)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/categories — create a new category
        group.MapPost("/", async (
            CreateCategoryCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/categories/{result.CategoryId}", result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("CreateCategory")
        .WithSummary("Yeni kategori oluştur")
        .Produces(201)
        .Produces(400);

        // PUT /api/v1/categories/{id} — update an existing category
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCategoryCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var adjusted = command with { Id = id };
            var result = await mediator.Send(adjusted, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("UpdateCategory")
        .WithSummary("Kategori güncelle")
        .Produces(200)
        .Produces(400);

        // DELETE /api/v1/categories/{id} — soft-delete a category
        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteCategoryCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("DeleteCategory")
        .WithSummary("Kategori sil (soft-delete)")
        .Produces(204)
        .Produces(400);
    }
}
