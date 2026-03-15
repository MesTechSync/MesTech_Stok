using MediatR;
using MesTech.Application.DTOs.Dropshipping;
using MesTech.Application.Interfaces.Dropshipping;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipProducts;

public class GetDropshipProductsHandler : IRequestHandler<GetDropshipProductsQuery, IReadOnlyList<DropshipProductDto>>
{
    private readonly IDropshipProductRepository _repository;

    public GetDropshipProductsHandler(IDropshipProductRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<DropshipProductDto>> Handle(GetDropshipProductsQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.GetByTenantAsync(request.TenantId, request.IsLinked, cancellationToken);
        return items.Select(p => new DropshipProductDto
        {
            Id = p.Id,
            DropshipSupplierId = p.DropshipSupplierId,
            ExternalProductId = p.ExternalProductId,
            ExternalUrl = p.ExternalUrl,
            Title = p.Title,
            OriginalPrice = p.OriginalPrice,
            SellingPrice = p.SellingPrice,
            StockQuantity = p.StockQuantity,
            ProductId = p.ProductId,
            IsLinked = p.IsLinked,
            LastSyncAt = p.LastSyncAt
        }).ToList().AsReadOnly();
    }
}
