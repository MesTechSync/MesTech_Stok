using MediatR;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Commands.RedeemPoints;

public sealed class RedeemPointsHandler : IRequestHandler<RedeemPointsCommand, RedeemPointsResult>
{
    private readonly ILoyaltyProgramRepository _programRepo;
    private readonly ILoyaltyTransactionRepository _transactionRepo;
    private readonly IUnitOfWork _uow;

    public RedeemPointsHandler(
        ILoyaltyProgramRepository programRepo,
        ILoyaltyTransactionRepository transactionRepo,
        IUnitOfWork uow)
    {
        _programRepo = programRepo;
        _transactionRepo = transactionRepo;
        _uow = uow;
    }

    public async Task<RedeemPointsResult> Handle(RedeemPointsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Load LoyaltyProgram, check MinRedeemPoints threshold
        var program = await _programRepo.GetActiveByTenantAsync(request.TenantId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No active loyalty program found for tenant {request.TenantId}");

        if (request.PointsToRedeem < program.MinRedeemPoints)
        {
            throw new InvalidOperationException(
                $"Minimum redeem threshold is {program.MinRedeemPoints} points. Requested: {request.PointsToRedeem}");
        }

        // 2. Calculate customer's available balance (sum Earn - sum Redeem - sum Expired)
        var totalEarned = await _transactionRepo.GetPointsSumByTypeAsync(
            request.TenantId, request.CustomerId, LoyaltyTransactionType.Earn, cancellationToken);

        var totalRedeemed = await _transactionRepo.GetPointsSumByTypeAsync(
            request.TenantId, request.CustomerId, LoyaltyTransactionType.Redeem, cancellationToken);

        var totalExpired = await _transactionRepo.GetPointsSumByTypeAsync(
            request.TenantId, request.CustomerId, LoyaltyTransactionType.Expire, cancellationToken);

        var availableBalance = totalEarned - Math.Abs(totalRedeemed) - Math.Abs(totalExpired);

        // 3. Validate: balance >= PointsToRedeem
        if (availableBalance < request.PointsToRedeem)
        {
            throw new InvalidOperationException(
                $"Insufficient points. Available: {availableBalance}, Requested: {request.PointsToRedeem}");
        }

        // 4. Create LoyaltyTransaction(type: Redeem, points: -PointsToRedeem)
        var transaction = LoyaltyTransaction.Create(
            request.TenantId,
            request.CustomerId,
            program.Id,
            -request.PointsToRedeem,
            LoyaltyTransactionType.Redeem,
            $"Redeemed {request.PointsToRedeem} points");

        await _transactionRepo.AddAsync(transaction, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // 5. Calculate discount: PointsToRedeem / 100 = TL discount
        var discountAmount = request.PointsToRedeem / 100m;
        var remainingBalance = availableBalance - request.PointsToRedeem;

        // 6. Return result
        return new RedeemPointsResult
        {
            RedeemedPoints = request.PointsToRedeem,
            DiscountAmount = discountAmount,
            RemainingBalance = remainingBalance,
            TransactionId = transaction.Id
        };
    }
}
