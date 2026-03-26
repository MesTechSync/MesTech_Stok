using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.EInvoice.Commands;
using MesTech.Application.Features.EInvoice.Queries;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class EInvoiceEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/e-invoices")
            .WithTags("E-Invoices")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/e-invoices — e-fatura listesi
        group.MapGet("/", async (
            DateTime? from, DateTime? to,
            EInvoiceStatus? status, string? providerId,
            int page = 1, int pageSize = 50,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetEInvoicesQuery(from, to, status, providerId, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetEInvoices")
        .WithSummary("E-fatura listesi (tarih, durum, sağlayıcı filtresi)")
        .CacheOutput("Report120s");

        // POST /api/v1/e-invoices — yeni e-fatura oluştur
        group.MapPost("/", async (
            CreateEInvoiceCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/e-invoices/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateEInvoice")
        .WithSummary("Yeni e-fatura oluştur (UBL-TR 1.2)")
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/e-invoices/{id} — e-fatura detayı
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetEInvoiceByIdQuery(id), ct);
            return result is not null
                ? Results.Ok(result)
                : Results.NotFound(new { Message = $"E-Fatura {id} bulunamadı" });
        })
        .WithName("GetEInvoiceById")
        .WithSummary("E-fatura detayı")
        .CacheOutput("Lookup60s");

        // POST /api/v1/e-invoices/{id}/send — e-fatura gönder
        group.MapPost("/{id:guid}/send", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var success = await mediator.Send(
                new SendEInvoiceCommand(id), ct);
            return success
                ? Results.Ok(new { EInvoiceId = id, Message = "E-Fatura başarıyla gönderildi" })
                : Results.BadRequest(new { EInvoiceId = id, Message = "E-Fatura gönderilemedi" });
        })
        .WithName("SendEInvoice")
        .WithSummary("E-faturayı GİB'e gönder")
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/e-invoices/{id}/cancel — e-fatura iptal
        group.MapPost("/{id:guid}/cancel", async (
            Guid id, string reason,
            ISender mediator, CancellationToken ct) =>
        {
            var success = await mediator.Send(
                new CancelEInvoiceCommand(id, reason), ct);
            return success
                ? Results.Ok(new { EInvoiceId = id, Message = "E-Fatura iptal edildi" })
                : Results.BadRequest(new { EInvoiceId = id, Message = "E-Fatura iptal edilemedi" });
        })
        .WithName("CancelEInvoice")
        .WithSummary("E-fatura iptal et");

        // GET /api/v1/e-invoices/check-vkn/{vkn} — VKN mükellef sorgusu
        group.MapGet("/check-vkn/{vkn}", async (
            string vkn,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new CheckVknMukellefQuery(vkn), ct);
            return Results.Ok(result);
        })
        .WithName("CheckVknMukellef")
        .WithSummary("VKN ile e-fatura mükellefi sorgula")
        .CacheOutput("Lookup60s");
    }
}
