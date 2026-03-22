using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateProduct;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, CreateProductResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Duplicate SKU kontrolü
        var existing = await _productRepository.GetBySKUAsync(request.SKU);
        if (existing != null)
            return new CreateProductResult { IsSuccess = false, ErrorMessage = $"SKU '{request.SKU}' already exists." };

        var product = new Product
        {
            Name = request.Name,
            SKU = request.SKU,
            Barcode = request.Barcode,
            PurchasePrice = request.PurchasePrice,
            SalePrice = request.SalePrice,
            CategoryId = request.CategoryId,
            SupplierId = request.SupplierId,
            WarehouseId = request.WarehouseId,
            Description = request.Description,
            MinimumStock = request.MinimumStock,
            MaximumStock = request.MaximumStock,
            TaxRate = request.TaxRate,
            Brand = request.Brand,
            IsActive = true,
        };

        product.MarkAsCreated();
        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateProductResult
        {
            IsSuccess = true,
            ProductId = product.Id
        };
    }
}
