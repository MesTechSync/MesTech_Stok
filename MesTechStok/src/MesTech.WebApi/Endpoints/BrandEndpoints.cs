using MediatR;
using MesTech.Application.Queries.GetBrandById;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.AspNetCore.OutputCaching;

namespace MesTech.WebApi.Endpoints;

/// <summary>
/// Marka yönetim endpoint'leri — full CRUD.
/// HH-DEV6-081 FIX: GetBrandByIdQuery handler mevcut, endpoint eksikti.
/// HH-DEV6-088 FIX: List/Create/Update/Delete endpoint'leri eklendi.
/// </summary>
public static class BrandEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/v1/brands")
            .WithTags("Brands")
            .RequireAuthorization()
            .RequireRateLimiting("PerApiKey");

        // GET /api/v1/brands — tüm markalar
        group.MapGet("/", async (
            IBrandRepository repo, CancellationToken ct) =>
        {
            var brands = await repo.GetAllAsync(ct);
            return Results.Ok(brands.Select(b => new BrandListItem(b.Id, b.Name, b.LogoUrl, b.IsActive)));
        })
        .WithName("GetBrands")
        .WithSummary("Tüm marka listesi — ad, logo, durum")
        .Produces<IEnumerable<BrandListItem>>(200)
        .CacheOutput("Lookup60s");

        // GET /api/v1/brands/{id} — marka detayı
        group.MapGet("/{id:guid}", async (
            Guid id,
            ISender mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBrandByIdQuery(id), ct);
            return result is not null
                ? Results.Ok(result)
                : Results.Problem(detail: $"Marka {id} bulunamadı.", statusCode: 404);
        })
        .WithName("GetBrandById")
        .WithSummary("Marka detayı — ad, logo, durum bilgisi")
        .Produces<GetBrandByIdResult>(200).Produces(404)
        .CacheOutput("Lookup60s");

        // POST /api/v1/brands — yeni marka oluştur
        group.MapPost("/", async (
            CreateBrandRequest request,
            IBrandRepository repo,
            IUnitOfWork uow,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest("Marka adı boş olamaz.");

            var existing = await repo.GetByNameAsync(request.Name, ct);
            if (existing is not null)
                return Results.Conflict($"'{request.Name}' adlı marka zaten mevcut.");

            var brand = Brand.Create(request.TenantId, request.Name, request.LogoUrl);
            await repo.AddAsync(brand, ct);
            await uow.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/brands/{brand.Id}",
                new BrandListItem(brand.Id, brand.Name, brand.LogoUrl, brand.IsActive));
        })
        .WithName("CreateBrand")
        .WithSummary("Yeni marka oluştur — ad + logo")
        .Produces<BrandListItem>(201).Produces(400).Produces(409);

        // PUT /api/v1/brands/{id} — marka güncelle
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateBrandRequest request,
            IBrandRepository repo,
            IUnitOfWork uow,
            CancellationToken ct) =>
        {
            var brand = await repo.GetByIdAsync(id, ct);
            if (brand is null)
                return Results.Problem(detail: $"Marka {id} bulunamadı.", statusCode: 404);

            if (!string.IsNullOrWhiteSpace(request.Name))
                brand.Rename(request.Name);
            if (request.LogoUrl is not null)
                brand.LogoUrl = request.LogoUrl;

            await repo.UpdateAsync(brand, ct);
            await uow.SaveChangesAsync(ct);

            return Results.Ok(new BrandListItem(brand.Id, brand.Name, brand.LogoUrl, brand.IsActive));
        })
        .WithName("UpdateBrand")
        .WithSummary("Marka güncelle — ad ve/veya logo değiştir")
        .Produces<BrandListItem>(200).Produces(404);

        // DELETE /api/v1/brands/{id} — marka pasife al (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IBrandRepository repo,
            IUnitOfWork uow,
            CancellationToken ct) =>
        {
            var brand = await repo.GetByIdAsync(id, ct);
            if (brand is null)
                return Results.Problem(detail: $"Marka {id} bulunamadı.", statusCode: 404);

            brand.Deactivate();
            await repo.UpdateAsync(brand, ct);
            await uow.SaveChangesAsync(ct);

            return Results.NoContent();
        })
        .WithName("DeleteBrand")
        .WithSummary("Marka pasife al — soft delete (IsActive = false)")
        .Produces(204).Produces(404);
    }

    // DTOs
    public record BrandListItem(Guid Id, string Name, string? LogoUrl, bool IsActive);
    public record CreateBrandRequest(Guid TenantId, string Name, string? LogoUrl);
    public record UpdateBrandRequest(string? Name, string? LogoUrl);
}
