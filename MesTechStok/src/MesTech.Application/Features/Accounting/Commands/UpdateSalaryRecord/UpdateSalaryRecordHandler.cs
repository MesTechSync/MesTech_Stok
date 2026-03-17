using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.UpdateSalaryRecord;

public class UpdateSalaryRecordHandler : IRequestHandler<UpdateSalaryRecordCommand>
{
    private readonly ISalaryRecordRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateSalaryRecordHandler(ISalaryRecordRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(UpdateSalaryRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"SalaryRecord {request.Id} not found.");

        if (request.PaymentStatus == PaymentStatus.Completed)
            record.MarkAsPaid(request.PaidDate);
        else
            record.UpdatePaymentStatus(request.PaymentStatus);

        await _repository.UpdateAsync(record, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
