using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.RecordCommission;

public class RecordCommissionHandler : IRequestHandler<RecordCommissionCommand, Guid>
{
    private readonly ICommissionRecordRepository _repository;
    private readonly IUnitOfWork _uow;

    public RecordCommissionHandler(ICommissionRecordRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(RecordCommissionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var record = CommissionRecord.Create(
            request.TenantId, request.Platform, request.GrossAmount,
            request.CommissionRate, request.CommissionAmount, request.ServiceFee,
            request.OrderId, request.Category);

        await _repository.AddAsync(record, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return record.Id;
    }
}
