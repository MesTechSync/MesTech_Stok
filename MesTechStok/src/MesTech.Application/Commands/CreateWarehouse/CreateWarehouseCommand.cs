using MediatR;

namespace MesTech.Application.Commands.CreateWarehouse;

public record CreateWarehouseCommand(
    string Name,
    string Code,
    string? Address = null,
    string? City = null,
    bool IsDefault = false,
    Guid TenantId = default
) : IRequest<CreateWarehouseResult>;

public class CreateWarehouseResult
{
    public bool IsSuccess { get; set; }
    public Guid WarehouseId { get; set; }
    public string? ErrorMessage { get; set; }
}
