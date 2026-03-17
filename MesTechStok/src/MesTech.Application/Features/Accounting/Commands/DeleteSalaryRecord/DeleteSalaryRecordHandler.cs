using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.DeleteSalaryRecord;

public class DeleteSalaryRecordHandler : IRequestHandler<DeleteSalaryRecordCommand>
{
    private readonly ISalaryRecordRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteSalaryRecordHandler(ISalaryRecordRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(DeleteSalaryRecordCommand request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"SalaryRecord {request.Id} not found.");

        // Soft delete — BaseEntity.IsDeleted
        record.IsDeleted = true;
        record.DeletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(record, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
