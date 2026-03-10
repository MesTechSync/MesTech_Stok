using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateIncome;

public class CreateIncomeHandler : IRequestHandler<CreateIncomeCommand, Guid>
{
    private readonly IIncomeRepository _incomeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateIncomeHandler(IIncomeRepository incomeRepository, IUnitOfWork unitOfWork)
    {
        _incomeRepository = incomeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateIncomeCommand request, CancellationToken cancellationToken)
    {
        var income = new Income
        {
            TenantId = request.TenantId,
            StoreId = request.StoreId,
            Description = request.Description,
            Amount = request.Amount,
            IncomeType = request.IncomeType,
            InvoiceId = request.InvoiceId,
            Date = request.Date ?? DateTime.UtcNow,
            Note = request.Note,
        };

        await _incomeRepository.AddAsync(income);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return income.Id;
    }
}
