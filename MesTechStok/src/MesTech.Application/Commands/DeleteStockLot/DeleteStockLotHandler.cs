using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.DeleteStockLot;

public sealed class DeleteStockLotHandler : IRequestHandler<DeleteStockLotCommand, DeleteStockLotResult>
{
    private readonly IStockLotRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteStockLotHandler(IStockLotRepository repository, IUnitOfWork uow)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<DeleteStockLotResult> Handle(DeleteStockLotCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new DeleteStockLotResult(false, $"Stok lot {request.Id} bulunamadı.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DeleteStockLotResult(true);
    }
}
