using MesTech.Application.DTOs;
using MesTech.Domain.Common;
using MediatR;

namespace MesTech.Application.Features.Product.Queries.GetPlatformProducts;

/// <summary>
/// Generic platform product query — pulls products from any marketplace adapter.
/// Used by Etsy/Shopify/WooCommerce/Zalando/Ozon/PttAvm Avalonia ViewModels.
/// G404: Replaces Task.Delay stub with real adapter data.
/// </summary>
public record GetPlatformProductsQuery(
    string PlatformCode,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<ProductDto>>;
