using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Product.Queries.GetProductVariants;

public sealed class GetProductVariantsHandler : IRequestHandler<GetProductVariantsQuery, ProductVariantMatrixDto>
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<GetProductVariantsHandler> _logger;

    public GetProductVariantsHandler(IProductRepository productRepository, ILogger<GetProductVariantsHandler> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<ProductVariantMatrixDto> Handle(GetProductVariantsQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false);
        if (product is null || product.TenantId != request.TenantId)
        {
            _logger.LogWarning("Product {ProductId} not found or tenant mismatch", request.ProductId);
            return new ProductVariantMatrixDto { ProductId = request.ProductId };
        }

        var variants = product.Variants?.Select(v => new VariantRowDto
        {
            VariantId = v.Id,
            SKU = v.SKU,
            Barcode = v.VariantBarcode,
            Color = v.Color,
            Size = v.Size,
            Stock = v.Stock,
            SalePrice = v.Price ?? v.PriceOverride ?? 0m,
            PurchasePrice = null,
            IsActive = v.IsActive,
            Attributes = v.Attributes
        }).ToList() ?? [];

        return new ProductVariantMatrixDto
        {
            ProductId = product.Id,
            ProductName = product.Name,
            Variants = variants,
            TotalStock = variants.Sum(v => v.Stock)
        };
    }
}
