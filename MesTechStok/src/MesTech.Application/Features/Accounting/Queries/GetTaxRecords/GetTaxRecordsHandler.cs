using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetTaxRecords;

public class GetTaxRecordsHandler : IRequestHandler<GetTaxRecordsQuery, IReadOnlyList<TaxRecordDto>>
{
    private readonly ITaxRecordRepository _repository;

    public GetTaxRecordsHandler(ITaxRecordRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<TaxRecordDto>> Handle(GetTaxRecordsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var records = await _repository.GetAllAsync(request.TenantId, request.TaxType, request.Year, cancellationToken);
        return records.Adapt<List<TaxRecordDto>>().AsReadOnly();
    }
}
