using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.UpdateProductImage;

public sealed class UpdateProductImageHandler
    : IRequestHandler<UpdateProductImageCommand, UpdateProductImageResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductImageHandler(
        IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdateProductImageResult> Handle(
        UpdateProductImageCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product == null)
            return new UpdateProductImageResult
            {
                IsSuccess = false,
                ErrorMessage = $"Product {request.ProductId} not found."
            };

        product.ImageUrl = request.ImageUrl;
        await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new UpdateProductImageResult { IsSuccess = true };
    }
}
