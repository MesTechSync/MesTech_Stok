using MediatR;
using MesTech.Application.Commands.GenerateEFatura;
using MesTech.WebApi.Filters;
using MesTech.Application.Commands.SendInvoice;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Features.Invoice.DTOs;
using MesTech.Domain.Common;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Application.Features.Invoice.Commands.ExportInvoiceReport;
using MesTech.Application.Features.Invoice.Commands.ExportInvoices;
using MesTech.Application.Features.Invoice.Queries;
using MesTech.Application.Features.Invoice.Queries.GetInvoiceSettings;
using MesTech.Application.Interfaces;
using MesTech.Application.Queries.GetInvoiceById;
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
        .AddEndpointFilter<Filters.IdempotencyFilter>()
        .RequirePermission("ManageInvoices");

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
        .Produces<PagedResult<InvoiceListDto>>(200)
        .CacheOutput("Report120s");

        // GET /api/v1/invoices/{id} — fatura detayı (GAP-1 FIX: handler mevcut, endpoint eksikti)
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetInvoiceByIdQuery(id), ct);
            return result is not null
                ? Results.Ok(result)
                : Results.Problem(detail: $"Fatura {id} bulunamadı.", statusCode: 404);
        })
        .WithName("GetInvoiceById")
        .WithSummary("Fatura detayı — kalemler, KDV, toplam, durum bilgisi")
        .Produces<MesTech.Application.DTOs.InvoiceDto>(200).Produces(404)
        .CacheOutput("Lookup60s");

        // GET /api/v1/invoices/{id}/pdf — fatura PDF indir (GAP-2 FIX: IInvoicePdfGenerator mevcut)
        group.MapGet("/{id:guid}/pdf", async (
            Guid id,
            ISender mediator,
            IInvoicePdfGenerator pdfGenerator,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var invoice = await mediator.Send(new GetInvoiceByIdQuery(id), ct);
            if (invoice is null)
                return Results.Problem(detail: $"Fatura {id} bulunamadı.", statusCode: 404);

            try
            {
                var pdfRequest = new InvoicePdfRequest(
                    InvoiceNumber: invoice.InvoiceNumber,
                    InvoiceType: invoice.Type.ToString(),
                    InvoiceDate: invoice.InvoiceDate,
                    SellerName: string.Empty, // Tenant config'den doldurulacak (generator içinde)
                    SellerVkn: string.Empty,
                    SellerTaxOffice: string.Empty,
                    SellerAddress: string.Empty,
                    BuyerName: invoice.CustomerName,
                    BuyerVkn: invoice.CustomerTaxNumber,
                    BuyerTaxOffice: invoice.CustomerTaxOffice,
                    BuyerAddress: invoice.CustomerAddress,
                    Currency: invoice.Currency,
                    SubTotal: invoice.SubTotal,
                    TaxTotal: invoice.TaxTotal,
                    GrandTotal: invoice.GrandTotal,
                    GibUuid: null,
                    Lines: invoice.Lines.Select((l, i) => new InvoiceLinePdfItem(
                        i + 1, l.ProductName, l.Quantity, "Adet",
                        l.UnitPrice, l.TaxRate, l.TaxAmount, l.LineTotal)).ToList());

                var pdfBytes = await pdfGenerator.GenerateInvoicePdfAsync(pdfRequest, ct);
                return Results.File(pdfBytes, "application/pdf",
                    $"fatura-{invoice.InvoiceNumber}.pdf");
            }
            catch (Exception ex)
            {
                loggerFactory.CreateLogger("InvoiceEndpoints")
                    .LogError(ex, "PDF generation failed for invoice {InvoiceId}", id);
                return Results.Problem(detail: "PDF oluşturulamadı.", statusCode: 500);
            }
        })
        .WithName("GetInvoicePdf")
        .WithSummary("Fatura PDF indir — A4 format, QuestPDF ile oluşturulur")
        .Produces(200).Produces(404).Produces(500);

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

        // POST /api/v1/invoices/{id}/upload/{platform} — fatura pazaryerine yükle (GAP-3 FIX)
        group.MapPost("/{id:guid}/upload/{platform}", async (
            Guid id,
            string platform,
            ISender mediator,
            IAdapterFactory adapterFactory,
            IInvoicePdfGenerator pdfGenerator,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("InvoiceEndpoints");
            var adapter = adapterFactory.Resolve(platform);
            if (adapter is null)
                return Results.Problem(detail: $"Platform '{platform}' adapter bulunamadı.", statusCode: 404);

            if (adapter is not IInvoiceCapableAdapter invoiceAdapter)
                return Results.Problem(detail: $"Platform '{platform}' fatura yükleme desteklemiyor.", statusCode: 400);

            var invoice = await mediator.Send(new GetInvoiceByIdQuery(id), ct);
            if (invoice is null)
                return Results.Problem(detail: $"Fatura {id} bulunamadı.", statusCode: 404);

            try
            {
                // PDF oluştur ve platforma yükle
                var pdfRequest = new InvoicePdfRequest(
                    InvoiceNumber: invoice.InvoiceNumber,
                    InvoiceType: invoice.Type.ToString(),
                    InvoiceDate: invoice.InvoiceDate,
                    SellerName: string.Empty, SellerVkn: string.Empty,
                    SellerTaxOffice: string.Empty, SellerAddress: string.Empty,
                    BuyerName: invoice.CustomerName,
                    BuyerVkn: invoice.CustomerTaxNumber,
                    BuyerTaxOffice: invoice.CustomerTaxOffice,
                    BuyerAddress: invoice.CustomerAddress,
                    Currency: invoice.Currency,
                    SubTotal: invoice.SubTotal, TaxTotal: invoice.TaxTotal, GrandTotal: invoice.GrandTotal,
                    GibUuid: null,
                    Lines: invoice.Lines.Select((l, i) => new InvoiceLinePdfItem(
                        i + 1, l.ProductName, l.Quantity, "Adet",
                        l.UnitPrice, l.TaxRate, l.TaxAmount, l.LineTotal)).ToList());

                var pdfBytes = await pdfGenerator.GenerateInvoicePdfAsync(pdfRequest, ct);
                var fileName = $"fatura-{invoice.InvoiceNumber}.pdf";

                var success = await invoiceAdapter.SendInvoiceFileAsync(
                    id.ToString(), pdfBytes, fileName, ct);

                if (!success)
                    return Results.Problem(detail: $"Fatura {platform}'a yüklenemedi.", statusCode: 502);

                logger.LogInformation(
                    "[InvoiceUpload] Fatura {InvoiceNumber} {Platform}'a yüklendi",
                    invoice.InvoiceNumber, platform);

                return Results.Ok(new { invoiceId = id, platform, uploaded = true, fileName });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Invoice upload failed: {InvoiceId} → {Platform}", id, platform);
                return Results.Problem(detail: "Fatura yükleme sırasında hata oluştu.", statusCode: 500);
            }
        })
        .WithName("UploadInvoiceToPlatform")
        .WithSummary("Fatura PDF'ini pazaryerine yükle (Trendyol, HB, N11)")
        .Produces(200).Produces(400).Produces(404).Produces(502)
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
        .Produces<List<InvoiceProviderStatusDto>>(200)
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
        .Produces<InvoiceReportDto>(200)
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
        .Produces<InvoiceSettingsDto>(200)
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

        // ═══ G85: E-FATURA OLUŞTURMA ═══

        // POST /api/v1/invoices/e-fatura — sipariş bazlı e-fatura/e-arşiv oluştur
        // VKN varsa e-Fatura, yoksa e-Arşiv tipi seçilir.
        group.MapPost("/e-fatura", async (
            GenerateEFaturaCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(command, ct);
            return Results.Ok(new { Message = "E-Fatura başarıyla oluşturuldu.", OrderId = command.OrderId });
        })
        .WithName("GenerateEFatura")
        .WithSummary("E-Fatura/E-Arşiv oluştur — sipariş bazlı otomatik fatura (G85)")
        .Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }
}
