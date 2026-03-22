using MediatR;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Queries.GetCustomerPoints;

public class GetCustomerPointsHandler : IRequestHandler<GetCustomerPointsQuery, GetCustomerPointsResult>
{
    private readonly ILoyaltyTransactionRepository _transactionRepo;

    public GetCustomerPointsHandler(ILoyaltyTransactionRepository transactionRepo)
        => _transactionRepo = transactionRepo;

    public async Task<GetCustomerPointsResult> Handle(
        GetCustomerPointsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Aggregate point sums by type
        var totalEarned = await _transactionRepo.GetPointsSumByTypeAsync(
            request.TenantId, request.CustomerId, LoyaltyTransactionType.Earn, cancellationToken);

        var totalRedeemed = await _transactionRepo.GetPointsSumByTypeAsync(
            request.TenantId, request.CustomerId, LoyaltyTransactionType.Redeem, cancellationToken);

        var totalExpired = await _transactionRepo.GetPointsSumByTypeAsync(
            request.TenantId, request.CustomerId, LoyaltyTransactionType.Expire, cancellationToken);

        var availableBalance = totalEarned - Math.Abs(totalRedeemed) - Math.Abs(totalExpired);

        // Last 20 transactions
        var recentTransactions = await _transactionRepo.GetByCustomerPagedAsync(
            request.TenantId, request.CustomerId, 20, cancellationToken);

        return new GetCustomerPointsResult
        {
            TotalEarned = totalEarned,
            TotalRedeemed = Math.Abs(totalRedeemed),
            TotalExpired = Math.Abs(totalExpired),
            AvailableBalance = availableBalance,
            TransactionHistory = recentTransactions.Select(t => new LoyaltyTransactionDto
            {
                Id = t.Id,
                Points = t.Points,
                Type = t.Type,
                Description = t.Description,
                CreatedAt = t.CreatedAt
            }).ToList().AsReadOnly()
        };
    }
}
