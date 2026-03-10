using MediatR;
using MesTech.Application.Queries.GetCategories;

namespace MesTech.WebApi.Endpoints;

public static class CategoryEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/categories").WithTags("Categories");

        // GET /api/v1/categories — list categories (optional active-only filter, default true)
        group.MapGet("/", async (
            ISender mediator,
            CancellationToken ct,
            bool activeOnly = true) =>
        {
            var result = await mediator.Send(
                new GetCategoriesQuery(activeOnly), ct);
            return Results.Ok(result);
        });
    }
}
