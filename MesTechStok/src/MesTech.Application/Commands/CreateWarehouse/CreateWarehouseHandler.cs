using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateWarehouse;

public sealed class CreateWarehouseHandler : IRequestHandler<CreateWarehouseCommand, CreateWarehouseResult>
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWarehouseHandler(
        IWarehouseRepository warehouseRepository,
        IUnitOfWork unitOfWork)
    {
        _warehouseRepository = warehouseRepository ?? throw new ArgumentNullException(nameof(warehouseRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CreateWarehouseResult> Handle(CreateWarehouseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name))
            return new CreateWarehouseResult { IsSuccess = false, ErrorMessage = "Depo adı boş olamaz." };

        if (string.IsNullOrWhiteSpace(request.Code))
            return new CreateWarehouseResult { IsSuccess = false, ErrorMessage = "Depo kodu boş olamaz." };

        var warehouse = new Warehouse
        {
            TenantId = request.TenantId,
            Name = request.Name,
            Code = request.Code,
            Address = request.Address,
            City = request.City,
            IsActive = true
        };

        if (request.IsDefault)
            warehouse.SetAsDefault();

        await _warehouseRepository.AddAsync(warehouse, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new CreateWarehouseResult
        {
            IsSuccess = true,
            WarehouseId = warehouse.Id
        };
    }
}
