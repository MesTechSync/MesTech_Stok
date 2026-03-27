using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Finance.Queries.GetBankAccounts;

public record GetBankAccountsQuery(Guid TenantId) : IRequest<IReadOnlyList<BankAccountDto>>, ICacheableQuery
{
    public string CacheKey => $"BankAccounts_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public sealed class BankAccountDto
{
    public Guid Id { get; init; }
    public string BankName { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string? IBAN { get; init; }
    public string CurrencyCode { get; init; } = "TRY";
    public decimal Balance { get; init; }
    public bool IsActive { get; init; }
}
