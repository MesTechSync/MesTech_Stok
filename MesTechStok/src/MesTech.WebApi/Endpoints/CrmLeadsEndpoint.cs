using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.Crm.Commands.CreateLead;
using MesTech.Application.Features.Crm.Queries.GetLeads;
using MesTech.Domain.Enums;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// CRM Leads endpoints — advanced lead search and creation.
/// GET  /api/v1/crm/leads-search — filterable lead list with pagination
/// POST /api/v1/crm/leads-new    — create lead with full details
/// </summary>
public static class CrmLeadsEndpoint
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/crm")
            .WithTags("CRM")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/crm/leads-search — advanced lead search with filters
        group.MapGet("/leads-search", async (
            ISender mediator,
            Guid tenantId,
            LeadStatus? status = null,
            Guid? assignedTo = null,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default) =>
        {
            var result = await mediator.Send(
                new GetLeadsQuery(tenantId, status, assignedTo, page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("SearchLeads")
        .WithSummary("Lead arama — durum, atanan kişi filtresi (EMR-09)")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/crm/leads-new — create a new lead with full details
        group.MapPost("/leads-new", async (
            ISender mediator,
            CreateLeadCommand command,
            CancellationToken ct = default) =>
        {
            var id = await mediator.Send(command, ct);
            return Results.Created($"/api/v1/crm/leads/{id}", new CreatedResponse(id));
        })
        .WithName("CreateLeadFull")
        .WithSummary("Yeni lead oluştur — detaylı bilgi ile (EMR-09)").Produces(200).Produces(400);
    }
}
