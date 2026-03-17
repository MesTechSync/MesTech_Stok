using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetTaxRecordById;

public class GetTaxRecordByIdHandler : IRequestHandler<GetTaxRecordByIdQuery, TaxRecordDto?>
{
    private readonly ITaxRecordRepository _repository;

    public GetTaxRecordByIdHandler(ITaxRecordRepository repository)
        => _repository = repository;

    public async Task<TaxRecordDto?> Handle(GetTaxRecordByIdQuery request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.Id, cancellationToken);
        return record?.Adapt<TaxRecordDto>();
    }
}
