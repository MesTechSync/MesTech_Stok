using MediatR;
using MesTech.Application.Commands.SendInvoice;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Application.Features.Invoice.Commands.ExportInvoiceReport;
using MesTech.Application.Features.Invoice.Commands.ExportInvoices;
using MesTech.Application.Features.Invoice.Queries;
using MesTech.Application.Features.Invoice.Queries.GetInvoiceSettings;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

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
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("MesTech.WebApi.Endpoints.InvoiceEndpoints");
            try
            {
                var providerType = (InvoiceProvider)request.Provider;
                var adapter = factory.Resolve(providerType);

                if (adapter is null)
                    return Results.Problem(detail: "Unknown provider", statusCode: 400);

                var result = await adapter.CreateInvoiceAsync(request.Invoice, ct);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Invoice creation failed via adapter");
                return Results.Problem(
                    detail: "Fatura olusturma basarisiz — lutfen tekrar deneyin veya destek ile iletisime gecin.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("CreateInvoiceViaAdapter")
        .WithSummary("Fatura oluştur (adapter üzerinden)")
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/invoices — fatura listesi
        group.MapGet("/", async (
            InvoiceType? type, InvoiceStatus? status, PlatformType? platform,
            DateTime? from, DateTime? to, string? search,
            int page = 1, int pageSize = 50,
            ISender mediator = default!, CancellationToken ct = default) =>
        {
            var safeSearch = search is { Length: > 500 } ? search[..500] : search;
            var result = await mediator.Send(
                new GetInvoicesQuery(type, status, platform, from, to, safeSearch, Math.Max(1, page), Math.Clamp(pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetInvoices")
        .WithSummary("Fatura listesi (tip, durum, platform, tarih filtresi)")
        .Produces(200)
        .CacheOutput("Report120s");

        // POST /api/v1/invoices/{id}/approve — fatura onayla
        group.MapPost("/{id:guid}/approve", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var success = await mediator.Send(new ApproveInvoiceCommand(id), ct);
            return success
                ? Results.Ok(new EntityActionResponse(id, "Approved"))
                : Results.Problem(detail: "Fatura onaylanamadı", statusCode: 400);
        })
        .WithName("ApproveInvoice")
        .WithSummary("Fatura onayla").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/invoices/bulk — toplu fatura oluştur
        group.MapPost("/bulk", async (
            BulkCreateInvoiceCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return Results.Ok(result);
        })
        .WithName("BulkCreateInvoice")
        .WithSummary("Toplu fatura oluştur (sipariş ID listesi)").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/invoices/providers — fatura sağlayıcı durumları
        group.MapGet("/providers", async (
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInvoiceProvidersQuery(), ct);
            return Results.Ok(result);
        })
        .WithName("GetInvoiceProviders")
        .WithSummary("Fatura sağlayıcı listesi ve durumları")
        .Produces(200)
        .CacheOutput("Lookup60s");

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
        .WithSummary("Fatura raporu (tarih aralığı + platform filtresi)")
        .Produces(200)
        .CacheOutput("Report120s");

        // GET /api/v1/invoices/settings — invoice settings (provider config, defaults)
        group.MapGet("/settings", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInvoiceSettingsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetInvoiceSettings")
        .WithSummary("Fatura ayarları — sağlayıcı konfigürasyonu ve varsayılanlar")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/invoices/{id}/send — send invoice to provider (e-Fatura/e-Arşiv)
        group.MapPost("/{id:guid}/send", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SendInvoiceCommand(id), ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("SendInvoice")
        .WithSummary("Faturayı e-Fatura/e-Arşiv sağlayıcısına gönder")
        .Produces(200)
        .Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/invoices/export — fatura listesi dışa aktarım (G564)
        group.MapPost("/export", async (
            ExportInvoicesCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            if (result.FileData.Length == 0)
                return Results.Problem(detail: "Export produced no data", statusCode: 400);
            return Results.File(result.FileData.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.FileName);
        })
        .WithName("ExportInvoices")
        .WithSummary("Fatura listesini Excel'e aktar")
        .Produces(200).Produces(400);

        // POST /api/v1/invoices/report/export — fatura raporu dışa aktarım (G564)
        group.MapPost("/report/export", async (
            ExportInvoiceReportCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            if (result.FileData.Length == 0)
                return Results.Problem(detail: "Report export produced no data", statusCode: 400);
            var contentType = command.Format.ToLowerInvariant() == "pdf"
                ? "application/pdf"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            return Results.File(result.FileData.ToArray(), contentType, result.FileName);
        })
        .WithName("ExportInvoiceReport")
        .WithSummary("Fatura raporu dışa aktar — Excel veya PDF")
        .Produces(200).Produces(400);
    }
}
