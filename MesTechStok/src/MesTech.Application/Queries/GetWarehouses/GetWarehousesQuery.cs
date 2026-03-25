using MediatR;

namespace MesTech.Application.Queries.GetWarehouses;

public record GetWarehousesQuery(bool ActiveOnly = true) : IRequest<IReadOnlyList<WarehouseListDto>>;

public sealed class WarehouseListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public bool HasClimateControl { get; set; }
}
