using MediatR;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.UpdateProduct;

public sealed class UpdateProductHandler : IRequestHandler<UpdateProductCommand, UpdateProductResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdateProductResult> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _productRepository.GetByIdAsync(request.ProductId).ConfigureAwait(false);
        if (product == null)
            return new UpdateProductResult { IsSuccess = false, ErrorMessage = $"Product {request.ProductId} not found." };

        if (request.Name != null) product.Name = request.Name;
        if (request.Description != null) product.Description = request.Description;
        if (request.PurchasePrice.HasValue) product.PurchasePrice = request.PurchasePrice.Value;
        if (request.SalePrice.HasValue) product.UpdatePrice(request.SalePrice.Value);
        if (request.ListPrice.HasValue) product.ListPrice = request.ListPrice.Value;
        if (request.TaxRate.HasValue) product.TaxRate = request.TaxRate.Value;
        if (request.CategoryId.HasValue) product.CategoryId = request.CategoryId.Value;
        if (request.SupplierId.HasValue) product.SupplierId = request.SupplierId.Value;
        if (request.WarehouseId.HasValue) product.WarehouseId = request.WarehouseId.Value;
        if (request.MinimumStock.HasValue) product.MinimumStock = request.MinimumStock.Value;
        if (request.MaximumStock.HasValue) product.MaximumStock = request.MaximumStock.Value;
        if (request.Brand != null) product.Brand = request.Brand;
        if (request.BrandId.HasValue) product.BrandId = request.BrandId.Value;
        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value) product.Activate();
            else product.Deactivate();
        }

        product.MarkAsUpdated();
        await _productRepository.UpdateAsync(product).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new UpdateProductResult { IsSuccess = true };
    }
}
