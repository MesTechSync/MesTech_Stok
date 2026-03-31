using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.UpdateWarehouse;

public sealed class UpdateWarehouseHandler : IRequestHandler<UpdateWarehouseCommand, bool>
{
    private readonly IWarehouseRepository _repo;
    private readonly IUnitOfWork _uow;
    public UpdateWarehouseHandler(IWarehouseRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<bool> Handle(UpdateWarehouseCommand request, CancellationToken cancellationToken)
    {
        var wh = await _repo.GetByIdAsync(request.WarehouseId).ConfigureAwait(false);
        if (wh is null || wh.TenantId != request.TenantId) return false;
        wh.Name = request.Name; wh.Code = request.Code; wh.Description = request.Description;
        wh.Type = request.Type; wh.IsActive = request.IsActive; wh.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(wh).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
