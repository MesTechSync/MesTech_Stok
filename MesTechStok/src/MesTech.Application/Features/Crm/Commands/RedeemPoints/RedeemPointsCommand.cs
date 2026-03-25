using MediatR;

namespace MesTech.Application.Features.Crm.Commands.RedeemPoints;

public record RedeemPointsCommand(
    Guid TenantId,
    Guid CustomerId,
    int PointsToRedeem
) : IRequest<RedeemPointsResult>;

public sealed class RedeemPointsResult
{
    public int RedeemedPoints { get; set; }
    public decimal DiscountAmount { get; set; }
    public int RemainingBalance { get; set; }
    public Guid TransactionId { get; set; }
}
