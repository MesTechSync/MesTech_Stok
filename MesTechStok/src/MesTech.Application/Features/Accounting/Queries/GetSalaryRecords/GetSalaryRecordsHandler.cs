using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetSalaryRecords;

public class GetSalaryRecordsHandler : IRequestHandler<GetSalaryRecordsQuery, IReadOnlyList<SalaryRecordDto>>
{
    private readonly ISalaryRecordRepository _repository;

    public GetSalaryRecordsHandler(ISalaryRecordRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<SalaryRecordDto>> Handle(GetSalaryRecordsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var records = await _repository.GetAllAsync(request.TenantId, request.Year, request.Month, cancellationToken);
        return records.Adapt<List<SalaryRecordDto>>().AsReadOnly();
    }
}
