using MediatR;
using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
namespace MesTech.Application.Features.Crm.Commands.CreateLoyaltyProgram;
public class CreateLoyaltyProgramHandler : IRequestHandler<CreateLoyaltyProgramCommand, Guid>
{
    private readonly ILoyaltyRepository _repo;
    public CreateLoyaltyProgramHandler(ILoyaltyRepository repo) => _repo = repo;
    public async Task<Guid> Handle(CreateLoyaltyProgramCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var program = LoyaltyProgram.Create(request.TenantId, request.Name, request.PointsPerPurchase, request.MinRedeemPoints);
        await _repo.AddAsync(program, cancellationToken);
        return program.Id;
    }
}
