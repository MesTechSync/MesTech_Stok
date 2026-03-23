using MediatR;

namespace MesTech.Application.Commands.UpdateWarehouse;

public record UpdateWarehouseCommand(Guid TenantId, Guid WarehouseId, string Name, string Code, string? Description, string Type, bool IsActive) : IRequest<bool>;
