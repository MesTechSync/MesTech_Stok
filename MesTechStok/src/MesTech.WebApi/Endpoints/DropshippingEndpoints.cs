using MediatR;
using MesTech.Application.Features.Dropshipping.Commands.CreateAutoOrder;
using MesTech.Application.Features.Dropshipping.Commands.CreateDropshipSupplier;
using MesTech.Application.Features.Dropshipping.Commands.LinkDropshipProduct;
using MesTech.Application.Features.Dropshipping.Commands.PlaceDropshipOrder;
using MesTech.Application.Features.Dropshipping.Commands.SyncDropshipProducts;
using MesTech.Application.Features.Dropshipping.Commands.SyncSupplierPrices;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipOrders;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipProducts;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipSuppliers;
using MesTech.Application.Features.Dropshipping.Queries.GetSupplierPerformance;

namespace MesTech.WebApi.Endpoints;

public static class DropshippingEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/dropshipping/suppliers")
            .WithTags("Dropshipping")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/dropshipping/suppliers — tedarikçi listesi
        group.MapGet("/", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetDropshipSuppliersQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetDropshipSuppliers")
        .WithSummary("Dropship tedarikçi listesi");

        // POST /api/v1/dropshipping/suppliers — yeni tedarikçi oluştur
        group.MapPost("/", async (
            CreateDropshipSupplierCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/dropshipping/suppliers/{id}", new { id });
        })
        .WithName("CreateDropshipSupplier")
        .WithSummary("Yeni dropship tedarikçi oluştur");

        // POST /api/v1/dropshipping/suppliers/{id}/sync — ürün senkronizasyonu tetikle
        group.MapPost("/{id:guid}/sync", async (
            Guid id, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var syncedCount = await mediator.Send(
                new SyncDropshipProductsCommand(tenantId, id), ct);
            return Results.Ok(new { supplierId = id, syncedProductCount = syncedCount });
        })
        .WithName("SyncDropshipProducts")
        .WithSummary("Tedarikçi ürün senkronizasyonu başlat");

        // ---- Product-level endpoints under /api/v1/dropshipping ----
        var productsGroup = app.MapGroup("/api/v1/dropshipping/products")
            .WithTags("Dropshipping")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/dropshipping/products — dropship ürün listesi
        productsGroup.MapGet("/", async (
            Guid tenantId, bool? isLinked,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetDropshipProductsQuery(tenantId, isLinked), ct);
            return Results.Ok(result);
        })
        .WithName("GetDropshipProducts")
        .WithSummary("Dropship ürün listesi (linked/unlinked filtresi)");

        // POST /api/v1/dropshipping/products/{id}/link — ürün eşleştir
        productsGroup.MapPost("/{id:guid}/link", async (
            Guid id, LinkDropshipProductRequest request,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(
                new LinkDropshipProductCommand(request.TenantId, id, request.MesTechProductId), ct);
            return Results.NoContent();
        })
        .WithName("LinkDropshipProduct")
        .WithSummary("Dropship ürününü MesTech ürünüyle eşleştir");

        // ---- Order-level endpoints under /api/v1/dropshipping ----
        var ordersGroup = app.MapGroup("/api/v1/dropshipping/orders")
            .WithTags("Dropshipping")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/dropshipping/orders — dropship sipariş listesi
        ordersGroup.MapGet("/", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetDropshipOrdersQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetDropshipOrders")
        .WithSummary("Dropship sipariş listesi");

        // POST /api/v1/dropshipping/orders — yeni dropship siparişi oluştur
        ordersGroup.MapPost("/", async (
            PlaceDropshipOrderCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/dropshipping/orders/{id}", new { id });
        })
        .WithName("PlaceDropshipOrder")
        .WithSummary("Dropship sipariş kaydı oluştur");

        // ---- Lifecycle endpoints under /api/v1/dropshipping ----
        var lifecycleGroup = app.MapGroup("/api/v1/dropshipping")
            .WithTags("Dropshipping")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/dropshipping/supplier-performance — tedarikçi performans raporu
        lifecycleGroup.MapGet("/supplier-performance", async (
            Guid tenantId, DateTime? from, DateTime? to,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetSupplierPerformanceQuery(tenantId, from, to), ct);
            return Results.Ok(result);
        })
        .WithName("GetSupplierPerformance")
        .WithSummary("Tedarikçi performans raporu (fulfillment, hız, rating)");

        // POST /api/v1/dropshipping/auto-order — düşük stoklu ürünler için otomatik sipariş
        lifecycleGroup.MapPost("/auto-order", async (
            CreateAutoOrderCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("CreateAutoOrder")
        .WithSummary("Minimum stok altındaki ürünler için otomatik dropship sipariş oluştur");

        // POST /api/v1/dropshipping/price-sync — tedarikçi fiyat senkronizasyonu
        lifecycleGroup.MapPost("/price-sync", async (
            SyncSupplierPricesCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("SyncSupplierPrices")
        .WithSummary("Tedarikçi feed'inden fiyat senkronizasyonu başlat");
    }

    /// <summary>
    /// Request body for linking a dropship product to a MesTech product.
    /// Route provides DropshipProductId; body provides TenantId + MesTechProductId.
    /// </summary>
    public record LinkDropshipProductRequest(Guid TenantId, Guid MesTechProductId);
}
