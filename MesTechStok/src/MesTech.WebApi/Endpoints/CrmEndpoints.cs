using MediatR;
using MesTech.Application.Features.Crm.Commands.CreateLead;
using MesTech.Application.Features.Crm.Commands.CreateDeal;
using MesTech.Application.Features.Crm.Commands.WinDeal;
using MesTech.Application.Features.Crm.Commands.LoseDeal;
using MesTech.Application.Features.Crm.Queries.GetDeals;
using MesTech.Application.Features.Crm.Queries.GetLeads;
using MesTech.Application.Features.Crm.Queries.GetPipelineKanban;
using MesTech.Application.Features.Crm.Queries.GetSuppliersCrm;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

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
                new GetLeadsQuery(tenantId, status, null, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetLeads")
        .WithSummary("Potansiyel müşteri listesi");

        // POST /api/v1/crm/leads
        group.MapPost("/leads", async (
            ISender mediator,
            CreateLeadCommand command,
            CancellationToken ct = default) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/crm/leads/{id}", new { id });
        })
        .WithName("CreateLead")
        .WithSummary("Yeni potansiyel müşteri oluştur");

        // POST /api/v1/crm/deals
        group.MapPost("/deals", async (
            ISender mediator,
            CreateDealCommand command,
            CancellationToken ct = default) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/crm/deals/{id}", new { id });
        })
        .WithName("CreateDeal")
        .WithSummary("Yeni fırsat oluştur");

        // POST /api/v1/crm/deals/{dealId}/win-and-create-order
        group.MapPost("/deals/{dealId:guid}/win-and-create-order",
            async (Guid dealId, ICrmOrderBridgeService bridge, CancellationToken ct) =>
            {
                var orderId = await bridge.CreateOrderFromDealAsync(dealId, ct);
                return Results.Ok(new { dealId, orderId });
            })
            .WithName("WinDealAndCreateOrder")
            .WithSummary("Deal kazanildi — Order baglantisi olustur (H27-3.4)");

        // POST /api/v1/crm/orders/{orderId}/create-lead
        group.MapPost("/orders/{orderId:guid}/create-lead",
            async (Guid orderId, ICrmOrderBridgeService bridge, CancellationToken ct) =>
            {
                var leadId = await bridge.CreateLeadFromOrderAsync(orderId, ct);
                return leadId.HasValue
                    ? Results.Created($"/api/v1/crm/leads/{leadId}", new { orderId, leadId })
                    : Results.Ok(new { orderId, message = "Lead zaten mevcut" });
            })
            .WithName("CreateLeadFromOrder")
            .WithSummary("Pazaryeri siparisi → Lead olustur (H27-3.4)");

        // POST /api/v1/crm/deals/{id}/win — Deal kazanıldı (H29-3.3)
        group.MapPost("/deals/{id:guid}/win", async (
            Guid id, Guid? orderId,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new WinDealCommand(id, orderId), ct);
            return Results.NoContent();
        })
        .WithName("WinDeal")
        .WithSummary("Deal kazanıldı olarak işaretle");

        // POST /api/v1/crm/deals/{id}/lose — Deal kaybedildi (H29-3.3)
        group.MapPost("/deals/{id:guid}/lose", async (
            Guid id, string reason,
            ISender mediator, CancellationToken ct) =>
        {
            await mediator.Send(new LoseDealCommand(id, reason), ct);
            return Results.NoContent();
        })
        .WithName("LoseDeal")
        .WithSummary("Deal kaybedildi olarak işaretle");

        // GET /api/v1/crm/deals — fırsat listesi
        group.MapGet("/deals", async (
            Guid tenantId, Guid? pipelineId, int? status, Guid? assignedTo,
            int page, int pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetDealsQuery(tenantId, pipelineId,
                    status.HasValue ? (DealStatus)status.Value : null,
                    assignedTo, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetDeals")
        .WithSummary("Fırsat listesi (pipeline + durum + atanan filtresi)");

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
        .WithSummary("Pipeline kanban board görünümü");

        // GET /api/v1/crm/suppliers — tedarikçi listesi
        group.MapGet("/suppliers", async (
            Guid tenantId, bool? isActive, bool? isPreferred, string? search,
            int page, int pageSize,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(
                new GetSuppliersCrmQuery(tenantId, isActive, isPreferred, search, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetSuppliersCrm")
        .WithSummary("Tedarikçi listesi (aktif + tercihli + arama filtresi)");
    }
}
