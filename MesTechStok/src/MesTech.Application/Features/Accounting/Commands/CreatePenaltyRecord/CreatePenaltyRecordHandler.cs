using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreatePenaltyRecord;

public class CreatePenaltyRecordHandler : IRequestHandler<CreatePenaltyRecordCommand, Guid>
{
    private readonly IPenaltyRecordRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreatePenaltyRecordHandler(IPenaltyRecordRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreatePenaltyRecordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var record = PenaltyRecord.Create(
            request.TenantId,
            request.Source,
            request.Description,
            request.Amount,
            request.PenaltyDate,
            request.DueDate,
            request.ReferenceNumber,
            request.RelatedOrderId,
            request.Currency,
            request.Notes);

        await _repository.AddAsync(record, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return record.Id;
    }
}
