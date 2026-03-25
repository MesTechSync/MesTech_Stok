using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.UpdatePenaltyRecord;

public sealed class UpdatePenaltyRecordHandler : IRequestHandler<UpdatePenaltyRecordCommand>
{
    private readonly IPenaltyRecordRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdatePenaltyRecordHandler(IPenaltyRecordRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(UpdatePenaltyRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"PenaltyRecord {request.Id} not found.");

        if (request.PaymentStatus == PaymentStatus.Completed)
            record.MarkAsPaid();
        else
            record.UpdatePaymentStatus(request.PaymentStatus);

        await _repository.UpdateAsync(record, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
