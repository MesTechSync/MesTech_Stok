using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Commands.AcceptQuotation;
using MesTech.Application.Commands.ConvertQuotationToInvoice;
using MesTech.Application.Commands.CreateQuotation;
using MesTech.Application.Commands.RejectQuotation;
using MesTech.Application.Queries.GetQuotationById;
using MesTech.Application.Queries.ListQuotations;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace MesTech.WebApi.Endpoints;

internal record ConvertRequest(string InvoiceNumber);

public static class QuotationEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/quotations").WithTags("Quotations").RequireRateLimiting("PerApiKey");

        // GET /api/v1/quotations — list quotations (optional status filter)
        group.MapGet("/", async (ISender mediator, [FromQuery] string? status, CancellationToken ct) =>
        {
            QuotationStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<QuotationStatus>(status, ignoreCase: true, out var s))
            {
                parsedStatus = s;
            }

            var result = await mediator.Send(new ListQuotationsQuery(parsedStatus), ct);
            return Results.Ok(result);
        })
        .WithName("ListQuotations")
        .WithSummary("Teklif listesi (durum filtresi)");

        // GET /api/v1/quotations/{id} — get single quotation with lines
        group.MapGet("/{id:guid}", async (Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetQuotationByIdQuery(id), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetQuotationById")
        .WithSummary("Tekil teklif detayı");

        // POST /api/v1/quotations — create a new quotation
        group.MapPost("/", async (CreateQuotationCommand command, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/quotations/{result.QuotationId}", result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("CreateQuotation")
        .WithSummary("Yeni teklif oluştur");

        // POST /api/v1/quotations/{id}/accept — accept a quotation
        group.MapPost("/{id:guid}/accept", async (Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new AcceptQuotationCommand(id), ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("AcceptQuotation")
        .WithSummary("Teklifi kabul et");

        // POST /api/v1/quotations/{id}/reject — reject a quotation
        group.MapPost("/{id:guid}/reject", async (Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RejectQuotationCommand(id), ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("RejectQuotation")
        .WithSummary("Teklifi reddet");

        // POST /api/v1/quotations/{id}/convert — convert accepted quotation to invoice
        group.MapPost("/{id:guid}/convert", async (Guid id, [FromBody] ConvertRequest body, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ConvertQuotationToInvoiceCommand(id, body.InvoiceNumber), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/invoices/{result.InvoiceId}", result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("ConvertQuotationToInvoice")
        .WithSummary("Kabul edilen teklifi faturaya dönüştür");
    }
}
