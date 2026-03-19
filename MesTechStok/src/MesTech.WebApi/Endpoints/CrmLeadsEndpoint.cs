// DEV1-DEPENDENCY: Application CRM Leads CQRS handlers not yet created.
// When DEV 1 creates GetLeadsQuery (full) / CreateLeadCommand (full), restore ISender dispatch.
//
// NOTE: CrmEndpoints.cs already has stub routes at /leads (GET/POST).
// This file uses /leads-search and /leads-new to avoid route conflicts.
// When DEV1 handlers are ready, consolidate into CrmEndpoints.cs.

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
        group.MapGet("/leads-search", (
            Guid tenantId,
            string? status,
            string? source,
            string? search,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default) =>
        {
            // DEV1-DEPENDENCY: GetLeadsQuery (full version) not yet available
            // Final: var result = await mediator.Send(
            //   new GetLeadsQuery(tenantId, status, source, search, page, pageSize), ct);
            return Results.Ok(new
            {
                TenantId = tenantId,
                Status = status,
                Source = source,
                Search = search,
                Page = page,
                PageSize = pageSize,
                Items = Array.Empty<object>(),
                TotalCount = 0,
                Message = "CRM Leads search endpoint stub — DEV1 Application handler pending"
            });
        })
        .WithName("SearchLeads")
        .WithSummary("Lead arama — durum, kaynak, metin filtresi (EMR-09)");

        // POST /api/v1/crm/leads-new — create a new lead with full details
        group.MapPost("/leads-new", (
            CreateLeadRequest request,
            CancellationToken ct) =>
        {
            // DEV1-DEPENDENCY: CreateLeadCommand (full version) not yet available
            // Final: var id = await mediator.Send(new CreateLeadCommand(...), ct);
            var stubId = Guid.NewGuid();
            return Results.Created($"/api/v1/crm/leads/{stubId}", new
            {
                Id = stubId,
                Request = request,
                Message = "CRM CreateLead endpoint stub — DEV1 Application handler pending"
            });
        })
        .WithName("CreateLeadFull")
        .WithSummary("Yeni lead oluştur — detaylı bilgi ile (EMR-09)");
    }

    /// <summary>Request DTO for full lead creation.</summary>
    public record CreateLeadRequest(
        Guid TenantId,
        string ContactName,
        string? Email,
        string? Phone,
        string? Company,
        string? Source,
        string? Notes);
}
