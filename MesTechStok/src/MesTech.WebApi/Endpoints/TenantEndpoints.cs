using MediatR;
using MesTech.Application.Features.Tenant.Commands.CreateTenant;
using MesTech.Application.Features.Tenant.Commands.UpdateTenant;
using MesTech.Application.Features.Tenant.Queries.GetTenant;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Admin-only tenant management endpoints.
/// </summary>
public static class TenantEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin/tenants")
            .WithTags("Tenants (Admin)")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/admin/tenants — kiracı listesi (admin)
        // TODO: GetTenantsQuery (list all) handler not yet available — only GetTenantQuery (by ID) exists
        group.MapGet("/", (int page = 1, int pageSize = 50) =>
            Results.Ok(new
            {
                Message = "Tenant list endpoint — GetTenantsQuery (list) handler not yet available",
                Page = page,
                PageSize = pageSize,
                Items = Array.Empty<object>(),
                TotalCount = 0,
                Status = "not_implemented"
            }))
        .WithName("GetTenants")
        .WithSummary("Kiracı listesi — admin only (TODO: GetTenantsQuery handler gerekli)");

        // POST /api/v1/admin/tenants — yeni kiracı oluştur
        group.MapPost("/", async (
            CreateTenantRequest request,
            ISender sender, CancellationToken ct) =>
        {
            var tenantId = await sender.Send(
                new CreateTenantCommand(request.Name, request.TaxNumber), ct);
            return Results.Created($"/api/v1/admin/tenants/{tenantId}", new { id = tenantId });
        })
        .WithName("CreateTenant")
        .WithSummary("Yeni kiracı oluştur — admin only");

        // GET /api/v1/admin/tenants/{id} — kiracı detayı
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender sender, CancellationToken ct) =>
        {
            var tenant = await sender.Send(new GetTenantQuery(id), ct);
            return tenant is not null
                ? Results.Ok(tenant)
                : Results.NotFound(new { error = $"Tenant {id} not found" });
        })
        .WithName("GetTenantById")
        .WithSummary("Kiracı detayı — admin only");
    }

    private record CreateTenantRequest(string Name, string? TaxNumber);
}
