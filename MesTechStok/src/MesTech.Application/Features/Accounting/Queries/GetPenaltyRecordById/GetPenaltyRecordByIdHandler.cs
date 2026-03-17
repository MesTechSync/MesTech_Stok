using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetPenaltyRecordById;

public class GetPenaltyRecordByIdHandler : IRequestHandler<GetPenaltyRecordByIdQuery, PenaltyRecordDto?>
{
    private readonly IPenaltyRecordRepository _repository;

    public GetPenaltyRecordByIdHandler(IPenaltyRecordRepository repository)
        => _repository = repository;

    public async Task<PenaltyRecordDto?> Handle(GetPenaltyRecordByIdQuery request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return record?.Adapt<PenaltyRecordDto>();
    }
}
