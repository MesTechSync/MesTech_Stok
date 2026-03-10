using Mapster;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetProductByBarcode;

public class GetProductByBarcodeHandler : IRequestHandler<GetProductByBarcodeQuery, ProductDto?>
{
    private readonly IProductRepository _productRepository;

    public GetProductByBarcodeHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }

    public async Task<ProductDto?> Handle(
        GetProductByBarcodeQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await _productRepository.GetByBarcodeAsync(request.Barcode).ConfigureAwait(false);
        if (product == null) return null;

        var dto = product.Adapt<ProductDto>();
        dto.ProfitMargin = product.ProfitMargin;
        dto.TotalValue = product.TotalValue;
        dto.NeedsReorder = product.NeedsReorder();
        dto.StockStatus = product.IsOutOfStock() ? "OutOfStock" :
                          product.IsLowStock() ? "Low" :
                          product.IsOverStock() ? "OverStock" : "Normal";
        return dto;
    }
}
