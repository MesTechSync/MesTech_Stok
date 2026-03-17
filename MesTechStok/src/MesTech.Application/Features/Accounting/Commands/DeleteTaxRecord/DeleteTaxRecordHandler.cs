using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.DeleteTaxRecord;

public class DeleteTaxRecordHandler : IRequestHandler<DeleteTaxRecordCommand>
{
    private readonly ITaxRecordRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteTaxRecordHandler(ITaxRecordRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(DeleteTaxRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"TaxRecord {request.Id} not found.");

        record.IsDeleted = true;
        record.DeletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(record, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
