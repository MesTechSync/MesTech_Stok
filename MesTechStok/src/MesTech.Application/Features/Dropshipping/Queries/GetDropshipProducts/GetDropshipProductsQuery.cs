using MediatR;
using MesTech.Application.DTOs.Dropshipping;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipProducts;

/// <summary>
/// Tenant'a ait dropship ürünlerini listeler. IsLinked filtresiyle linked/unlinked ayrımı yapılabilir.
/// </summary>
public record GetDropshipProductsQuery(Guid TenantId, bool? IsLinked = null)
    : IRequest<IReadOnlyList<DropshipProductDto>>;
