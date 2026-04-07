using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public sealed class VatDeclarationRepository : IVatDeclarationRepository
{
    private readonly AppDbContext _context;
    public VatDeclarationRepository(AppDbContext context) => _context = context;

    public async Task<VatDeclaration?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.VatDeclarations
            .FirstOrDefaultAsync(v => v.Id == id, ct).ConfigureAwait(false);

    public async Task<VatDeclaration?> GetByPeriodAsync(Guid tenantId, int year, int month, CancellationToken ct = default)
        => await _context.VatDeclarations
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.Year == year && v.Month == month, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<VatDeclaration>> GetByYearAsync(Guid tenantId, int year, CancellationToken ct = default)
        => await _context.VatDeclarations
            .Where(v => v.TenantId == tenantId && v.Year == year)
            .OrderBy(v => v.Month)
            .Take(12)
            .AsNoTracking().ToListAsync(ct).ConfigureAwait(false);

    public async Task AddAsync(VatDeclaration declaration, CancellationToken ct = default)
        => await _context.VatDeclarations.AddAsync(declaration, ct).ConfigureAwait(false);

    public Task UpdateAsync(VatDeclaration declaration, CancellationToken ct = default)
    {
        _context.VatDeclarations.Update(declaration);
        return Task.CompletedTask;
    }
}
