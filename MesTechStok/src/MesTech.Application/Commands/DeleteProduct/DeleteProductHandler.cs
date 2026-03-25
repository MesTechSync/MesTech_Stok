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
        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            return new DeleteProductResult { IsSuccess = false, ErrorMessage = $"Product {request.ProductId} not found." };
        }

        await _productRepository.DeleteAsync(request.ProductId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteProductResult { IsSuccess = true };
    }
}
