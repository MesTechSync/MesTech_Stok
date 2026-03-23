using MediatR;

namespace MesTech.Application.Commands.DeleteWarehouse;

public record DeleteWarehouseCommand(Guid TenantId, Guid WarehouseId) : IRequest<bool>;
