using MediatR;
using MesTech.Application.Features.Crm.Commands.ReplyToMessage;
using MesTech.Application.Features.Crm.Queries.GetPlatformMessages;
using MesTech.Domain.Enums;

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
        group.MapGet("/messages", async (
            ISender mediator,
            Guid tenantId,
            PlatformType? platform = null,
            MessageStatus? status = null,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetPlatformMessagesQuery(tenantId, platform, status, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetPlatformMessages")
        .WithSummary("Platform mesaj kutusu — Trendyol, HB, N11 vb. (EMR-09)");

        // POST /api/v1/crm/messages/{id}/reply — reply to a platform message
        group.MapPost("/messages/{id:guid}/reply", async (
            ISender mediator,
            Guid id,
            CrmReplyRequest request,
            CancellationToken ct = default) =>
        {
            await mediator.Send(
                new ReplyToMessageCommand(id, request.Reply, request.RepliedBy), ct);
            return Results.NoContent();
        })
        .WithName("ReplyToMessage")
        .WithSummary("Platform mesajına yanıt gönder (EMR-09)");
    }

    /// <summary>Request DTO for message reply.</summary>
    public record CrmReplyRequest(string Reply, string RepliedBy);
}
