using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateExpense;

public sealed class CreateExpenseHandler : IRequestHandler<CreateExpenseCommand, Guid>
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateExpenseHandler(IExpenseRepository expenseRepository, IUnitOfWork unitOfWork)
    {
        _expenseRepository = expenseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var expense = new Expense
        {
            TenantId = request.TenantId,
            StoreId = request.StoreId,
            Description = request.Description,
            ExpenseType = request.ExpenseType,
            Date = request.Date ?? DateTime.UtcNow,
            Note = request.Note,
            IsRecurring = request.IsRecurring,
            RecurrencePeriod = request.RecurrencePeriod,
        };
        expense.SetAmount(request.Amount);

        await _expenseRepository.AddAsync(expense);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return expense.Id;
    }
}
