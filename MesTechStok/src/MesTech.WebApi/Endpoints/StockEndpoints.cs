using MediatR;
using MesTech.Application.Commands.SyncPlatform;
using MesTech.Application.DTOs;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;
using MesTech.Application.Commands.AddStock;
using MesTech.Application.Commands.AddStockLot;
using MesTech.Application.Commands.AdjustStock;
using MesTech.Application.Commands.RemoveStock;
using MesTech.Application.Commands.TransferStock;
using MesTech.Application.Features.Stock.Commands.CreateStockLot;
using MesTech.Application.Features.Stock.Commands.StartStockCount;
using MesTech.Application.Features.Stock.Queries.GetStockLots;
using MesTech.Application.Features.Stock.Queries.GetStockPlacements;
using MesTech.Application.Features.Stock.Queries.GetStockSummary;
using MesTech.Application.Features.Stock.Queries.GetStockTransfers;
using MesTech.Application.Features.Stock.Queries.GetStockValueReport;
using MesTech.Application.Queries.GetInventoryPaged;
using MesTech.Application.Queries.GetInventoryStatistics;
using MesTech.Application.Queries.GetInventoryValue;
using MesTech.Application.Queries.GetStockMovements;

namespace MesTech.WebApi.Endpoints;

public static class StockEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/stock").WithTags("Stock").RequireRateLimiting("PerApiKey")
            .AddEndpointFilter<Filters.NullResultFilter>();

        // GET /api/v1/stock/movements — list stock movements (optional filters)
        group.MapGet("/movements", async (
            Guid? productId,
            DateTime? from,
            DateTime? to,
            ISender mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetStockMovementsQuery(productId, from, to), ct);
            return Results.Ok(result);
        })
        .WithName("GetStockMovements")
        .WithSummary("Stok hareketleri listesi (ürün, tarih filtresi)")
        .Produces<IReadOnlyList<StockMovementDto>>(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Lookup60s");

        // GET /api/v1/stock/value — total inventory value
        group.MapGet("/value", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInventoryValueQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetInventoryValue")
        .WithSummary("Toplam envanter değeri")
        .Produces<InventoryValueResult>(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Lookup60s");

        // POST /api/v1/stock/add — add stock to a product
        group.MapPost("/add", async (AddStockCommand command, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("AddStock")
        .WithSummary("Ürüne stok girişi")
        .Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/stock/remove — remove stock from a product
        group.MapPost("/remove", async (RemoveStockCommand command, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("RemoveStock")
        .WithSummary("Üründen stok çıkışı")
        .Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/stock/inventory — paged inventory list with filters
        group.MapGet("/inventory", async (
            int? page,
            int? pageSize,
            string? search,
            StockStatusFilter? stockFilter,
            InventorySortOrder? sortOrder,
            ISender mediator,
            CancellationToken ct) =>
        {
            var safeSearch = search is { Length: > 500 } ? search[..500] : search;
            var query = new GetInventoryPagedQuery(
                Page: page ?? 1,
                PageSize: Math.Clamp(pageSize ?? 50, 1, 100),
                SearchTerm: safeSearch,
                StatusFilter: stockFilter ?? StockStatusFilter.All,
                SortOrder: sortOrder ?? InventorySortOrder.ProductName);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("GetInventoryPaged")
        .WithSummary("Sayfalanmış envanter listesi (arama + stok durumu filtresi)")
        .Produces<GetInventoryPagedResult>(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Lookup60s");

        // GET /api/v1/stock/statistics — inventory statistics (totals, values, alerts)
        group.MapGet("/statistics", async (ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInventoryStatisticsQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetInventoryStatistics")
        .WithSummary("Stok istatistikleri (toplam, değer, uyarılar)")
        .Produces<InventoryStatisticsDto>(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Lookup60s");

        // POST /api/v1/stock/transfer — inter-warehouse stock transfer
        group.MapPost("/transfer", async (
            TransferStockCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("TransferStock")
        .WithSummary("Depolar arası stok transferi")
        .Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/stock/adjust — stock adjustment (correction/reconciliation)
        group.MapPost("/adjust", async (
            AdjustStockCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("AdjustStock")
        .WithSummary("Stok düzeltme / sayım farkı girişi")
        .Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/stock/lot — add stock lot (batch tracking)
        group.MapPost("/lot", async (
            AddStockLotCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("AddStockLot")
        .WithSummary("Lot/parti bazlı stok girişi")
        .Produces(200).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/stock/summary — stock summary (totals, value, alerts)
        group.MapGet("/summary", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetStockSummaryQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetStockSummary")
        .WithSummary("Stok özeti — toplam adet, değer, uyarılar")
        .Produces<StockSummaryResult>(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/stock/transfers — recent stock transfers
        group.MapGet("/transfers", async (
            Guid tenantId,
            int? count,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetStockTransfersQuery(tenantId, Math.Clamp(count ?? 100, 1, 200)), ct);
            return Results.Ok(result);
        })
        .WithName("GetStockTransfers")
        .WithSummary("Depolar arası transfer geçmişi")
        .Produces<IReadOnlyList<StockTransferItemDto>>(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Lookup60s");

        // GET /api/v1/stock/value-report — stock value report (FIFO/COGS)
        group.MapGet("/value-report", async (
            Guid tenantId,
            Guid? warehouseId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetStockValueReportQuery(tenantId, warehouseId), ct);
            return Results.Ok(result);
        })
        .WithName("GetStockValueReport")
        .WithSummary("Stok değerleme raporu — depo bazlı, FIFO/COGS")
        .Produces<StockValueReportResult>(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // POST /api/v1/stock/count — start a stock count session
        group.MapPost("/count", async (
            StartStockCountCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/stock/count/{result.SessionId}", result);
        })
        .WithName("StartStockCount")
        .WithSummary("Stok sayım oturumu başlat")
        .Produces(201).ProducesProblem(401).ProducesProblem(429)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/stock/lots — stok lot listesi (FIFO sıralı)
        group.MapGet("/lots", async (
            Guid tenantId, int? limit,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetStockLotsQuery(tenantId, limit ?? 100), ct);
            return Results.Ok(result);
        })
        .WithName("GetStockLots")
        .WithSummary("Stok lot listesi — lot numarası, miktar, maliyet, son kullanma")
        .Produces<IReadOnlyList<StockLotDto>>(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // POST /api/v1/stock/lots — yeni stok lot kaydı oluştur
        group.MapPost("/lots", async (
            CreateStockLotCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/stock/lots/{result.LotId}", result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("CreateStockLot")
        .WithSummary("Yeni stok lot kaydı — lot numarası, miktar, birim maliyet, depo")
        .Produces(201).Produces(400).ProducesProblem(401).ProducesProblem(429)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/stock/placements — stok yerleşim listesi (depo/raf/bölge)
        group.MapGet("/placements", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetStockPlacementsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetStockPlacements")
        .WithSummary("Stok yerleşim listesi — depo, raf, bölge bazlı stok dağılımı")
        .Produces<IReadOnlyList<StockPlacementDto>>(200).ProducesProblem(401).ProducesProblem(429)
        .CacheOutput("Report120s");

        // ═══ HH-D6-002 / HH-DEV6-S01: PLATFORM STOK SENKRONIZASYONU ═══

        // POST /api/v1/stock/sync/{platformType} — stok seviyelerini platforma gönder (Push)
        // Trendyol, HB, N11, Amazon vb. platformlara güncel stok push eder.
        // SyncPlatformCommand(Push) → platform adapter stok güncellemesini tetikler.
        group.MapPost("/sync/{platformType}", async (
            string platformType,
            Guid tenantId,
            ISender mediator,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(platformType))
                return Results.Problem(detail: "Platform tipi boş olamaz.", statusCode: 400);

            var result = await mediator.Send(
                new SyncPlatformCommand(platformType, SyncDirection.Push), ct);

            return result.IsSuccess
                ? Results.Ok(new
                {
                    Platform = platformType,
                    SyncedCount = result.ItemsProcessed,
                    FailedCount = result.ItemsFailed,
                    result.ErrorMessage,
                    SyncedAt = DateTime.UtcNow
                })
                : Results.Problem(detail: result.ErrorMessage, statusCode: 422);
        })
        .WithName("SyncStockToPlatform")
        .WithSummary("Platform stok senkronizasyonu — stok seviyelerini platforma push et (HH-D6-002)")
        .Produces(200).Produces(400).Produces(422).ProducesProblem(429)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }
}
