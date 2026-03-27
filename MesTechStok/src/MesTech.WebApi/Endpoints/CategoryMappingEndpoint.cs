using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.CategoryMapping.Commands.MapCategory;
using MesTech.Application.Features.CategoryMapping.Queries.GetCategoryMappings;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class CategoryMappingEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/category-mappings")
            .WithTags("CategoryMappings")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/category-mappings?platform={platform} — kategori eşleştirme listesi
        group.MapGet("/", async (
            Guid tenantId,
            PlatformType? platform,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCategoryMappingsQuery(tenantId, platform), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetCategoryMappings")
        .WithSummary("Platform bazlı kategori eşleştirme listesi").Produces(200).Produces(400);

        // POST /api/v1/category-mappings — yeni kategori eşleştirmesi oluştur
        group.MapPost("/", async (
            MapCategoryCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/category-mappings/{id}", new CreatedResponse(id));
        })
        .WithName("MapCategory")
        .WithSummary("Yeni kategori eşleştirmesi oluştur").Produces(200).Produces(400);
    }
}
