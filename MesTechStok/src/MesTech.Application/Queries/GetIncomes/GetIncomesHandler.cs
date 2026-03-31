using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetIncomes;

public sealed class GetIncomesHandler : IRequestHandler<GetIncomesQuery, IReadOnlyList<IncomeDto>>
{
    private readonly IIncomeRepository _incomeRepository;

    public GetIncomesHandler(IIncomeRepository incomeRepository)
    {
        _incomeRepository = incomeRepository;
    }

    public async Task<IReadOnlyList<IncomeDto>> Handle(GetIncomesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Type.HasValue)
        {
            var byType = await _incomeRepository.GetByTypeAsync(request.Type.Value, request.TenantId).ConfigureAwait(false);
            return byType.Adapt<List<IncomeDto>>().AsReadOnly();
        }

        if (request.From.HasValue && request.To.HasValue)
        {
            var byRange = await _incomeRepository.GetByDateRangeAsync(request.From.Value, request.To.Value, request.TenantId).ConfigureAwait(false);
            return byRange.Adapt<List<IncomeDto>>().AsReadOnly();
        }

        var all = await _incomeRepository.GetAllAsync(request.TenantId).ConfigureAwait(false);
        return all.Adapt<List<IncomeDto>>().AsReadOnly();
    }
}
