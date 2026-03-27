using System.Text.Json;
using System.Threading.RateLimiting;
using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.System.Queries.GetAuditLogs;
using MesTech.Application.Features.System.Queries.GetBackupHistory;

namespace MesTech.WebApi.Endpoints;

public static class SystemEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/system")
            .WithTags("System")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/system/audit-logs — erişim/denetim logları
        group.MapGet("/audit-logs", async (
            Guid tenantId,
            DateTime? from = null, DateTime? to = null,
            string? userId = null, string? action = null,
            int page = 1, int pageSize = 50,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetAuditLogsQuery(tenantId, from, to, userId, action, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetAuditLogs")
        .WithSummary("Erişim ve denetim logları (tarih/kullanıcı/aksiyon filtresi)").Produces(200).Produces(400);

        // GET /api/v1/system/backups — yedek geçmişi
        group.MapGet("/backups", async (
            Guid tenantId, int limit = 20,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(new GetBackupHistoryQuery(tenantId, limit), ct);
            return Results.Ok(result);
        })
        .WithName("GetBackupHistory")
        .WithSummary("Yedekleme geçmişi (son N kayıt)").Produces(200).Produces(400);

        // GET /api/v1/system/rate-limit-status — API kota durumu
        group.MapGet("/rate-limit-status", (HttpContext httpContext) =>
        {
            var rateLimitFeature = httpContext.Features
                .Get<RateLimitLease>();

            return Results.Ok(new
            {
                Limit = 100,
                WindowSeconds = 60,
                Policy = "PerApiKey",
                Description = "100 request per minute per API key"
            });
        })
        .WithName("GetRateLimitStatus")
        .WithSummary("API rate limit kota bilgisi").Produces(200).Produces(400);

        // ── N8N / Automation Webhook Endpoints (G130) ──

        // POST /api/v1/system/automation/webhook — N8N workflow trigger receiver
        group.MapPost("/automation/webhook", async (
            HttpContext httpContext,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Automation");

            using var reader = new StreamReader(httpContext.Request.Body);
            var body = await reader.ReadToEndAsync(ct);

            var source = httpContext.Request.Headers["X-Automation-Source"].FirstOrDefault() ?? "n8n";
            var eventType = httpContext.Request.Headers["X-Automation-Event"].FirstOrDefault() ?? "unknown";
            var secret = httpContext.Request.Headers["X-Automation-Secret"].FirstOrDefault();

            // Simple shared secret validation
            var expectedSecret = httpContext.RequestServices
                .GetRequiredService<IConfiguration>()["Automation:WebhookSecret"];

            if (!string.IsNullOrWhiteSpace(expectedSecret) && secret != expectedSecret)
            {
                logger.LogWarning("Automation webhook rejected: invalid secret from {Source}", source);
                return Results.Json(
                    ApiResponse<object>.Fail("Invalid automation secret", "AUTH_FAILED"),
                    statusCode: 401);
            }

            logger.LogInformation(
                "Automation webhook received: Source={Source} Event={Event} BodyLength={Length}",
                source, eventType, body.Length);

            return Results.Ok(ApiResponse<StatusResponse>.Ok(
                new StatusResponse("accepted", $"Event '{eventType}' from '{source}' processed")));
        })
        .WithName("AutomationWebhook")
        .WithSummary("N8N/automation workflow webhook receiver (G130)")
        .AllowAnonymous();

        // GET /api/v1/system/automation/status — N8N entegrasyon durumu
        group.MapGet("/automation/status", (IConfiguration configuration) =>
        {
            var n8nUrl = configuration["Automation:N8NBaseUrl"];
            var webhookSecret = configuration["Automation:WebhookSecret"];

            return Results.Ok(ApiResponse<object>.Ok(new
            {
                n8nConfigured = !string.IsNullOrWhiteSpace(n8nUrl),
                n8nBaseUrl = n8nUrl ?? "not configured",
                webhookSecretConfigured = !string.IsNullOrWhiteSpace(webhookSecret),
                supportedWorkflows = new[]
                {
                    "WF-01: siparis→fatura→PDF→email→WA",
                    "WF-02: service-down→TG+WA alarm",
                    "WF-03: dusuk-stok→TG+reorder",
                    "WF-04: gunluk-ozet 20:00",
                    "WF-05: musteri→CRM+Chatwoot",
                    "WF-06: fatura→UBL dogrulama",
                    "WF-07: service-recovery→bildirim",
                    "WF-08: haftalik-guvenlik",
                    "WF-09: backup→MinIO 03:00",
                    "WF-10: sefer→muhasebe"
                }
            }));
        })
        .WithName("GetAutomationStatus")
        .WithSummary("N8N otomasyon entegrasyon durumu ve desteklenen workflow listesi").Produces(200).Produces(400);
    }
}
