using Mapster;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.SearchProductsForImageMatch;

public class SearchProductsForImageMatchHandler
    : IRequestHandler<SearchProductsForImageMatchQuery, IReadOnlyList<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public SearchProductsForImageMatchHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
    }

    public async Task<IReadOnlyList<ProductDto>> Handle(
        SearchProductsForImageMatchQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync().ConfigureAwait(false);
        return products.Select(p =>
        {
            var dto = p.Adapt<ProductDto>();
            dto.ProfitMargin = p.ProfitMargin;
            dto.TotalValue = p.TotalValue;
            dto.NeedsReorder = p.NeedsReorder();
            dto.StockStatus = p.IsOutOfStock() ? "OutOfStock" :
                              p.IsLowStock() ? "Low" :
                              p.IsOverStock() ? "OverStock" : "Normal";
            return dto;
        }).ToList();
    }
}
