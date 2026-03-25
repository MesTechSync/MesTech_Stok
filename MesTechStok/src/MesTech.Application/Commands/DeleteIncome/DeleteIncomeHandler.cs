using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.DeleteIncome;

public sealed class DeleteIncomeHandler : IRequestHandler<DeleteIncomeCommand>
{
    private readonly IIncomeRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteIncomeHandler(IIncomeRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(DeleteIncomeCommand request, CancellationToken cancellationToken)
    {
        var income = await _repository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Income {request.Id} not found.");

        income.IsDeleted = true;
        income.DeletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(income);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
