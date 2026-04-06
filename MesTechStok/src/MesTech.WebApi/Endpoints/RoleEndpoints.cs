using MesTech.Application.DTOs;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.WebApi.Filters;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Role CRUD endpoints — GET/POST/PUT/DELETE /api/v1/roles (HH-DEV6-005).
/// Permission assignment: POST /api/v1/roles/{id}/permissions (HH-DEV6-006).
/// </summary>
public static class RoleEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/roles").WithTags("Roles")
            .RequireRateLimiting("PerApiKey")
            .RequireAuthorization();

        // GET /api/v1/roles — list all roles
        group.MapGet("/", async (
            IRoleRepository roleRepo,
            CancellationToken ct) =>
        {
            var roles = await roleRepo.GetAllAsync(ct);
            var dtos = roles.Select(r => new RoleDto(
                r.Id, r.Name, r.Description, r.IsActive, r.IsSystemRole, r.TenantId)).ToList();
            return Results.Ok(ApiResponse<List<RoleDto>>.Ok(dtos));
        })
        .WithName("GetRoles")
        .WithSummary("Tüm rolleri listele")
        .Produces<ApiResponse<List<RoleDto>>>(200)
        .RequirePermission("ManageRoles");

        // GET /api/v1/roles/{id} — get role by id with permissions
        group.MapGet("/{id:guid}", async (
            Guid id,
            IRoleRepository roleRepo,
            CancellationToken ct) =>
        {
            var role = await roleRepo.GetByIdAsync(id, ct);
            if (role is null)
                return Results.NotFound(ApiResponse<RoleDetailDto>.Fail("Role not found."));

            var dto = new RoleDetailDto(
                role.Id, role.Name, role.Description, role.IsActive, role.IsSystemRole,
                role.TenantId,
                role.RolePermissions.Select(rp => rp.PermissionId).ToList());
            return Results.Ok(ApiResponse<RoleDetailDto>.Ok(dto));
        })
        .WithName("GetRoleById")
        .WithSummary("Rol detayı — permission ID'leri ile birlikte")
        .Produces<ApiResponse<RoleDetailDto>>(200)
        .Produces(404)
        .RequirePermission("ManageRoles");

        // POST /api/v1/roles — create role
        group.MapPost("/", async (
            CreateRoleRequest request,
            IRoleRepository roleRepo,
            IUnitOfWork unitOfWork,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(ApiResponse<CreatedResponse>.Fail("Role name is required."));

            var existing = await roleRepo.GetByNameAsync(request.Name, ct);
            if (existing is not null)
                return Results.Conflict(ApiResponse<CreatedResponse>.Fail("Role name already exists."));

            var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
                return Results.BadRequest(ApiResponse<CreatedResponse>.Fail("Tenant context is required."));

            var role = new Role
            {
                Name = request.Name,
                Description = request.Description,
                IsActive = true,
                IsSystemRole = false,
                TenantId = tenantId
            };

            await roleRepo.AddAsync(role, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/roles/{role.Id}",
                ApiResponse<CreatedResponse>.Ok(new CreatedResponse(role.Id)));
        })
        .WithName("CreateRole")
        .WithSummary("Yeni rol oluştur (HH-DEV6-005)")
        .Produces<ApiResponse<CreatedResponse>>(StatusCodes.Status201Created)
        .Produces(400).Produces(409)
        .RequirePermission("ManageRoles");

        // PUT /api/v1/roles/{id} — update role
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateRoleRequest request,
            IRoleRepository roleRepo,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var role = await roleRepo.GetByIdAsync(id, ct);
            if (role is null)
                return Results.NotFound(ApiResponse<StatusResponse>.Fail("Role not found."));

            if (role.IsSystemRole)
                return Results.BadRequest(ApiResponse<StatusResponse>.Fail("System roles cannot be modified."));

            if (request.Name is not null) role.Name = request.Name;
            if (request.Description is not null) role.Description = request.Description;
            if (request.IsActive.HasValue) role.IsActive = request.IsActive.Value;

            await roleRepo.UpdateAsync(role, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Results.Ok(ApiResponse<StatusResponse>.Ok(
                new StatusResponse("Updated", "Rol güncellendi.")));
        })
        .WithName("UpdateRole")
        .WithSummary("Rol bilgilerini güncelle")
        .Produces<ApiResponse<StatusResponse>>(200)
        .Produces(404)
        .RequirePermission("ManageRoles");

        // DELETE /api/v1/roles/{id} — soft-delete role (IsActive=false)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IRoleRepository roleRepo,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var role = await roleRepo.GetByIdAsync(id, ct);
            if (role is null)
                return Results.NotFound(ApiResponse<StatusResponse>.Fail("Role not found."));

            if (role.IsSystemRole)
                return Results.BadRequest(ApiResponse<StatusResponse>.Fail("System roles cannot be deleted."));

            role.IsActive = false;
            await roleRepo.UpdateAsync(role, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Results.Ok(ApiResponse<StatusResponse>.Ok(
                new StatusResponse("Deleted", "Rol devre dışı bırakıldı.")));
        })
        .WithName("DeleteRole")
        .WithSummary("Rol soft-delete (IsActive=false)")
        .Produces<ApiResponse<StatusResponse>>(200)
        .Produces(404)
        .RequirePermission("ManageRoles");

        // POST /api/v1/roles/{id}/permissions — assign permissions to role (HH-DEV6-006)
        group.MapPost("/{id:guid}/permissions", async (
            Guid id,
            AssignPermissionsRequest request,
            IRoleRepository roleRepo,
            IUnitOfWork unitOfWork,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var role = await roleRepo.GetByIdAsync(id, ct);
            if (role is null)
                return Results.NotFound(ApiResponse<StatusResponse>.Fail("Role not found."));

            if (request.PermissionIds is null || request.PermissionIds.Count == 0)
                return Results.BadRequest(ApiResponse<StatusResponse>.Fail("At least one permission ID is required."));

            var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            Guid.TryParse(userIdClaim, out var grantedByUserId);

            // Get existing permission IDs on this role
            var existingPermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

            // Add only new permissions (idempotent)
            var newPermissions = request.PermissionIds
                .Where(pid => !existingPermissionIds.Contains(pid))
                .Select(pid => new RolePermission
                {
                    RoleId = id,
                    PermissionId = pid,
                    TenantId = role.TenantId,
                    GrantedDate = DateTime.UtcNow,
                    GrantedByUserId = grantedByUserId == Guid.Empty ? null : grantedByUserId
                })
                .ToList();

            if (newPermissions.Count > 0)
            {
                // Access DbContext through UnitOfWork pattern — add RolePermissions
                var dbContext = httpContext.RequestServices
                    .GetRequiredService<MesTech.Infrastructure.Persistence.AppDbContext>();
                await dbContext.Set<RolePermission>().AddRangeAsync(newPermissions, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }

            return Results.Ok(ApiResponse<StatusResponse>.Ok(
                new StatusResponse("PermissionsAssigned",
                    $"{newPermissions.Count} yeni permission atandı.")));
        })
        .WithName("AssignPermissionsToRole")
        .WithSummary("Role permission ata — idempotent (HH-DEV6-006)")
        .Produces<ApiResponse<StatusResponse>>(200)
        .Produces(400).Produces(404)
        .RequirePermission("ManageRoles");
    }

    // ── DTOs ──

    public record RoleDto(
        Guid Id, string Name, string? Description, bool IsActive, bool IsSystemRole, Guid TenantId);

    public record RoleDetailDto(
        Guid Id, string Name, string? Description, bool IsActive, bool IsSystemRole,
        Guid TenantId, List<Guid> PermissionIds);

    public record CreateRoleRequest(string Name, string? Description);

    public record UpdateRoleRequest(string? Name, string? Description, bool? IsActive);

    public record AssignPermissionsRequest(List<Guid> PermissionIds);
}
