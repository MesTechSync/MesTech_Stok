using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Generic platform endpoint helper — DRY: 15 platform × 4 endpoint = 1 helper.
/// products/categories/connection/sync — tum platformlar icin standart endpoint seti.
/// </summary>
public static class PlatformEndpointHelper
{
    /// <summary>
    /// Register 4 standard endpoints for a platform:
    /// GET  /api/v1/{route}/products, /categories, /connection
    /// POST /api/v1/{route}/sync
    /// </summary>
    public static void MapPlatformEndpoints(
        WebApplication app,
        string route,
        string platformCode,
        string displayName)
    {
        var group = app.MapGroup($"/api/v1/{route}")
            .WithTags(displayName)
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        group.MapGet("/products", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve(platformCode);
            if (adapter is null)
                return Results.Problem(detail: $"{displayName} adapter bulunamadi.", statusCode: 503);
            var products = await adapter.PullProductsAsync(ct);
            return Results.Ok(new PlatformProductsResponse(displayName, products.Count, products));
        })
        .WithName($"Get{displayName.Replace(" ", "")}Products")
        .WithSummary($"{displayName} urunlerini cek")
        .Produces(200).ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/categories", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve(platformCode);
            if (adapter is null)
                return Results.Problem(detail: $"{displayName} adapter bulunamadi.", statusCode: 503);
            var categories = await adapter.GetCategoriesAsync(ct);
            return Results.Ok(new PlatformCategoriesResponse(displayName, categories.Count, categories));
        })
        .WithName($"Get{displayName.Replace(" ", "")}Categories")
        .WithSummary($"{displayName} kategori listesi")
        .Produces(200).ProducesProblem(503)
        .CacheOutput("Lookup60s");

        group.MapGet("/connection", async (IAdapterFactory f, CancellationToken ct) =>
        {
            var adapter = f.Resolve(platformCode);
            if (adapter is null)
                return Results.Problem(detail: $"{displayName} adapter bulunamadi.", statusCode: 503);
            var result = await adapter.TestConnectionAsync(new Dictionary<string, string>(), ct);
            return Results.Ok(new PlatformConnectionResponse(
                displayName, result.IsSuccess, result.StoreName,
                result.ProductCount, result.ErrorMessage,
                result.ResponseTime.TotalMilliseconds));
        })
        .WithName($"Test{displayName.Replace(" ", "")}Connection")
        .WithSummary($"{displayName} API baglanti testi")
        .Produces(200).ProducesProblem(503);

        // POST /api/v1/{route}/sync — generic platform sync
        group.MapPost("/sync", async (
            MediatR.ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new SyncPlatformCommand(platformCode, SyncDirection.Bidirectional, null), ct);
            return Results.Ok(result);
        })
        .WithName($"Sync{displayName.Replace(" ", "")}Full")
        .WithSummary($"{displayName} tam senkronizasyon tetikle")
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .Produces(200).Produces(400);
    }

    // ── Typed Response DTOs — Swagger contract stability ──

    public sealed record PlatformProductsResponse(string Platform, int Count, object Products);
    public sealed record PlatformCategoriesResponse(string Platform, int Count, object Categories);
    public sealed record PlatformConnectionResponse(
        string Platform, bool IsConnected, string? StoreName,
        int? ProductCount, string? ErrorMessage, double ResponseTimeMs);
}
