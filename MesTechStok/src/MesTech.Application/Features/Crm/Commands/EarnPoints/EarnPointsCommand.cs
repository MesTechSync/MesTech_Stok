using MediatR;

namespace MesTech.Application.Features.Crm.Commands.EarnPoints;

public record EarnPointsCommand(
    Guid TenantId,
    Guid CustomerId,
    Guid OrderId,
    decimal OrderAmount
) : IRequest<EarnPointsResult>;

public class EarnPointsResult
{
    public int EarnedPoints { get; set; }
    public Guid TransactionId { get; set; }
}
