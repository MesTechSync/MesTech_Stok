using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface ICargoExpenseRepository
{
    Task<IReadOnlyList<CargoExpense>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<decimal> GetTotalCostAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(CargoExpense expense, CancellationToken ct = default);
}
