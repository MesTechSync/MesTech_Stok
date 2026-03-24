using MediatR;
using MesTech.Application.Queries.GetWarehouses;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetWarehouseById;

public class GetWarehouseByIdHandler : IRequestHandler<GetWarehouseByIdQuery, WarehouseListDto?>
{
    private readonly IWarehouseRepository _repo;
    public GetWarehouseByIdHandler(IWarehouseRepository repo) => _repo = repo;

    public async Task<WarehouseListDto?> Handle(GetWarehouseByIdQuery request, CancellationToken cancellationToken)
    {
        var w = await _repo.GetByIdAsync(request.WarehouseId);
        if (w is null) return null;

        return new WarehouseListDto
        {
            Id = w.Id,
            Name = w.Name,
            Code = w.Code,
            Description = w.Description,
            Type = w.Type,
            City = w.City,
            Address = w.Address,
            IsActive = w.IsActive,
            IsDefault = w.IsDefault,
            HasClimateControl = w.HasClimateControl
        };
    }
}
