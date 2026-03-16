using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Webhook receiver endpoint'leri.
/// Tum platformlardan gelen webhook bildirimlerini alir ve isler.
/// POST /api/webhooks/{platform} — generic webhook receiver.
/// </summary>
public static class WebhookEndpoints
{
    /// <summary>
    /// Platform-specific webhook imza header isimleri.
    /// </summary>
    private static readonly Dictionary<string, string> SignatureHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        ["trendyol"] = "X-Trendyol-Signature",
        ["shopify"] = "X-Shopify-Hmac-Sha256",
        ["woocommerce"] = "X-WC-Webhook-Signature",
        ["hepsiburada"] = "X-HB-Signature"
    };

    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/webhooks").WithTags("Webhooks");

        // POST /api/webhooks/{platform} — generic webhook receiver
        group.MapPost("/{platform}", async (
            string platform,
            HttpContext httpContext,
            IWebhookProcessor processor,
            CancellationToken ct) =>
        {
            // Read raw body
            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync(ct);

            if (string.IsNullOrWhiteSpace(body))
                return Results.BadRequest(new { error = "Empty webhook body" });

            // Extract signature from platform-specific header
            string? signature = null;
            var normalizedPlatform = platform.ToLowerInvariant();

            if (SignatureHeaders.TryGetValue(normalizedPlatform, out var headerName))
            {
                signature = httpContext.Request.Headers[headerName].FirstOrDefault();
            }

            // Fallback: generic signature header
            signature ??= httpContext.Request.Headers["X-Webhook-Signature"].FirstOrDefault();

            // Process webhook
            var result = await processor.ProcessAsync(normalizedPlatform, body, signature, ct);

            if (result.Success)
            {
                return Results.Ok(new
                {
                    status = "accepted",
                    eventType = result.EventType
                });
            }

            return Results.UnprocessableEntity(new
            {
                status = "rejected",
                error = result.Error
            });
        })
        .WithName("ReceiveWebhook")
        .WithDescription("Receives webhook notifications from marketplace platforms")
        .Produces(200)
        .Produces(400)
        .Produces(422);
    }
}
