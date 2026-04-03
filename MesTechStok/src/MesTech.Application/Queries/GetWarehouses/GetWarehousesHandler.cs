using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetWarehouses;

public sealed class GetWarehousesHandler : IRequestHandler<GetWarehousesQuery, IReadOnlyList<WarehouseListDto>>
{
    private readonly IWarehouseRepository _warehouseRepository;

    public GetWarehousesHandler(IWarehouseRepository warehouseRepository)
    {
        _warehouseRepository = warehouseRepository;
    }

    public async Task<IReadOnlyList<WarehouseListDto>> Handle(GetWarehousesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var warehouses = await _warehouseRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        if (request.ActiveOnly)
        {
            warehouses = warehouses.Where(w => w.IsActive).ToList();
        }

        return warehouses.Select(w => new WarehouseListDto
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
            HasClimateControl = w.HasClimateControl,
        }).ToList();
    }
}
