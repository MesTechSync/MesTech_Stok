using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface IVatDeclarationRepository
{
    Task<VatDeclaration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<VatDeclaration?> GetByPeriodAsync(Guid tenantId, int year, int month, CancellationToken ct = default);
    Task<IReadOnlyList<VatDeclaration>> GetByYearAsync(Guid tenantId, int year, CancellationToken ct = default);
    Task AddAsync(VatDeclaration declaration, CancellationToken ct = default);
    Task UpdateAsync(VatDeclaration declaration, CancellationToken ct = default);
}
