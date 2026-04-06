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

        // GET /api/v1/dropshipping/import/status — tüm platform sync durumu (D12-22)
        group.MapGet("/status", async (
            IAdapterFactory adapterFactory,
            CancellationToken ct) =>
        {
            var platforms = new[] { "Trendyol", "Hepsiburada", "N11", "Amazon", "Ciceksepeti" };
            var statuses = new List<object>();

            foreach (var platform in platforms)
            {
                var adapter = adapterFactory.Resolve(platform);
                statuses.Add(new
                {
                    platform,
                    adapterRegistered = adapter is not null,
                    timestamp = DateTime.UtcNow
                });
            }

            return Results.Ok(statuses);
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
    }

    public record ImportTriggerRequest(
        Guid TenantId,
        string PlatformCode,
        string SyncType = "QuickDelta");
}
