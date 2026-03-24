using MediatR;
using MesTech.Application.Queries.GetWarehouses;

namespace MesTech.Application.Queries.GetWarehouseById;

public record GetWarehouseByIdQuery(Guid WarehouseId) : IRequest<WarehouseListDto?>;
