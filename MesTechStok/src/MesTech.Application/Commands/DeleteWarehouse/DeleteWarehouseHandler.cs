using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.DeleteWarehouse;

public sealed class DeleteWarehouseHandler : IRequestHandler<DeleteWarehouseCommand, bool>
{
    private readonly IWarehouseRepository _repo;
    private readonly IUnitOfWork _uow;
    public DeleteWarehouseHandler(IWarehouseRepository repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<bool> Handle(DeleteWarehouseCommand request, CancellationToken cancellationToken)
    {
        var wh = await _repo.GetByIdAsync(request.WarehouseId).ConfigureAwait(false);
        if (wh is null || wh.TenantId != request.TenantId) return false;
        await _repo.DeleteAsync(request.WarehouseId).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
