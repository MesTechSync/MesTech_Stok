using MediatR;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Commands.EarnPoints;

public class EarnPointsHandler : IRequestHandler<EarnPointsCommand, EarnPointsResult>
{
    private readonly ILoyaltyProgramRepository _programRepo;
    private readonly ILoyaltyTransactionRepository _transactionRepo;
    private readonly IUnitOfWork _uow;

    public EarnPointsHandler(
        ILoyaltyProgramRepository programRepo,
        ILoyaltyTransactionRepository transactionRepo,
        IUnitOfWork uow)
    {
        _programRepo = programRepo;
        _transactionRepo = transactionRepo;
        _uow = uow;
    }

    public async Task<EarnPointsResult> Handle(EarnPointsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Load active LoyaltyProgram for tenant
        var program = await _programRepo.GetActiveByTenantAsync(request.TenantId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"No active loyalty program found for tenant {request.TenantId}");

        // 2. Calculate points: OrderAmount * PointsPerPurchase (round down)
        var earnedPoints = (int)Math.Floor(request.OrderAmount * program.PointsPerPurchase);

        if (earnedPoints <= 0)
        {
            return new EarnPointsResult { EarnedPoints = 0, TransactionId = null };
        }

        // 3. Create LoyaltyTransaction(type: Earn)
        var transaction = LoyaltyTransaction.Create(
            request.TenantId,
            request.CustomerId,
            program.Id,
            earnedPoints,
            LoyaltyTransactionType.Earn,
            $"Order {request.OrderId} — {request.OrderAmount:N2} TL");

        await _transactionRepo.AddAsync(transaction, cancellationToken);

        // 4. Save and return earned points
        await _uow.SaveChangesAsync(cancellationToken);

        return new EarnPointsResult
        {
            EarnedPoints = earnedPoints,
            TransactionId = transaction.Id
        };
    }
}
