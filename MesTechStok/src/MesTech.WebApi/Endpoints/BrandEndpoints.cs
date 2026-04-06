using MediatR;
using MesTech.Application.Queries.GetBrandById;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Marka yönetim endpoint'leri.
/// HH-DEV6-081 FIX: GetBrandByIdQuery handler mevcut, endpoint eksikti.
/// </summary>
public static class BrandEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/brands")
            .WithTags("Brands")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/brands/{id} — marka detayı
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBrandByIdQuery(id), ct);
            return result is not null
                ? Results.Ok(result)
                : Results.Problem(detail: $"Marka {id} bulunamadı.", statusCode: 404);
        })
        .WithName("GetBrandById")
        .WithSummary("Marka detayı — ad, logo, durum bilgisi")
        .Produces<GetBrandByIdResult>(200).Produces(404)
        .CacheOutput("Lookup60s");
    }
}
