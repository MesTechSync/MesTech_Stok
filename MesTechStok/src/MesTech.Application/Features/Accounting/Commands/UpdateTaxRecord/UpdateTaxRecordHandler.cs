using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.UpdateTaxRecord;

public sealed class UpdateTaxRecordHandler : IRequestHandler<UpdateTaxRecordCommand>
{
    private readonly ITaxRecordRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateTaxRecordHandler(ITaxRecordRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(UpdateTaxRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"TaxRecord {request.Id} not found.");

        if (request.MarkAsPaid)
            record.MarkAsPaid();

        await _repository.UpdateAsync(record, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
