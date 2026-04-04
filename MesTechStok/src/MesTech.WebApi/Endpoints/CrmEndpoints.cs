using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Crm;
using MesTech.Application.Commands.SyncBitrix24Contacts;
using MesTech.Application.Features.Crm.Commands.CreateLead;
using MesTech.Application.Features.Crm.Commands.CreateDeal;
using MesTech.Application.Features.Crm.Commands.SaveCrmSettings;
using MesTech.Application.Features.Crm.Queries.GetCrmSettings;
using MesTech.Application.Features.Crm.Commands.UpdateDealStage;
using MesTech.Application.Features.Crm.Commands.WinDeal;
using MesTech.Application.Features.Crm.Commands.LoseDeal;
using MesTech.Application.Features.Crm.Queries.GetBitrix24Deals;
using MesTech.Application.Features.Crm.Queries.GetBitrix24Pipeline;
using MesTech.Application.Features.Crm.Queries.GetContactsPaged;
using MesTech.Application.Features.Crm.Queries.GetDeals;
using MesTech.Application.Features.Crm.Queries.GetLeadScore;
using MesTech.Application.Features.Crm.Queries.GetLeads;
using MesTech.Application.Features.Crm.Queries.GetPipelineKanban;
using MesTech.Application.Features.Crm.Queries.GetSuppliersCrm;
using MesTech.Application.Interfaces;
using MesTech.Application.Queries.GetBitrix24DealStatus;
using MesTech.Application.Queries.GetSuppliersPaged;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

public static class CrmEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/crm")
            .WithTags("CRM")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/crm/leads
        group.MapGet("/leads", async (
            ISender mediator,
            Guid tenantId,
            LeadStatus? status = null,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetLeadsQuery(tenantId, status, null, Math.Max(1, page), Math.Clamp(pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetLeads")
        .WithSummary("Potansiyel müşteri listesi")
        .Produces<GetLeadsResult>(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/crm/leads
        group.MapPost("/leads", async (
            ISender mediator,
            CreateLeadCommand command,
            CancellationToken ct = default) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/crm/leads/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateLead")
        .WithSummary("Yeni potansiyel müşteri oluştur")
        .Produces(201).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/crm/deals
        group.MapPost("/deals", async (
            ISender mediator,
            CreateDealCommand command,
            CancellationToken ct = default) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/crm/deals/{id}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(id)));
        })
        .WithName("CreateDeal")
        .WithSummary("Yeni fırsat oluştur")
        .Produces(201).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/crm/deals/{dealId}/win-and-create-order
        group.MapPost("/deals/{dealId:guid}/win-and-create-order",
            async (Guid dealId, ICrmOrderBridgeService bridge, CancellationToken ct) =>
            {
                var orderId = await bridge.CreateOrderFromDealAsync(dealId, ct);
                return Results.Ok(ApiResponse<object>.Ok(new { dealId, orderId }));
            })
            .WithName("WinDealAndCreateOrder")
            .WithSummary("Deal kazanildi — Order baglantisi olustur (H27-3.4)")
            .Produces(200).Produces(400)
            .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/crm/orders/{orderId}/create-lead
        group.MapPost("/orders/{orderId:guid}/create-lead",
            async (Guid orderId, ICrmOrderBridgeService bridge, CancellationToken ct) =>
            {
                var leadId = await bridge.CreateLeadFromOrderAsync(orderId, ct);
                return leadId.HasValue
                    ? Results.Created($"/api/v1/crm/leads/{leadId}", ApiResponse<CreatedResponse>.Ok(new CreatedResponse(leadId.Value)))
                    : Results.Ok(ApiResponse<StatusResponse>.Ok(new StatusResponse("exists", "Lead zaten mevcut")));
            })
            .WithName("CreateLeadFromOrder")
            .WithSummary("Pazaryeri siparisi → Lead olustur (H27-3.4)")
            .Produces(201).Produces(200)
            .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/crm/deals/{id}/win — Deal kazanıldı (H29-3.3)
        group.MapPost("/deals/{id:guid}/win", async (
            Guid id, Guid? orderId,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new WinDealCommand(id, orderId), ct);
            return Results.NoContent();
        })
        .WithName("WinDeal")
        .WithSummary("Deal kazanıldı olarak işaretle")
        .Produces(204).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // POST /api/v1/crm/deals/{id}/lose — Deal kaybedildi (H29-3.3)
        group.MapPost("/deals/{id:guid}/lose", async (
            Guid id, string reason,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new LoseDealCommand(id, reason), ct);
            return Results.NoContent();
        })
        .WithName("LoseDeal")
        .WithSummary("Deal kaybedildi olarak işaretle")
        .Produces(204).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/crm/deals — fırsat listesi
        group.MapGet("/deals", async (
            Guid tenantId, Guid? pipelineId, int? status, Guid? assignedTo,
            int page, int pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetDealsQuery(tenantId, pipelineId,
                    status.HasValue ? (DealStatus)status.Value : null,
                    assignedTo, Math.Max(1, page), Math.Clamp(pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetDeals")
        .WithSummary("Fırsat listesi (pipeline + durum + atanan filtresi)")
        .Produces<GetDealsResult>(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/crm/pipelines/{pipelineId}/kanban — kanban görünümü
        group.MapGet("/pipelines/{pipelineId:guid}/kanban", async (
            Guid pipelineId, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetPipelineKanbanQuery(tenantId, pipelineId), ct);
            return Results.Ok(result);
        })
        .WithName("GetPipelineKanban")
        .WithSummary("Pipeline kanban board görünümü")
        .Produces<KanbanBoardDto>(200)
        .CacheOutput("Dashboard30s");

        // GET /api/v1/crm/suppliers — tedarikçi listesi
        group.MapGet("/suppliers", async (
            Guid tenantId, bool? isActive, bool? isPreferred, string? search,
            int page, int pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var safeSearch = search is { Length: > 500 } ? search[..500] : search;
            var result = await mediator.Send(
                new GetSuppliersCrmQuery(tenantId, isActive, isPreferred, safeSearch, Math.Max(1, page), Math.Clamp(pageSize, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetSuppliersCrm")
        .WithSummary("Tedarikçi listesi (aktif + tercihli + arama filtresi)")
        .Produces<GetSuppliersCrmResult>(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/crm/bitrix24/sync-contacts — Bitrix24 contact senkronizasyonu
        group.MapPost("/bitrix24/sync-contacts", async (
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new SyncBitrix24ContactsCommand(), ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.UnprocessableEntity(result);
        })
        .WithName("SyncBitrix24Contacts")
        .WithSummary("Bitrix24 CRM contact senkronizasyonu başlat")
        .Produces(200).Produces(422)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/crm/bitrix24/deals — Bitrix24 deal listesi
        group.MapGet("/bitrix24/deals", async (
            Guid tenantId, Guid? stageId, int? page, int? pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetBitrix24DealsQuery(tenantId, stageId, page ?? 1, Math.Clamp(pageSize ?? 50, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetBitrix24Deals")
        .WithSummary("Bitrix24 deal listesi (stage filtreli)")
        .Produces<Bitrix24DealsResult>(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/crm/bitrix24/deal-status/{orderId} — sipariş → Bitrix24 deal durumu
        group.MapGet("/bitrix24/deal-status/{orderId:guid}", async (
            Guid orderId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBitrix24DealStatusQuery(orderId), ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetBitrix24DealStatus")
        .WithSummary("Sipariş → Bitrix24 deal durum eşleşmesi")
        .Produces<Bitrix24DealStatusDto>(200).Produces(404)
        .CacheOutput("Lookup60s");

        // GET /api/v1/crm/bitrix24/pipeline — Bitrix24 pipeline durumu
        group.MapGet("/bitrix24/pipeline", async (
            Guid tenantId, string? stageFilter,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetBitrix24PipelineQuery(tenantId, stageFilter), ct);
            return Results.Ok(result);
        })
        .WithName("GetBitrix24Pipeline")
        .WithSummary("Bitrix24 pipeline durumu ve stage dağılımı")
        .Produces<Bitrix24PipelineResult>(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/crm/contacts — paginated contact list
        group.MapGet("/contacts", async (
            Guid tenantId, int? page, int? pageSize, string? search,
            ISender mediator, CancellationToken ct) =>
        {
            var safeSearch = search is { Length: > 500 } ? search[..500] : search;
            var result = await mediator.Send(
                new GetContactsPagedQuery(tenantId, page ?? 1, Math.Clamp(pageSize ?? 20, 1, 100), safeSearch), ct);
            return Results.Ok(result);
        })
        .WithName("GetContactsPaged")
        .WithSummary("Kişi listesi — sayfalanmış, arama destekli")
        .Produces<ContactsPagedResult>(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/crm/leads/{leadId}/score — lead scoring
        group.MapGet("/leads/{leadId:guid}/score", async (
            Guid leadId, Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetLeadScoreQuery(leadId, tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetLeadScore")
        .WithSummary("Potansiyel müşteri puanlama — RFM + davranış skoru")
        .Produces<LeadScoreResult>(200)
        .CacheOutput("Report120s");

        // PUT /api/v1/crm/deals/{dealId}/stage — deal stage güncelleme
        group.MapPut("/deals/{dealId:guid}/stage", async (
            Guid dealId, UpdateDealStageCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var adjusted = command with { DealId = dealId };
            var result = await mediator.Send(adjusted, ct);
            return result.IsSuccess
                ? Results.Ok(result)
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("UpdateDealStage")
        .WithSummary("Deal pipeline aşamasını güncelle")
        .Produces(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();

        // GET /api/v1/crm/suppliers/paged — sayfalanmış tedarikçi listesi
        group.MapGet("/suppliers/paged", async (
            string? search, int? page, int? pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var safeSearch = search is { Length: > 500 } ? search[..500] : search;
            var result = await mediator.Send(
                new GetSuppliersPagedQuery(safeSearch, page ?? 1, Math.Clamp(pageSize ?? 50, 1, 100)), ct);
            return Results.Ok(result);
        })
        .WithName("GetCrmSuppliersPaged")
        .WithSummary("Sayfalanmış tedarikçi listesi (arama destekli)")
        .Produces<PagedSupplierResult>(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/crm/settings — CRM ayarları (G564)
        group.MapGet("/settings", async (
            Guid tenantId,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetCrmSettingsQuery(tenantId), ct);
            return Results.Ok(result);
        })
        .WithName("GetCrmSettings")
        .WithSummary("CRM ayarları — auto-assign, pipeline, lead score threshold")
        .Produces<CrmSettingsDto>(200);

        // POST /api/v1/crm/settings — CRM ayarları kaydet (G564)
        group.MapPost("/settings", async (
            SaveCrmSettingsCommand command,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new StatusResponse("saved", "CRM settings saved"))
                : Results.Problem(detail: result.ErrorMessage, statusCode: 400);
        })
        .WithName("SaveCrmSettings")
        .WithSummary("CRM ayarları kaydet — lead scoring, pipeline, email tracking")
        .Produces<StatusResponse>(200).Produces(400)
        .AddEndpointFilter<Filters.IdempotencyFilter>();
    }
}
