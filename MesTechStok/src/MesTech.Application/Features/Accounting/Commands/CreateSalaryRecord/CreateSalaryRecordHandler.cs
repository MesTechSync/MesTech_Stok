using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateSalaryRecord;

public sealed class CreateSalaryRecordHandler : IRequestHandler<CreateSalaryRecordCommand, Guid>
{
    private readonly ISalaryRecordRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateSalaryRecordHandler(ISalaryRecordRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateSalaryRecordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var record = SalaryRecord.Create(
            request.TenantId,
            request.EmployeeName,
            request.GrossSalary,
            request.SGKEmployer,
            request.SGKEmployee,
            request.IncomeTax,
            request.StampTax,
            request.Year,
            request.Month,
            request.EmployeeId,
            request.Notes);

        await _repository.AddAsync(record, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return record.Id;
    }
}
