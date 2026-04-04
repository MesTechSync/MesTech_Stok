using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetKarZarar;

public sealed class GetKarZararHandler : IRequestHandler<GetKarZararQuery, KarZararDto>
{
    private readonly IIncomeRepository _incomeRepository;
    private readonly IExpenseRepository _expenseRepository;

    public GetKarZararHandler(IIncomeRepository incomeRepository, IExpenseRepository expenseRepository)
    {
        _incomeRepository = incomeRepository;
        _expenseRepository = expenseRepository;
    }

    public async Task<KarZararDto> Handle(GetKarZararQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var incomes = await _incomeRepository.GetByDateRangeAsync(request.From, request.To, request.TenantId, cancellationToken).ConfigureAwait(false);
        var expenses = await _expenseRepository.GetByDateRangeAsync(request.From, request.To, request.TenantId, cancellationToken).ConfigureAwait(false);

        var toplamGelir = incomes.Sum(i => i.Amount);
        var toplamGider = expenses.Sum(e => e.Amount);
        var netKar = toplamGelir - toplamGider;

        return new KarZararDto
        {
            ToplamGelir = toplamGelir,
            ToplamGider = toplamGider,
            NetKar = netKar,
            DönemBasi = request.From,
            DönemSonu = request.To,
        };
    }
}
