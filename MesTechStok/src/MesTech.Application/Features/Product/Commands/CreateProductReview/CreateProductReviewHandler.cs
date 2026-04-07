using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Product.Commands.CreateProductReview;

public sealed class CreateProductReviewHandler : IRequestHandler<CreateProductReviewCommand, Guid>
{
    private readonly IProductReviewRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateProductReviewHandler(IProductReviewRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateProductReviewCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Idempotent check — skip if already imported
        var existing = await _repository.GetByExternalIdAsync(
            request.TenantId, request.ExternalReviewId, request.Platform, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
            return existing.Id;

        var review = ProductReview.Create(
            request.TenantId,
            request.ProductId,
            request.Platform,
            request.ExternalReviewId,
            request.CustomerName,
            request.Comment,
            request.Rating,
            request.ReviewDate,
            request.IsReplied);

        await _repository.AddAsync(review, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return review.Id;
    }
}
