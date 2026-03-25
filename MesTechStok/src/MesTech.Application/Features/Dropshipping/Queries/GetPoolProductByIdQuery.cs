using MediatR;
using MesTech.Application.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Queries;

public record GetPoolProductByIdQuery(Guid PoolProductId) : IRequest<PoolProductDto?>;

public sealed class GetPoolProductByIdQueryHandler(
    IDropshippingPoolRepository poolRepo
) : IRequestHandler<GetPoolProductByIdQuery, PoolProductDto?>
{
    public async Task<PoolProductDto?> Handle(
        GetPoolProductByIdQuery req, CancellationToken cancellationToken)
    {
        var p = await poolRepo.GetPoolProductByIdAsync(req.PoolProductId, cancellationToken);
        if (p is null) return null;

        return new PoolProductDto(
            p.Id,
            p.ProductId,
            p.Product?.Name ?? string.Empty,
            p.Product?.SKU ?? string.Empty,
            p.Product?.Barcode,
            p.PoolPrice,
            p.IsActive,
            null,
            p.UpdatedAt
        );
    }
}
