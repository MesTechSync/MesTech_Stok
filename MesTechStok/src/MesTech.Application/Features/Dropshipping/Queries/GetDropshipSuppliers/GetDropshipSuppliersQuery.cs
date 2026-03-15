using MediatR;
using MesTech.Application.DTOs.Dropshipping;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipSuppliers;

public record GetDropshipSuppliersQuery(Guid TenantId)
    : IRequest<IReadOnlyList<DropshipSupplierDto>>;
