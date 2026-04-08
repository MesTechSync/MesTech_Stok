using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class ProductReviewRepository : IProductReviewRepository
{
    private readonly AppDbContext _context;
    public ProductReviewRepository(AppDbContext context) => _context = context;

    public async Task<ProductReview?> GetByExternalIdAsync(
        Guid tenantId, string externalReviewId, PlatformType platform, CancellationToken ct = default)
        => await _context.Set<ProductReview>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantId == tenantId
                                   && r.ExternalReviewId == externalReviewId
                                   && r.Platform == platform, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ProductReview>> GetByProductAsync(
        Guid productId, CancellationToken ct = default)
        => await _context.Set<ProductReview>()
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.ReviewDate)
            .Take(1000)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(ProductReview review, CancellationToken ct = default)
        => await _context.Set<ProductReview>().AddAsync(review, ct).ConfigureAwait(false);

    public Task UpdateAsync(ProductReview review, CancellationToken ct = default)
    {
        _context.Set<ProductReview>().Update(review);
        return Task.CompletedTask;
    }
}
