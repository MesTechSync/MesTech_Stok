using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.UpdateIncome;

public sealed class UpdateIncomeHandler : IRequestHandler<UpdateIncomeCommand>
{
    private readonly IIncomeRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateIncomeHandler(IIncomeRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(UpdateIncomeCommand request, CancellationToken cancellationToken)
    {
        var income = await _repository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Income {request.Id} not found.");

        if (request.Description is not null) income.Description = request.Description;
        if (request.Amount.HasValue) income.SetAmount(request.Amount.Value);
        if (request.IncomeType.HasValue) income.IncomeType = request.IncomeType.Value;
        if (request.Note is not null) income.Note = request.Note;

        await _repository.UpdateAsync(income);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
