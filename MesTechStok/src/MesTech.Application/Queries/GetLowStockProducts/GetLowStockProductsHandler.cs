using Mapster;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetLowStockProducts;

public sealed class GetLowStockProductsHandler : IRequestHandler<GetLowStockProductsQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetLowStockProductsHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var products = await _productRepository.GetLowStockAsync(cancellationToken).ConfigureAwait(false);
        return products.Adapt<List<ProductDto>>().AsReadOnly();
    }
}
