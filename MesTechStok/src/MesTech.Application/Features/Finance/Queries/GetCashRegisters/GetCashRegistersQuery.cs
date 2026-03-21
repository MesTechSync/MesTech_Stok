using MediatR;

namespace MesTech.Application.Features.Finance.Queries.GetCashRegisters;

public record GetCashRegistersQuery(Guid TenantId) : IRequest<IReadOnlyList<CashRegisterDto>>;

public record CashRegisterDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = "TRY";
    public decimal Balance { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public int TransactionCount { get; init; }
}
