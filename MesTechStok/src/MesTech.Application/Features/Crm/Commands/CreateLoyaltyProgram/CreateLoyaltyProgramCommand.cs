using MediatR;
namespace MesTech.Application.Features.Crm.Commands.CreateLoyaltyProgram;
public record CreateLoyaltyProgramCommand(Guid TenantId, string Name, decimal PointsPerPurchase, int MinRedeemPoints) : IRequest<Guid>;
