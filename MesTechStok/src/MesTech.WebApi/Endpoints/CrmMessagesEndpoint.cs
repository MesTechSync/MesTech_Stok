// DEV1-DEPENDENCY: Application CRM Messages CQRS handlers not yet created.
// When DEV 1 creates GetPlatformMessagesQuery / ReplyToMessageCommand, restore ISender dispatch.

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// CRM Messages endpoints — platform message inbox and reply.
/// GET  /api/v1/crm/messages — list platform messages (Trendyol, HB, N11 etc.)
/// POST /api/v1/crm/messages/{id}/reply — reply to a platform message
/// </summary>
public static class CrmMessagesEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/crm")
            .WithTags("CRM")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/crm/messages — platform message inbox
        group.MapGet("/messages", (
            Guid tenantId,
            string? platform,
            string? status,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default) =>
        {
            // DEV1-DEPENDENCY: GetPlatformMessagesQuery not yet available
            // Final: var result = await mediator.Send(
            //   new GetPlatformMessagesQuery(tenantId, platform, status, page, pageSize), ct);
            return Results.Ok(new
            {
                TenantId = tenantId,
                Platform = platform,
                Status = status,
                Page = page,
                PageSize = pageSize,
                Items = Array.Empty<object>(),
                TotalCount = 0,
                Message = "CRM Messages endpoint stub — DEV1 Application handler pending"
            });
        })
        .WithName("GetPlatformMessages")
        .WithSummary("Platform mesaj kutusu — Trendyol, HB, N11 vb. (EMR-09)");

        // POST /api/v1/crm/messages/{id}/reply — reply to a platform message
        group.MapPost("/messages/{id:guid}/reply", (
            Guid id,
            CrmReplyRequest request,
            CancellationToken ct) =>
        {
            // DEV1-DEPENDENCY: ReplyToMessageCommand not yet available
            // Final: await mediator.Send(new ReplyToMessageCommand(id, request.Reply), ct);
            return Results.Accepted($"/api/v1/crm/messages/{id}", new
            {
                MessageId = id,
                Reply = request.Reply,
                Message = "CRM ReplyToMessage endpoint stub — DEV1 Application handler pending"
            });
        })
        .WithName("ReplyToMessage")
        .WithSummary("Platform mesajına yanıt gönder (EMR-09)");
    }

    /// <summary>Request DTO for message reply.</summary>
    public record CrmReplyRequest(string Reply);
}
