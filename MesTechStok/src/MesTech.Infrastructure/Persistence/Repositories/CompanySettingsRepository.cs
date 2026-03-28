using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class CompanySettingsRepository : ICompanySettingsRepository
{
    private readonly AppDbContext _context;

    public CompanySettingsRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CompanySettings?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.CompanySettings
            .AsNoTracking().FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

    public async Task AddAsync(CompanySettings settings, CancellationToken ct = default)
        => await _context.CompanySettings.AddAsync(settings, ct);

    public Task UpdateAsync(CompanySettings settings, CancellationToken ct = default)
    {
        _context.CompanySettings.Update(settings);
        return Task.CompletedTask;
    }
}
