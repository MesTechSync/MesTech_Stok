using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Platform.Commands.TriggerSync;
using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// D12-22: Import Pipeline Endpoints — Entegratör ürün çekme + sync yönetimi.
/// POST /import/trigger, GET /import/status, GET /pool/products/{id}/variants,
/// POST /pool/products/{id}/import-to-stock.
/// </summary>
public static class DropshippingImportEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/dropshipping/import")
            .WithTags("Dropshipping Import")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        // POST /api/v1/dropshipping/import/trigger — platform sync tetikle (D12-22)
        group.MapPost("/trigger", async (
            ImportTriggerRequest request,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new TriggerSyncCommand(request.TenantId, request.PlatformCode), ct);
            return result.IsSuccess
                ? Results.Ok(new { jobId = result.JobId, platform = request.PlatformCode, syncType = request.SyncType })
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("TriggerDropshippingImport")
        .WithSummary("Platform ürün çekme tetikle — QuickDelta, PoolScan, FullReconciliation")
        .Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/dropshipping/import/issues — sync sorunları listesi (D12-22 / Sprint 0)
        group.MapGet("/issues", async (
            string? platformCode,
            MesTech.Infrastructure.Integration.Services.IPlatformSyncIssueService issueService,
            CancellationToken ct) =>
        {
            MesTech.Domain.Enums.PlatformType? platform = null;
            if (!string.IsNullOrEmpty(platformCode) &&
                Enum.TryParse<MesTech.Domain.Enums.PlatformType>(platformCode, true, out var parsed))
                platform = parsed;

            var issues = await issueService.GetOpenIssuesAsync(platform, ct);
            return Results.Ok(new { total = issues.Count, issues });
        })
        .WithName("GetSyncIssues")
        .WithSummary("Platform sync sorunları — barkod çakışması, kategori mismatch vb.")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // POST /api/v1/dropshipping/import/issues/{id}/resolve — sorun çöz (D12-22 / Sprint 0)
        group.MapPost("/issues/{id:guid}/resolve", async (
            Guid id,
            ResolveIssueRequest request,
            MesTech.Infrastructure.Integration.Services.IPlatformSyncIssueService issueService,
            CancellationToken ct) =>
        {
            var issues = await issueService.GetOpenIssuesAsync(ct: ct);
            var issue = issues.FirstOrDefault(i => i.Id == id);
            if (issue is null)
                return Results.Problem(detail: $"Issue {id} bulunamadı.", statusCode: 404);

            issue.IsResolved = true;
            issue.ResolvedAt = DateTime.UtcNow;

            return Results.Ok(new { issueId = id, resolved = true, resolution = request.Resolution });
        })
        .WithName("ResolveSyncIssue")
        .WithSummary("Sync sorununu çözüldü olarak işaretle")
        .Produces(200).Produces(404)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/dropshipping/import/status — tüm platform sync durumu (D12-22)
        group.MapGet("/status", (
            IIntegratorOrchestrator orchestrator) =>
        {
            var adapters = orchestrator.RegisteredAdapters;
            var statuses = adapters.Select(a => new
            {
                platform = a.PlatformCode,
                displayName = a.PlatformCode,
                isEnabled = true,
                timestamp = DateTime.UtcNow
            }).ToList();

            return Results.Ok(new { total = statuses.Count, platforms = statuses });
        })
        .WithName("GetDropshippingImportStatus")
        .WithSummary("Platform sync durumu — adapter kayıt ve son sync bilgisi")
        .Produces(200)
        .CacheOutput("Dashboard30s");

        // ═══ Pool Genişletilmiş Endpoint'ler (D12-23) ═══

        var poolGroup = app.MapGroup("/api/v1/dropshipping/pool")
            .WithTags("Dropshipping Pool")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/dropshipping/pool/products/{id}/reliability — 4 boyutlu güvenilirlik (D12-24)
        poolGroup.MapGet("/products/{id:guid}/reliability", (
            Guid id,
            IProductReliabilityCalculator calculator) =>
        {
            // Demo input — gerçek veri entegrasyonu DEV 1'in PoolProduct entity'si ile gelecek
            var result = calculator.Calculate(new ProductReliabilityInput(
                SupplierReliabilityScore: 85,
                ReturnRate: 2.1m,
                ComplaintRate: 0.8m,
                AverageRating: 4.3m,
                TotalReviews: 127,
                SalesLast30Days: 42,
                StockConsistencyRate: 91.5m,
                AverageDeliveryDays: 2.3m,
                DamageRate: 0.5m,
                OnTimeDeliveryRate: 94.2m));

            return Results.Ok(new { productId = id, reliability = result });
        })
        .WithName("GetPoolProductReliability")
        .WithSummary("Havuz ürün güvenilirlik skoru — 4 boyutlu (tedarikçi/kalite/satış/lojistik)")
        .Produces(200);

        // GET /api/v1/dropshipping/pool/products/{id}/media — ürün medya listesi (D12-23)
        poolGroup.MapGet("/products/{id:guid}/media", async (
            Guid id,
            MesTech.Infrastructure.Persistence.AppDbContext db,
            CancellationToken ct) =>
        {
            var media = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync(
                    System.Linq.Queryable.Where(
                        Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                            .AsNoTracking(db.ProductMedia),
                        m => m.ProductId == id),
                    ct);

            return Results.Ok(new
            {
                productId = id,
                total = media.Count,
                images = media.Where(m => m.Type == MesTech.Domain.Entities.MediaType.Image)
                    .OrderBy(m => m.SortOrder)
                    .Select(m => new { m.Id, m.Url, m.ThumbnailUrl, m.AltText, m.SortOrder }),
                videos = media.Where(m => m.Type == MesTech.Domain.Entities.MediaType.Video)
                    .Select(m => new { m.Id, m.Url, m.DurationSeconds }),
                certificates = media.Where(m => m.Type == MesTech.Domain.Entities.MediaType.Certificate)
                    .Select(m => new { m.Id, m.Url, m.AltText })
            });
        })
        .WithName("GetPoolProductMedia")
        .WithSummary("Havuz ürün medya listesi — görsel, video, sertifika (D12-23)")
        .Produces(200);

        // GET /api/v1/dropshipping/pool/products/{id}/variants — ürün varyantları (D12-23)
        poolGroup.MapGet("/products/{id:guid}/variants", async (
            Guid id,
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new MesTech.Application.Features.Product.Queries.GetProductVariants.GetProductVariantsQuery(tenantId, id), ct);
            return Results.Ok(new { productId = id, variants = result });
        })
        .WithName("GetPoolProductVariants")
        .WithSummary("Havuz ürün varyant matrisi — renk/beden/barkod/fiyat (D12-23)")
        .Produces(200);

        // POST /api/v1/dropshipping/pool/products/{id}/import-to-stock — havuzdan stoğa aktar (D12-23)
        poolGroup.MapPost("/products/{id:guid}/import-to-stock", async (
            Guid id,
            ImportToStockRequest request,
            MesTech.Infrastructure.Persistence.AppDbContext db,
            ISender mediator,
            CancellationToken ct) =>
        {
            var poolProduct = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(
                    Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                        .Include(db.Set<MesTech.Domain.Entities.DropshippingPoolProduct>(), p => p.Product),
                    p => p.Id == id, ct);

            if (poolProduct is null)
                return Results.Problem(detail: $"Havuz ürünü {id} bulunamadı.", statusCode: 404);

            if (poolProduct.Product is null)
                return Results.Problem(detail: "Havuz ürününe bağlı kaynak ürün bulunamadı.", statusCode: 400);

            var source = poolProduct.Product;
            // SKU benzersizlik: DS-{SKU}-{timestamp} (çakışma önleme)
            var uniqueSku = $"DS-{source.SKU}-{DateTime.UtcNow:yyMMddHHmm}";
            // PurchasePrice: request'ten marj oranı veya default %70
            var purchasePrice = poolProduct.PoolPrice * (request.MarginRate > 0 ? (1 - request.MarginRate) : 0.7m);

            var createResult = await mediator.Send(
                new MesTech.Application.Commands.CreateProduct.CreateProductCommand(
                    Name: source.Name,
                    SKU: uniqueSku,
                    Barcode: source.Barcode,
                    PurchasePrice: purchasePrice,
                    SalePrice: poolProduct.PoolPrice,
                    CategoryId: source.CategoryId,
                    Description: source.Description,
                    Brand: source.Brand,
                    ImageUrl: source.ImageUrl,
                    MinimumStock: request.InitialStock > 0 ? 5 : 0,
                    SyncToPlatforms: request.SyncToPlatforms), ct);

            if (!createResult.IsSuccess)
                return Results.Problem(detail: createResult.ErrorMessage, statusCode: 400);

            // Link pool product → new stock product (best-effort — CreateProduct already committed)
            try
            {
                await mediator.Send(
                    new MesTech.Application.Features.Dropshipping.Commands.LinkDropshipProduct.LinkDropshipProductCommand(
                        request.TenantId, poolProduct.ProductId, createResult.ProductId), ct);
            }
            catch (Exception)
            {
                // Link failed but product created — return partial success
                return Results.Ok(new { productId = createResult.ProductId, poolProductId = id, linked = false,
                    warning = "Ürün oluşturuldu ama link başarısız — manuel eşleştirme gerekli" });
            }

            return Results.Created($"/api/v1/products/{createResult.ProductId}",
                new { productId = createResult.ProductId, poolProductId = id, linked = true });
        })
        .WithName("ImportPoolProductToStock")
        .WithSummary("Havuz ürününü stoğa aktar — yeni ürün oluştur + link (D12-23)")
        .Produces(201).Produces(400).Produces(404)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }

    public record ImportTriggerRequest(
        Guid TenantId,
        string PlatformCode,
        string SyncType = "QuickDelta");

    public record ImportToStockRequest(
        Guid TenantId,
        int InitialStock = 0,
        bool SyncToPlatforms = false,
        decimal MarginRate = 0);

    public record ResolveIssueRequest(string Resolution, string? NewBarcode = null);
}
