using MesTech.Application.DTOs;
using MediatR;
using MesTech.Application.Features.Tenant.Commands.CreateTenant;
using MesTech.Application.Features.Tenant.Commands.UpdateTenant;
using MesTech.Application.Features.Tenant.Queries.GetTenant;
using MesTech.Application.Features.Tenant.Queries.GetTenants;
using Microsoft.AspNetCore.OutputCaching;

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
        group.MapGet("/", async (
            int page,
            int pageSize,
            ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTenantsQuery(page, pageSize), ct);
            return Results.Ok(result);
        })
        .WithName("GetTenants")
        .WithSummary("Kiracı listesi — admin only")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // POST /api/v1/admin/tenants — yeni kiracı oluştur
        group.MapPost("/", async (
            CreateTenantRequest request,
            ISender sender, CancellationToken ct) =>
        {
            var tenantId = await sender.Send(
                new CreateTenantCommand(request.Name, request.TaxNumber), ct);
            return Results.Created($"/api/v1/admin/tenants/{tenantId}", new CreatedResponse(tenantId));
        })
        .WithName("CreateTenant")
        .WithSummary("Yeni kiracı oluştur — admin only").Produces(200).Produces(400);

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
        .WithSummary("Kiracı detayı — admin only")
        .Produces(200)
        .CacheOutput("Lookup60s");

        // PUT /api/v1/admin/tenants/{id} — kiracı güncelle
        group.MapPut("/{id:guid}", async (
            Guid id, UpdateTenantRequest request,
            ISender sender, CancellationToken ct) =>
        {
            var success = await sender.Send(
                new UpdateTenantCommand(id, request.Name, request.TaxNumber, request.IsActive), ct);
            return success ? Results.NoContent() : Results.NotFound(new { error = $"Tenant {id} not found" });
        })
        .WithName("UpdateTenant")
        .WithSummary("Kiracı bilgilerini güncelle — admin only").Produces(200).Produces(400);
    }

    private record CreateTenantRequest(string Name, string? TaxNumber);
    private record UpdateTenantRequest(string Name, string? TaxNumber, bool IsActive);
}
