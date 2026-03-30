using System.Text.Json;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

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

            // OWASP A08: Validate JSON structure before processing (prevent malformed payload injection)
            try
            {
                using var jsonDoc = JsonDocument.Parse(body);
                // Ensure root is object or array — reject primitives and malformed JSON
                if (jsonDoc.RootElement.ValueKind != JsonValueKind.Object &&
                    jsonDoc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    return Results.BadRequest(ApiResponse<object>.Fail(
                        "Webhook body must be a JSON object or array", "INVALID_JSON_STRUCTURE"));
                }
            }
            catch (JsonException)
            {
                return Results.BadRequest(ApiResponse<object>.Fail(
                    "Webhook body is not valid JSON", "INVALID_JSON"));
            }

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
        .WithSummary("Platform webhook alıcı — DLQ destekli (Trendyol, HB, N11, vb.)")
        .WithDescription("Receives webhook notifications from marketplace platforms — failed webhooks saved to DLQ")
        .Produces(200)
        .Produces(400)
        .Produces(422)
        .AllowAnonymous() // Webhook'lar platform'dan JWT olmadan gelir
        .WithMetadata(new RequestSizeLimitAttribute(1_048_576)); // G088 FIX: 1MB limit (tipik webhook 1-100KB)

        // GET /api/webhooks/dead-letters — DLQ list (admin)
        group.MapGet("/dead-letters", async (
            WebhookDeadLetterStatus? status,
            int page,
            int pageSize,
            IWebhookDeadLetterRepository dlqRepo,
            CancellationToken ct) =>
        {
            var (items, total) = await dlqRepo.GetPagedAsync(status, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), ct);
            return Results.Ok(ApiResponse<PagedResponse<WebhookDeadLetter>>.Ok(
                new PagedResponse<WebhookDeadLetter>(items, total)));
        })
        .WithName("GetWebhookDeadLetters")
        .WithSummary("Webhook dead letter queue — başarısız webhook listesi (admin)")
        .Produces(200).Produces(400)
        .RequireRateLimiting("PerApiKey");

        // ── Webhook Test Console (G108 — DEV6-TUR10) ──
        // Sandbox only — production'da çalışmaz

        // GET /api/webhooks/test/platforms — test edilebilir platformlar + event tipleri
        group.MapGet("/test/platforms", (IWebhookProcessor processor) =>
        {
            var platforms = SignatureHeaders.Keys.Select(p => new
            {
                platform = p,
                signatureHeader = SignatureHeaders[p],
                sampleEvents = GetSampleEventTypes(p)
            }).ToList();

            return Results.Ok(ApiResponse<object>.Ok(platforms));
        })
        .WithName("GetWebhookTestPlatforms")
        .WithSummary("Webhook test konsolu — platform listesi ve event tipleri")
        .Produces(200)
        .RequireRateLimiting("PerApiKey");

        // GET /api/webhooks/test/sample/{platform}/{eventType} — örnek payload
        group.MapGet("/test/sample/{platform}/{eventType}", (string platform, string eventType) =>
        {
            var payload = GenerateSamplePayload(platform.ToLowerInvariant(), eventType.ToLowerInvariant());
            return payload is not null
                ? Results.Ok(ApiResponse<object>.Ok(new { platform, eventType, payload }))
                : Results.NotFound(ApiResponse<object>.Fail(
                    $"Sample payload not found for {platform}/{eventType}", "NOT_FOUND"));
        })
        .WithName("GetWebhookSamplePayload")
        .WithSummary("Platform + event tipi için örnek webhook payload")
        .Produces(200).Produces(404)
        .RequireRateLimiting("PerApiKey");

        // POST /api/webhooks/test — test webhook gönder ve sonucu göster
        group.MapPost("/test", async (
            WebhookTestRequest request,
            IWebhookProcessor processor,
            ILoggerFactory loggerFactory,
            IWebHostEnvironment env,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.WebhookEndpoints");

            // Production guard
            if (env.IsProduction())
                return Results.Json(
                    ApiResponse<object>.Fail("Webhook test konsolu production ortamında devre dışıdır.", "PRODUCTION_BLOCKED"),
                    statusCode: 403);

            if (string.IsNullOrWhiteSpace(request.Platform) || string.IsNullOrWhiteSpace(request.Payload))
                return Results.BadRequest(ApiResponse<object>.Fail("Platform and Payload are required.", "VALIDATION"));

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await processor.ProcessAsync(
                request.Platform.ToLowerInvariant(),
                request.Payload,
                request.Signature,
                ct);
            sw.Stop();

            logger.LogInformation(
                "Webhook test: Platform={Platform} Event={Event} Success={Success} Duration={Ms}ms",
                request.Platform, result.EventType, result.Success, sw.ElapsedMilliseconds);

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                platform = request.Platform,
                eventType = result.EventType ?? "unknown",
                success = result.Success,
                error = result.Error,
                durationMs = sw.ElapsedMilliseconds,
                timestamp = DateTime.UtcNow
            }));
        })
        .WithName("TestWebhook")
        .WithSummary("Webhook test konsolu — payload gönder, sonucu gör (sandbox only)")
        .Produces(200).Produces(400).Produces(403)
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
        .Produces(200).Produces(404)
        .RequireRateLimiting("PerApiKey");
    }

    // ── Request Records ──

    internal record WebhookTestRequest(string Platform, string Payload, string? Signature = null);

    // ── Helper Methods ──

    private static string[] GetSampleEventTypes(string platform) => platform.ToLowerInvariant() switch
    {
        "trendyol" => new[] { "order.created", "order.shipped", "order.cancelled", "product.updated" },
        "hepsiburada" => new[] { "order.new", "order.shipped", "return.created" },
        "amazon" => new[] { "ORDER_CHANGE", "LISTINGS_ITEM_MFN_QUANTITY_CHANGE", "RETURN_CREATED" },
        "shopify" => new[] { "orders/create", "orders/fulfilled", "products/update", "refunds/create" },
        "woocommerce" => new[] { "order.created", "order.updated", "product.updated" },
        "ebay" => new[] { "MARKETPLACE_ACCOUNT_DELETION", "ITEM_SOLD" },
        "n11" => new[] { "order.new", "order.shipped" },
        "ciceksepeti" => new[] { "order.created", "order.shipped" },
        "ozon" => new[] { "ORDER_NEW", "ORDER_DELIVERED" },
        _ => new[] { "order.created", "order.updated" }
    };

    private static string? GenerateSamplePayload(string platform, string eventType)
    {
        var orderId = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        var timestamp = DateTime.UtcNow.ToString("o");

        return platform switch
        {
            "trendyol" when eventType.Contains("order") => JsonSerializer.Serialize(new
            {
                orderNumber = $"TY-{orderId}",
                status = "Created",
                lines = new[] { new { sku = "SAMPLE-SKU-001", quantity = 1, amount = 149.90m } },
                customerFirstName = "Test",
                customerLastName = "User",
                orderDate = timestamp
            }),
            "shopify" when eventType.Contains("order") => JsonSerializer.Serialize(new
            {
                id = Random.Shared.NextInt64(1_000_000, 9_999_999),
                order_number = $"#SH-{orderId}",
                financial_status = "paid",
                line_items = new[] { new { sku = "SAMPLE-SKU-001", quantity = 1, price = "149.90" } },
                created_at = timestamp
            }),
            "amazon" when eventType.Contains("ORDER") => JsonSerializer.Serialize(new
            {
                NotificationType = eventType,
                Payload = new
                {
                    AmazonOrderId = $"111-{orderId}-{Random.Shared.Next(1000, 9999)}",
                    OrderStatus = "Unshipped",
                    PurchaseDate = timestamp
                }
            }),
            "hepsiburada" when eventType.Contains("order") => JsonSerializer.Serialize(new
            {
                orderId = $"HB-{orderId}",
                status = "New",
                items = new[] { new { merchantSku = "SAMPLE-SKU-001", quantity = 1, unitPrice = 149.90m } },
                createdDate = timestamp
            }),
            _ => JsonSerializer.Serialize(new
            {
                platform,
                eventType,
                orderId = $"TEST-{orderId}",
                timestamp,
                testData = true
            })
        };
    }
}
