using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Common;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Dropshipping.Queries;

public record GetDropshippingPoolsQuery(
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<DropshippingPoolDto>>;

public record DropshippingPoolDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsPublic,
    bool IsActive,
    string PricingStrategy,
    int ProductCount,
    DateTime CreatedAt
);

public sealed class GetDropshippingPoolsQueryHandler(
    IDropshippingPoolRepository poolRepo,
    ITenantProvider tenantProvider
) : IRequestHandler<GetDropshippingPoolsQuery, PagedResult<DropshippingPoolDto>>
{
    public async Task<PagedResult<DropshippingPoolDto>> Handle(
        GetDropshippingPoolsQuery req, CancellationToken cancellationToken)
    {
        var (items, total) = await poolRepo.GetPoolsPagedAsync(
            tenantId: tenantProvider.GetCurrentTenantId(),
            isActive: req.IsActive,
            page: req.Page,
            pageSize: req.PageSize,
            ct: cancellationToken);

        var dtos = items.Select(p => new DropshippingPoolDto(
            p.Id,
            p.Name,
            p.Description,
            p.IsPublic,
            p.IsActive,
            p.PricingStrategy.ToString(),
            p.Products.Count,
            p.CreatedAt
        )).ToList();

        return PagedResult<DropshippingPoolDto>.Create(
            (IReadOnlyList<DropshippingPoolDto>)dtos, total, req.Page, req.PageSize);
    }
}
