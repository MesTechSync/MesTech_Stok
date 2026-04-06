using MesTech.Application.DTOs;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.WebApi.Filters;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// User CRUD endpoints — GET/POST/PUT/DELETE /api/v1/users (HH-DEV6-004).
/// Soft-delete pattern: DELETE sets IsActive=false.
/// </summary>
public static class UserEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/users").WithTags("Users")
            .RequireRateLimiting("PerApiKey")
            .RequireAuthorization();

        // GET /api/v1/users — list all users
        group.MapGet("/", async (
            IUserRepository userRepo,
            CancellationToken ct) =>
        {
            var users = await userRepo.GetAllAsync(ct);
            var dtos = users.Select(u => new UserDto(
                u.Id, u.Username, u.Email, u.FirstName, u.LastName,
                u.Phone, u.IsActive, u.IsEmailConfirmed, u.IsMfaEnabled,
                u.LastLoginDate, u.TenantId)).ToList();
            return Results.Ok(ApiResponse<List<UserDto>>.Ok(dtos));
        })
        .WithName("GetUsers")
        .WithSummary("Tüm kullanıcıları listele")
        .Produces<ApiResponse<List<UserDto>>>(200)
        .RequirePermission("ManageUsers");

        // GET /api/v1/users/{id} — get user by id
        group.MapGet("/{id:guid}", async (
            Guid id,
            IUserRepository userRepo,
            CancellationToken ct) =>
        {
            var user = await userRepo.GetByIdAsync(id, ct);
            if (user is null)
                return Results.NotFound(ApiResponse<UserDto>.Fail("User not found."));

            var dto = new UserDto(
                user.Id, user.Username, user.Email, user.FirstName, user.LastName,
                user.Phone, user.IsActive, user.IsEmailConfirmed, user.IsMfaEnabled,
                user.LastLoginDate, user.TenantId);
            return Results.Ok(ApiResponse<UserDto>.Ok(dto));
        })
        .WithName("GetUserById")
        .WithSummary("Kullanıcı detayı — ID ile")
        .Produces<ApiResponse<UserDto>>(200)
        .Produces(404)
        .RequirePermission("ManageUsers");

        // POST /api/v1/users — create user
        group.MapPost("/", async (
            CreateUserRequest request,
            IUserRepository userRepo,
            IUnitOfWork unitOfWork,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(ApiResponse<CreatedResponse>.Fail("Username and Password are required."));

            if (request.Password.Length < 8)
                return Results.BadRequest(ApiResponse<CreatedResponse>.Fail("Password must be at least 8 characters."));

            // Check duplicate username
            var existing = await userRepo.GetByUsernameAsync(request.Username, ct);
            if (existing is not null)
                return Results.Conflict(ApiResponse<CreatedResponse>.Fail("Username already exists."));

            // Extract tenantId from JWT claims
            var tenantClaim = httpContext.User.FindFirst("tenant_id")?.Value;
            if (string.IsNullOrEmpty(tenantClaim) || !Guid.TryParse(tenantClaim, out var tenantId))
                return Results.BadRequest(ApiResponse<CreatedResponse>.Fail("Tenant context is required."));

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsActive = true,
                TenantId = tenantId
            };

            await userRepo.AddAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/users/{user.Id}",
                ApiResponse<CreatedResponse>.Ok(new CreatedResponse(user.Id)));
        })
        .WithName("CreateUser")
        .WithSummary("Yeni kullanıcı oluştur (HH-DEV6-004)")
        .Produces<ApiResponse<CreatedResponse>>(StatusCodes.Status201Created)
        .Produces(400).Produces(409)
        .RequirePermission("ManageUsers");

        // PUT /api/v1/users/{id} — update user
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateUserRequest request,
            IUserRepository userRepo,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var user = await userRepo.GetByIdAsync(id, ct);
            if (user is null)
                return Results.NotFound(ApiResponse<StatusResponse>.Fail("User not found."));

            if (request.Email is not null) user.Email = request.Email;
            if (request.FirstName is not null) user.FirstName = request.FirstName;
            if (request.LastName is not null) user.LastName = request.LastName;
            if (request.Phone is not null) user.Phone = request.Phone;
            if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

            await userRepo.UpdateAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Results.Ok(ApiResponse<StatusResponse>.Ok(
                new StatusResponse("Updated", "Kullanıcı güncellendi.")));
        })
        .WithName("UpdateUser")
        .WithSummary("Kullanıcı bilgilerini güncelle")
        .Produces<ApiResponse<StatusResponse>>(200)
        .Produces(404)
        .RequirePermission("ManageUsers");

        // DELETE /api/v1/users/{id} — soft-delete user (IsActive=false)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IUserRepository userRepo,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var user = await userRepo.GetByIdAsync(id, ct);
            if (user is null)
                return Results.NotFound(ApiResponse<StatusResponse>.Fail("User not found."));

            user.IsActive = false;
            await userRepo.UpdateAsync(user, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Results.Ok(ApiResponse<StatusResponse>.Ok(
                new StatusResponse("Deleted", "Kullanıcı devre dışı bırakıldı.")));
        })
        .WithName("DeleteUser")
        .WithSummary("Kullanıcı soft-delete (IsActive=false)")
        .Produces<ApiResponse<StatusResponse>>(200)
        .Produces(404)
        .RequirePermission("ManageUsers");
    }

    // ── DTOs ──

    public record UserDto(
        Guid Id, string Username, string? Email, string? FirstName, string? LastName,
        string? Phone, bool IsActive, bool IsEmailConfirmed, bool IsMfaEnabled,
        DateTime? LastLoginDate, Guid TenantId);

    public record CreateUserRequest(
        string Username, string Password, string? Email,
        string? FirstName, string? LastName, string? Phone);

    public record UpdateUserRequest(
        string? Email, string? FirstName, string? LastName,
        string? Phone, bool? IsActive);
}
