using MediatR;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Crm.Queries.GetCustomerPoints;

public record GetCustomerPointsQuery(
    Guid TenantId,
    Guid CustomerId
) : IRequest<GetCustomerPointsResult>;

public sealed class GetCustomerPointsResult
{
    public int TotalEarned { get; set; }
    public int TotalRedeemed { get; set; }
    public int TotalExpired { get; set; }
    public int AvailableBalance { get; set; }
    public IReadOnlyList<LoyaltyTransactionDto> TransactionHistory { get; set; } = [];
}

public sealed class LoyaltyTransactionDto
{
    public Guid Id { get; set; }
    public int Points { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
