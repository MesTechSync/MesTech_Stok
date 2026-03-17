using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateTaxRecord;

public class CreateTaxRecordHandler : IRequestHandler<CreateTaxRecordCommand, Guid>
{
    private readonly ITaxRecordRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateTaxRecordHandler(ITaxRecordRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateTaxRecordCommand request, CancellationToken cancellationToken)
    {
        var record = TaxRecord.Create(
            request.TenantId,
            request.Period,
            request.TaxType,
            request.TaxableAmount,
            request.TaxAmount,
            request.DueDate);

        await _repository.AddAsync(record, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return record.Id;
    }
}
