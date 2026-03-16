namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Admin-only tenant management endpoints.
/// DEV1-DEPENDENCY: Tenant CQRS handlers (GetTenantsQuery, CreateTenantCommand, GetTenantByIdQuery)
/// not yet available. Endpoints are stubbed until Application handlers are created by DEV 1.
/// </summary>
public static class TenantEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/admin/tenants")
            .WithTags("Tenants (Admin)")
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/admin/tenants — kiracı listesi (admin)
        // DEV1-DEPENDENCY: GetTenantsQuery not yet available
        group.MapGet("/", (int page = 1, int pageSize = 50) =>
            Results.Ok(new
            {
                Message = "Tenant list endpoint — DEV1 GetTenantsQuery pending",
                Page = page,
                PageSize = pageSize,
                Items = Array.Empty<object>(),
                TotalCount = 0,
                Status = "not_implemented"
            }))
        .WithName("GetTenants")
        .WithSummary("Kiracı listesi — admin only (DEV1-DEPENDENCY)");

        // POST /api/v1/admin/tenants — yeni kiracı oluştur
        // DEV1-DEPENDENCY: CreateTenantCommand not yet available
        group.MapPost("/", () =>
            Results.Accepted("/api/v1/admin/tenants", new
            {
                Message = "Create tenant endpoint — DEV1 CreateTenantCommand pending",
                Status = "not_implemented"
            }))
        .WithName("CreateTenant")
        .WithSummary("Yeni kiracı oluştur — admin only (DEV1-DEPENDENCY)");

        // GET /api/v1/admin/tenants/{id} — kiracı detayı
        // DEV1-DEPENDENCY: GetTenantByIdQuery not yet available
        group.MapGet("/{id:guid}", (Guid id) =>
            Results.Ok(new
            {
                Message = "Tenant detail endpoint — DEV1 GetTenantByIdQuery pending",
                TenantId = id,
                Status = "not_implemented"
            }))
        .WithName("GetTenantById")
        .WithSummary("Kiracı detayı — admin only (DEV1-DEPENDENCY)");
    }
}
