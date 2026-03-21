using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Queries.GetCashRegisters;

public class GetCashRegistersHandler : IRequestHandler<GetCashRegistersQuery, IReadOnlyList<CashRegisterDto>>
{
    private readonly ICashRegisterRepository _repository;

    public GetCashRegistersHandler(ICashRegisterRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<CashRegisterDto>> Handle(GetCashRegistersQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var registers = await _repository.GetByTenantIdAsync(request.TenantId, cancellationToken);
        return registers.Select(r => new CashRegisterDto
        {
            Id = r.Id,
            Name = r.Name,
            CurrencyCode = r.CurrencyCode,
            Balance = r.Balance,
            IsDefault = r.IsDefault,
            IsActive = r.IsActive,
            TransactionCount = r.Transactions.Count
        }).ToList().AsReadOnly();
    }
}
