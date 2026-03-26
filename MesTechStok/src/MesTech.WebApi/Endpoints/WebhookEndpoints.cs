using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Webhook receiver endpoint'leri — marketplace + payment webhook'ları.
/// Başarısız webhook'lar WebhookDeadLetter'a kaydedilir (DEV6-TUR14).
/// </summary>
public static class WebhookEndpoints
{
    private static readonly Dictionary<string, string> SignatureHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["trendyol"] = "X-Trendyol-Signature",
        ["shopify"] = "X-Shopify-Hmac-Sha256",
        ["woocommerce"] = "X-WC-Webhook-Signature",
        ["hepsiburada"] = "X-HB-Signature",
        ["amazon"] = "X-Amz-Sns-Message-Id",
        ["ciceksepeti"] = "X-CS-Signature",
        ["ebay"] = "X-eBay-Signature",
        ["n11"] = "X-N11-Signature",
        ["ozon"] = "X-Ozon-Signature",
        ["pttavm"] = "X-Signature",
        ["pazarama"] = "X-Pazarama-Signature",
        ["etsy"] = "X-Etsy-Signature",
        ["zalando"] = "X-Zalando-Signature",
        ["bitrix24"] = "X-Bitrix24-Token"
    };

    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/webhooks").WithTags("Webhooks");

        // POST /api/webhooks/{platform} — generic webhook receiver with DLQ
        group.MapPost("/{platform}", async (
            string platform,
            HttpContext httpContext,
            IWebhookProcessor processor,
            IWebhookDeadLetterRepository dlqRepo,
            IUnitOfWork uow,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.WebhookEndpoints");

            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync(ct);

            if (string.IsNullOrWhiteSpace(body))
                return Results.BadRequest(ApiResponse<object>.Fail("Empty webhook body", "EMPTY_BODY"));

            string? signature = null;
            var normalizedPlatform = platform.ToLowerInvariant();

            if (SignatureHeaders.TryGetValue(normalizedPlatform, out var headerName))
                signature = httpContext.Request.Headers[headerName].FirstOrDefault();
            signature ??= httpContext.Request.Headers["X-Webhook-Signature"].FirstOrDefault();

            var result = await processor.ProcessAsync(normalizedPlatform, body, signature, ct);

            if (result.Success)
            {
                return Results.Ok(ApiResponse<StatusResponse>.Ok(
                    new StatusResponse("accepted", result.EventType)));
            }

            // FAILED → Dead Letter Queue'ya kaydet (DEV6-TUR14)
            logger.LogWarning(
                "Webhook failed — saving to DLQ. Platform={Platform}, Error={Error}",
                normalizedPlatform, result.Error);

            var deadLetter = WebhookDeadLetter.Create(
                normalizedPlatform,
                result.EventType ?? "unknown",
                body,
                signature,
                result.Error ?? "Unknown processing error");

            await dlqRepo.AddAsync(deadLetter, ct);
            await uow.SaveChangesAsync(ct);

            return Results.UnprocessableEntity(ApiResponse<StatusResponse>.Fail(
                result.Error ?? "Webhook processing failed", "WEBHOOK_FAILED"));
        })
        .WithName("ReceiveWebhook")
        .WithDescription("Receives webhook notifications from marketplace platforms — failed webhooks saved to DLQ")
        .Produces(200)
        .Produces(400)
        .Produces(422);

        // GET /api/webhooks/dead-letters — DLQ list (admin)
        group.MapGet("/dead-letters", async (
            WebhookDeadLetterStatus? status,
            int page,
            int pageSize,
            IWebhookDeadLetterRepository dlqRepo,
            CancellationToken ct) =>
        {
            var (items, total) = await dlqRepo.GetPagedAsync(status, page, pageSize, ct);
            return Results.Ok(ApiResponse<PagedResponse<WebhookDeadLetter>>.Ok(
                new PagedResponse<WebhookDeadLetter>(items, total)));
        })
        .WithName("GetWebhookDeadLetters")
        .WithSummary("Webhook dead letter queue — başarısız webhook listesi (admin)")
        .RequireRateLimiting("PerApiKey");

        // POST /api/webhooks/dead-letters/{id}/resolve — manual resolve
        group.MapPost("/dead-letters/{id:guid}/resolve", async (
            Guid id,
            string resolvedBy,
            IWebhookDeadLetterRepository dlqRepo,
            IUnitOfWork uow,
            CancellationToken ct) =>
        {
            var item = await dlqRepo.GetByIdAsync(id, ct);
            if (item is null) return Results.NotFound();

            item.MarkManuallyResolved(resolvedBy);
            await uow.SaveChangesAsync(ct);
            return Results.Ok(ApiResponse<StatusResponse>.Ok(new StatusResponse("resolved")));
        })
        .WithName("ResolveWebhookDeadLetter")
        .WithSummary("Dead letter webhook'u manuel çözüldü olarak işaretle")
        .RequireRateLimiting("PerApiKey");
    }
}
