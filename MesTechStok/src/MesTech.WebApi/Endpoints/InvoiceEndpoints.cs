using MediatR;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Application.Features.Invoice.Queries;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.WebApi.Endpoints;

internal record InvoiceEndpointRequest(
    int Provider,
    InvoiceCreateRequest Invoice);

public static class InvoiceEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/invoices").WithTags("Invoices").RequireRateLimiting("PerApiKey");

        // POST /api/v1/invoices — create an invoice via the resolved provider adapter
        group.MapPost("/", async (
            InvoiceEndpointRequest request,
            IInvoiceAdapterFactory factory,
            CancellationToken ct) =>
        {
            try
            {
                var providerType = (InvoiceProvider)request.Provider;
                var adapter = factory.Resolve(providerType);

                if (adapter is null)
                    return Results.BadRequest("Unknown provider");

                var result = await adapter.CreateInvoiceAsync(request.Invoice, ct);
                return Results.Ok(result);
            }
            catch (Exception)
            {
                return Results.Problem("Fatura olusturma basarisiz — lutfen tekrar deneyin veya destek ile iletisime gecin.");
            }
        })
        .WithName("CreateInvoiceViaAdapter")
        .WithSummary("Fatura oluştur (adapter üzerinden)");

        // GET /api/v1/invoices — fatura listesi
        group.MapGet("/", async (
            InvoiceType? type, InvoiceStatus? status, PlatformType? platform,
            DateTime? from, DateTime? to, string? search,
            int page = 1, int pageSize = 50,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetInvoicesQuery(type, status, platform, from, to, search, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetInvoices")
        .WithSummary("Fatura listesi (tip, durum, platform, tarih filtresi)");

        // POST /api/v1/invoices/{id}/approve — fatura onayla
        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var success = await mediator.Send(new ApproveInvoiceCommand(id), ct);
            return success
                ? Results.Ok(new { InvoiceId = id, Approved = true })
                : Results.BadRequest(new { InvoiceId = id, Message = "Fatura onaylanamadı" });
        })
        .WithName("ApproveInvoice")
        .WithSummary("Fatura onayla");

        // POST /api/v1/invoices/bulk — toplu fatura oluştur
        group.MapPost("/bulk", async (
            BulkCreateInvoiceCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("BulkCreateInvoice")
        .WithSummary("Toplu fatura oluştur (sipariş ID listesi)");

        // GET /api/v1/invoices/providers — fatura sağlayıcı durumları
        group.MapGet("/providers", async (
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInvoiceProvidersQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetInvoiceProviders")
        .WithSummary("Fatura sağlayıcı listesi ve durumları");

        // GET /api/v1/invoices/report — fatura raporu
        group.MapGet("/report", async (
            DateTime from, DateTime to, PlatformType? platform,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetInvoiceReportQuery(from, to, platform), ct);
            return Results.Ok(result);
        })
        .WithName("GetInvoiceReport")
        .WithSummary("Fatura raporu (tarih aralığı + platform filtresi)");
    }
}
