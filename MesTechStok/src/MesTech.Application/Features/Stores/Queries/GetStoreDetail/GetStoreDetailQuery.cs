using MediatR;

namespace MesTech.Application.Features.Stores.Queries.GetStoreDetail;

public record GetStoreDetailQuery(Guid TenantId, Guid StoreId) : IRequest<StoreDetailDto?>;
