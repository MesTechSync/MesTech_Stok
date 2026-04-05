using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.DeleteQuotation;

public sealed class DeleteQuotationHandler : IRequestHandler<DeleteQuotationCommand, DeleteQuotationResult>
{
    private readonly IQuotationRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteQuotationHandler(IQuotationRepository repository, IUnitOfWork uow)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
    }

    public async Task<DeleteQuotationResult> Handle(DeleteQuotationCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var entity = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return new DeleteQuotationResult(false, $"Teklif {request.Id} bulunamadı.");

        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new DeleteQuotationResult(true);
    }
}
