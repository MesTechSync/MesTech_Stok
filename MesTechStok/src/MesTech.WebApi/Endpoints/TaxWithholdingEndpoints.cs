using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.Accounting.Commands.RecordTaxWithholding;
using MesTech.Application.Features.Accounting.Queries.GetWithholdingRates;
using MesTech.Application.Features.Accounting.Queries.ListTaxWithholdings;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class TaxWithholdingEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/accounting")
            .WithTags("Accounting - Tax Withholding")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/accounting/withholding-rates — KDV tevkifat oranlari
        group.MapGet("/withholding-rates", async (
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetWithholdingRatesQuery(), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetWithholdingRates")
        .WithSummary("KDV tevkifat oranlari listesi (GiB resmi listesi)");

        // GET /api/v1/accounting/tax-withholdings — tevkifat kayitlari listesi
        group.MapGet("/tax-withholdings", async (
            Guid tenantId, DateTime? from, DateTime? to,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new ListTaxWithholdingsQuery(tenantId, from, to), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("ListTaxWithholdings")
        .WithSummary("Tevkifat kayitlari listesi (tarih araligi filtresi)");

        // POST /api/v1/accounting/tax-withholdings — yeni tevkifat kaydi olustur
        group.MapPost("/tax-withholdings", async (
            RecordTaxWithholdingCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/tax-withholdings/{id}", new CreatedResponse(id));
        })
        .WithName("RecordTaxWithholding")
        .WithSummary("Yeni KDV tevkifat kaydi olustur");
    }
}
