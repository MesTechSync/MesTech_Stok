using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Queries;

public record GetPoolProductsQuery(
    Guid? PoolId = null,
    ReliabilityColor? ColorFilter = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<PoolProductDto>>;

public record PoolProductDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string Sku,
    string? Barcode,
    decimal PoolPrice,
    bool IsActive,
    string? SupplierInfo,
    DateTime LastUpdated
);

public sealed class GetPoolProductsQueryHandler(
    IDropshippingPoolRepository poolRepo,
    ITenantProvider tenantProvider
) : IRequestHandler<GetPoolProductsQuery, PagedResult<PoolProductDto>>
{
    public async Task<PagedResult<PoolProductDto>> Handle(
        GetPoolProductsQuery req, CancellationToken cancellationToken)
    {
        var (items, total) = await poolRepo.GetProductsPagedAsync(
            tenantId: tenantProvider.GetCurrentTenantId(),
            poolId: req.PoolId,
            colorFilter: req.ColorFilter,
            search: req.Search,
            page: req.Page,
            pageSize: req.PageSize,
            ct: cancellationToken);

        var dtos = items.Select(p => new PoolProductDto(
            p.Id,
            p.ProductId,
            p.Product?.Name ?? string.Empty,
            p.Product?.SKU ?? string.Empty,
            p.Product?.Barcode,
            p.PoolPrice,
            p.IsActive,
            null, // Supplier info ayrı join gerektirir
            p.UpdatedAt
        )).ToList();

        return PagedResult<PoolProductDto>.Create(
            (IReadOnlyList<PoolProductDto>)dtos, total, req.Page, req.PageSize);
    }
}
