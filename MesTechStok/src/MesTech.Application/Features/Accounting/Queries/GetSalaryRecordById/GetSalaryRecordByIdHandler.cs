using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetSalaryRecordById;

public sealed class GetSalaryRecordByIdHandler : IRequestHandler<GetSalaryRecordByIdQuery, SalaryRecordDto?>
{
    private readonly ISalaryRecordRepository _repository;

    public GetSalaryRecordByIdHandler(ISalaryRecordRepository repository)
        => _repository = repository;

    public async Task<SalaryRecordDto?> Handle(GetSalaryRecordByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return record?.Adapt<SalaryRecordDto>();
    }
}
