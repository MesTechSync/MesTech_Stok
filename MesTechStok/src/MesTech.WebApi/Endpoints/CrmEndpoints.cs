// DEV1-DEPENDENCY: Application CRM CQRS handlers not yet created.
// When DEV 1 creates the following, restore the using directives and handler bodies:
//   MesTech.Application.Features.Crm.Commands.CreateLead.CreateLeadCommand
//   MesTech.Application.Features.Crm.Commands.CreateDeal.CreateDealCommand
//   MesTech.Application.Features.Crm.Queries.GetLeads.GetLeadsQuery

// using MediatR;
// using MesTech.Application.Features.Crm.Commands.CreateLead;
// using MesTech.Application.Features.Crm.Commands.CreateDeal;
// using MesTech.Application.Features.Crm.Queries.GetLeads;
// using MesTech.Domain.Enums;

using MesTech.Application.Interfaces;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// CRM API endpoints — GET /api/v1/crm/leads, POST /api/v1/crm/leads, POST /api/v1/crm/deals.
/// DEV1-DEPENDENCY: Endpoints are stubbed until Application CQRS handlers are created by DEV 1.
///
/// Final implementation:
/// <code>
/// public static class CrmEndpoints
/// {
///     public static void Map(WebApplication app)
///     {
///         var group = app.MapGroup("/api/v1/crm")
///             .WithTags("CRM")
///             .RequireRateLimiting("PerApiKey");
///
///         // GET /api/v1/crm/leads
///         group.MapGet("/leads", async (
///             ISender mediator,
///             Guid tenantId,
///             LeadStatus? status = null,
///             int page = 1,
///             int pageSize = 50,
///             CancellationToken ct = default) =>
///         {
///             var result = await mediator.Send(
///                 new GetLeadsQuery(tenantId, status, null, page, pageSize), ct);
///             return Results.Ok(result);
///         })
///         .WithName("GetLeads")
///         .WithSummary("Potansiyel müşteri listesi");
///
///         // POST /api/v1/crm/leads
///         group.MapPost("/leads", async (
///             ISender mediator,
///             CreateLeadCommand command,
///             CancellationToken ct = default) =>
///         {
///             var id = await mediator.Send(command, ct);
///             return Results.Created($"/api/v1/crm/leads/{id}", new { id });
///         })
///         .WithName("CreateLead")
///         .WithSummary("Yeni potansiyel müşteri oluştur");
///
///         // POST /api/v1/crm/deals
///         group.MapPost("/deals", async (
///             ISender mediator,
///             CreateDealCommand command,
///             CancellationToken ct = default) =>
///         {
///             var id = await mediator.Send(command, ct);
///             return Results.Created($"/api/v1/crm/deals/{id}", new { id });
///         })
///         .WithName("CreateDeal")
///         .WithSummary("Yeni fırsat oluştur");
///     }
/// }
/// </code>
/// </summary>
public static class CrmEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/crm")
            .WithTags("CRM")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/crm/leads — DEV1-DEPENDENCY: GetLeadsQuery not yet available
        group.MapGet("/leads", (Guid? tenantId, int page = 1, int pageSize = 50) =>
            Results.Ok(new
            {
                Message = "CRM Leads endpoint stub — DEV1 Application handler pending",
                TenantId = tenantId,
                Page = page,
                PageSize = pageSize,
                Items = Array.Empty<object>(),
                TotalCount = 0
            }))
            .WithName("GetLeads")
            .WithSummary("Potansiyel müşteri listesi (DEV1-DEPENDENCY)");

        // POST /api/v1/crm/leads — DEV1-DEPENDENCY: CreateLeadCommand not yet available
        group.MapPost("/leads", () =>
            Results.Accepted("/api/v1/crm/leads", new
            {
                Message = "CRM CreateLead endpoint stub — DEV1 Application handler pending"
            }))
            .WithName("CreateLead")
            .WithSummary("Yeni potansiyel müşteri oluştur (DEV1-DEPENDENCY)");

        // POST /api/v1/crm/deals — DEV1-DEPENDENCY: CreateDealCommand not yet available
        group.MapPost("/deals", () =>
            Results.Accepted("/api/v1/crm/deals", new
            {
                Message = "CRM CreateDeal endpoint stub — DEV1 Application handler pending"
            }))
            .WithName("CreateDeal")
            .WithSummary("Yeni fırsat oluştur (DEV1-DEPENDENCY)");

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
    }
}
