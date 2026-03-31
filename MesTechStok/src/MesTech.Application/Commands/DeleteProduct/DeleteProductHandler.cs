using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.DeleteProduct;

public sealed class DeleteProductHandler : IRequestHandler<DeleteProductCommand, DeleteProductResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<DeleteProductResult> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var product = await _productRepository.GetByIdAsync(request.ProductId).ConfigureAwait(false);
        if (product == null)
        {
            return new DeleteProductResult { IsSuccess = false, ErrorMessage = $"Product {request.ProductId} not found." };
        }

        // G349: FK dependency check — child record varsa silme
        if (product.OrderItems.Count > 0)
            return new DeleteProductResult { IsSuccess = false, ErrorMessage = $"Product has {product.OrderItems.Count} order items — cannot delete a product with order history." };
        if (product.PlatformMappings.Count > 0)
            return new DeleteProductResult { IsSuccess = false, ErrorMessage = $"Product is mapped to {product.PlatformMappings.Count} platforms — remove mappings first." };

        await _productRepository.DeleteAsync(request.ProductId).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DeleteProductResult { IsSuccess = true };
    }
}
