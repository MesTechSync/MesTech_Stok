using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetPenaltyRecords;

public class GetPenaltyRecordsHandler : IRequestHandler<GetPenaltyRecordsQuery, IReadOnlyList<PenaltyRecordDto>>
{
    private readonly IPenaltyRecordRepository _repository;

    public GetPenaltyRecordsHandler(IPenaltyRecordRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<PenaltyRecordDto>> Handle(GetPenaltyRecordsQuery request, CancellationToken cancellationToken)
    {
        var records = await _repository.GetAllAsync(request.TenantId, request.Source, cancellationToken);
        return records.Adapt<List<PenaltyRecordDto>>().AsReadOnly();
    }
}
