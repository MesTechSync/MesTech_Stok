using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Accounting;
using MediatR;
using MesTech.Application.Commands.CreateCariHareket;
using MesTech.Application.Commands.CreateCariHesap;
using MesTech.Application.Commands.DeleteCariHesap;
using MesTech.Application.Commands.UpdateCariHesap;
using MesTech.Application.Queries.GetCariHareketler;
using MesTech.Application.Queries.GetCariHesaplar;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class CariHesapEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/accounting/cari-hesaplar")
            .WithTags("Accounting - Cari Hesaplar")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/accounting/cari-hesaplar — cari hesap listesi
        group.MapGet("/", async (
            Guid? tenantId, CariHesapType? type,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCariHesaplarQuery(type, tenantId), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetCariHesaplar")
        .WithSummary("Cari hesap listesi (tip filtresi: Musteri / Tedarikci / HerIkisi)").Produces<IReadOnlyList<CariHesapDto>>(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/accounting/cari-hesaplar/{id} — tek cari hesap
        group.MapGet("/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            // Return all and let client filter — no GetById query exists yet
            var result = await mediator.Send(
                new GetCariHesaplarQuery(null, null), ct);
            var item = result.FirstOrDefault(c => c.Id == id);
            return item is not null ? Results.Ok(item) : Results.NotFound();
        })
        .CacheOutput("Lookup60s")
        .WithName("GetCariHesapById")
        .WithSummary("Tek cari hesap detayi").Produces<CariHesapDto>(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/accounting/cari-hesaplar — yeni cari hesap olustur
        group.MapPost("/", async (
            CreateCariHesapCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/accounting/cari-hesaplar/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateCariHesap")
        .WithSummary("Yeni cari hesap olustur").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // PUT /api/v1/accounting/cari-hesaplar/{id} — cari hesap guncelle
        group.MapPut("/{id:guid}", async (
            Guid id, UpdateCariHesapCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { Id = id };
            await mediator.Send(updated, ct);
            return Results.NoContent();
        })
        .WithName("UpdateCariHesap")
        .WithSummary("Cari hesap bilgilerini guncelle").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // DELETE /api/v1/accounting/cari-hesaplar/{id} — cari hesap sil (soft-delete)
        group.MapDelete("/{id:guid}", async (
            Guid id, ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DeleteCariHesapCommand(id), ct);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("DeleteCariHesap")
        .WithSummary("Cari hesap sil — bakiye sıfır olmalı, hareket yoksa soft-delete")
        .Produces(204).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/accounting/cari-hesaplar/{id}/hareketler — cari hesap hareketleri
        group.MapGet("/{id:guid}/hareketler", async (
            Guid id, DateTime? from, DateTime? to,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetCariHareketlerQuery(id, from, to), ct);
            return Results.Ok(result);
        })
        .CacheOutput("Lookup60s")
        .WithName("GetCariHareketler")
        .WithSummary("Cari hesap hareketleri (tarih filtresi)").Produces<IReadOnlyList<CariHareketDto>>(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/accounting/cari-hesaplar/{id}/hareketler — cari hareket oluştur
        group.MapPost("/{id:guid}/hareketler", async (
            Guid id, CreateCariHareketCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var updated = command with { CariHesapId = id };
            var result = await mediator.Send(updated, ct);
            return Results.Created($"/api/v1/accounting/cari-hesaplar/{id}/hareketler/{result}", new CreatedResponse(result));
        })
        .WithName("CreateCariHareket")
        .WithSummary("Yeni cari hareket oluştur (borç/alacak)").Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }
}
