using Mapster;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetExpenseById;

public class GetExpenseByIdHandler : IRequestHandler<GetExpenseByIdQuery, ExpenseDto?>
{
    private readonly IExpenseRepository _repository;

    public GetExpenseByIdHandler(IExpenseRepository repository)
        => _repository = repository;

    public async Task<ExpenseDto?> Handle(GetExpenseByIdQuery request, CancellationToken cancellationToken)
    {
        var expense = await _repository.GetByIdAsync(request.Id);
        return expense?.Adapt<ExpenseDto>();
    }
}
