using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IProductReviewRepository
{
    Task<ProductReview?> GetByExternalIdAsync(Guid tenantId, string externalReviewId, PlatformType platform, CancellationToken ct = default);
    Task<IReadOnlyList<ProductReview>> GetByProductAsync(Guid productId, CancellationToken ct = default);
    Task AddAsync(ProductReview review, CancellationToken ct = default);
    Task UpdateAsync(ProductReview review, CancellationToken ct = default);
}
