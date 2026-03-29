using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.Messaging.Endpoints;

/// <summary>
/// DLQ izleme ve reprocess minimal API endpoints.
/// Admin auth (X-Admin-Key header) gerektirir.
/// </summary>
public static class DlqEndpoints
{
    public static IEndpointRouteBuilder MapDlqEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/internal/dlq")
            .WithTags("DLQ Management")
            .AddEndpointFilter(async (ctx, next) =>
            {
                var config = ctx.HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
                var expectedKey = config["Admin:InternalApiKey"] ?? "";
                var providedKey = ctx.HttpContext.Request.Headers["X-Admin-Key"].FirstOrDefault() ?? "";
                if (string.IsNullOrEmpty(expectedKey) || !string.Equals(expectedKey, providedKey, StringComparison.Ordinal))
                    return Results.Unauthorized();
                return await next(ctx);
            });

        // GET /api/internal/dlq/status
        group.MapGet("/status", async (DlqReprocessService service, CancellationToken ct) =>
        {
            var status = await service.GetDlqStatusAsync(ct).ConfigureAwait(false);
            return Results.Ok(new
            {
                queues = status,
                total_error_messages = status.Sum(s => s.MessageCount),
                checked_at = DateTimeOffset.UtcNow
            });
        });

        // POST /api/internal/dlq/reprocess/{queueName}
        group.MapPost("/reprocess/{queueName}", async (
            string queueName,
            DlqReprocessService service,
            CancellationToken ct) =>
        {
            var result = await service.ReprocessAsync(queueName, maxMessages: 10, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        return app;
    }
}
