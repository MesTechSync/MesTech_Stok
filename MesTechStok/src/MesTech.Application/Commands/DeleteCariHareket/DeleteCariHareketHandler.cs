using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.DeleteCariHareket;

public sealed class DeleteCariHareketHandler : IRequestHandler<DeleteCariHareketCommand, DeleteCariHareketResult>
{
    private readonly ICariHareketRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteCariHareketHandler(ICariHareketRepository repository, IUnitOfWork uow)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<DeleteCariHareketResult> Handle(DeleteCariHareketCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new DeleteCariHareketResult(false, $"Cari hareket {request.Id} bulunamadı.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DeleteCariHareketResult(true);
    }
}
