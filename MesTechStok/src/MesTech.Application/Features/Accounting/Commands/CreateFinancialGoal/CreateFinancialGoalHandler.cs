using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateFinancialGoal;

public class CreateFinancialGoalHandler : IRequestHandler<CreateFinancialGoalCommand, Guid>
{
    private readonly IFinancialGoalRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateFinancialGoalHandler(IFinancialGoalRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateFinancialGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = FinancialGoal.Create(
            request.TenantId, request.Title, request.TargetAmount, request.StartDate, request.EndDate);

        await _repository.AddAsync(goal, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return goal.Id;
    }
}
