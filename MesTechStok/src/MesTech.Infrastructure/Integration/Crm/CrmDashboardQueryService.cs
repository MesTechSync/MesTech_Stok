using MesTech.Application.DTOs.Crm;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Integration.Crm;

/// <summary>
/// CRM dashboard icin cross-entity sorgulama servisi.
/// EF Core ile implement edilir — Lead, Contact, Deal, PlatformMessage vb. tabloları birlestirerek dashboard verileri uretir.
/// </summary>
public sealed class CrmDashboardQueryService : ICrmDashboardQueryService
{
    private readonly AppDbContext _dbContext;

    public CrmDashboardQueryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CrmDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken ct = default)
    {
        var totalLeads = await _dbContext.Leads
            .CountAsync(l => l.TenantId == tenantId, ct);

        var openDeals = await _dbContext.Deals
            .CountAsync(d => d.TenantId == tenantId && !d.IsDeleted, ct);

        var pipelineValue = await _dbContext.Deals
            .Where(d => d.TenantId == tenantId && !d.IsDeleted)
            .SumAsync(d => d.Amount, ct);

        var totalMessages = await _dbContext.PlatformMessages
            .CountAsync(m => m.TenantId == tenantId, ct);

        var unreadMessages = await _dbContext.PlatformMessages
            .CountAsync(m => m.TenantId == tenantId && m.Status == Domain.Enums.MessageStatus.Unread, ct);

        var totalContacts = await _dbContext.CrmContacts
            .CountAsync(c => c.TenantId == tenantId, ct);

        return new CrmDashboardDto
        {
            TotalCustomers = totalContacts,
            ActiveCustomers = totalContacts,
            VipCustomers = 0,
            TotalSuppliers = 0,
            TotalLeads = totalLeads,
            OpenDeals = openDeals,
            PipelineValue = pipelineValue,
            UnreadMessages = unreadMessages,
            TotalMessages = totalMessages,
            StageSummaries = [],
            RecentActivities = []
        };
    }

    public async Task<(IReadOnlyList<CustomerCrmDto> Items, int TotalCount)> GetCustomersPagedAsync(
        Guid tenantId, bool? isVip, bool? isActive, string? searchTerm,
        int page, int pageSize, CancellationToken ct = default)
    {
        // Stub: CRM contacts mapped as customers
        var query = _dbContext.CrmContacts
            .Where(c => c.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(c =>
                c.FullName.ToLower().Contains(term) ||
                (c.Email != null && c.Email.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(ct);

        var contacts = await query
            .OrderBy(c => c.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerCrmDto
            {
                Id = c.Id,
                Name = c.FullName,
                Email = c.Email ?? string.Empty
            })
            .ToListAsync(ct);

        return (contacts, totalCount);
    }

    public Task<(IReadOnlyList<SupplierCrmDto> Items, int TotalCount)> GetSuppliersPagedAsync(
        Guid tenantId, bool? isActive, bool? isPreferred, string? searchTerm,
        int page, int pageSize, CancellationToken ct = default)
    {
        // Stub: empty supplier list — full implementation in future wave
        IReadOnlyList<SupplierCrmDto> empty = [];
        return Task.FromResult((empty, 0));
    }
}
