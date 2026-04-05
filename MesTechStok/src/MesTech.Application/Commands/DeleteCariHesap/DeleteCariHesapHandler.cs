using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.DeleteCariHesap;

public sealed class DeleteCariHesapHandler : IRequestHandler<DeleteCariHesapCommand, DeleteCariHesapResult>
{
    private readonly ICariHesapRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteCariHesapHandler(ICariHesapRepository repository, IUnitOfWork uow)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<DeleteCariHesapResult> Handle(DeleteCariHesapCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new DeleteCariHesapResult { IsSuccess = false, ErrorMessage = $"CariHesap {request.Id} bulunamadı." };

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DeleteCariHesapResult { IsSuccess = true };
    }
}
