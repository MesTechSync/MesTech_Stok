using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.CreateIncome;

public sealed class CreateIncomeHandler : IRequestHandler<CreateIncomeCommand, Guid>
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
        ArgumentNullException.ThrowIfNull(request);
        var income = new Income
        {
            TenantId = request.TenantId,
            StoreId = request.StoreId,
            Description = request.Description,
            IncomeType = request.IncomeType,
            InvoiceId = request.InvoiceId,
            Date = request.Date ?? DateTime.UtcNow,
            Note = request.Note,
        };
        income.SetAmount(request.Amount);

        await _incomeRepository.AddAsync(income).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return income.Id;
    }
}
