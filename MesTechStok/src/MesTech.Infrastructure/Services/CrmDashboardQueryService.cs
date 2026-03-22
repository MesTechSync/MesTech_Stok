using MesTech.Application.DTOs.Crm;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Services;

public class CrmDashboardQueryService : ICrmDashboardQueryService
{
    private readonly AppDbContext _db;

    public CrmDashboardQueryService(AppDbContext db)
        => _db = db ?? throw new ArgumentNullException(nameof(db));

    public async Task<CrmDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken ct = default)
    {
        var dto = new CrmDashboardDto
        {
            TotalCustomers = await _db.Customers
                .CountAsync(c => c.TenantId == tenantId && !c.IsDeleted, ct),
            ActiveCustomers = await _db.Customers
                .CountAsync(c => c.TenantId == tenantId && c.IsActive && !c.IsDeleted, ct),
            VipCustomers = await _db.Customers
                .CountAsync(c => c.TenantId == tenantId && c.IsVip && !c.IsDeleted, ct),
            TotalSuppliers = await _db.Suppliers
                .CountAsync(s => s.TenantId == tenantId && !s.IsDeleted, ct),
            TotalLeads = await _db.Leads
                .CountAsync(l => l.TenantId == tenantId && !l.IsDeleted, ct),
            OpenDeals = await _db.Deals
                .CountAsync(d => d.TenantId == tenantId && d.Status == DealStatus.Open && !d.IsDeleted, ct),
            PipelineValue = await _db.Deals
                .Where(d => d.TenantId == tenantId && d.Status == DealStatus.Open && !d.IsDeleted)
                .SumAsync(d => d.Amount, ct),
            UnreadMessages = await _db.PlatformMessages
                .CountAsync(m => m.TenantId == tenantId && m.Status == MessageStatus.Unread && !m.IsDeleted, ct),
            TotalMessages = await _db.PlatformMessages
                .CountAsync(m => m.TenantId == tenantId && !m.IsDeleted, ct),
        };

        dto.StageSummaries = await _db.PipelineStages
            .Where(s => !s.IsDeleted)
            .Select(s => new StageSummaryDto
            {
                StageId = s.Id,
                StageName = s.Name,
                StageColor = s.Color,
                DealCount = _db.Deals.Count(d => d.StageId == s.Id && d.TenantId == tenantId && !d.IsDeleted),
                TotalValue = _db.Deals
                    .Where(d => d.StageId == s.Id && d.TenantId == tenantId && !d.IsDeleted)
                    .Sum(d => d.Amount)
            })
            .AsNoTracking().ToListAsync(ct);

        dto.RecentActivities = await _db.Activities
            .Where(a => a.TenantId == tenantId && !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new RecentActivityDto
            {
                Id = a.Id,
                Type = a.Type.ToString(),
                Subject = a.Subject,
                OccurredAt = a.CreatedAt
            })
            .AsNoTracking().ToListAsync(ct);

        return dto;
    }

    public async Task<(IReadOnlyList<CustomerCrmDto> Items, int TotalCount)> GetCustomersPagedAsync(
        Guid tenantId, bool? isVip, bool? isActive, string? searchTerm,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Customers.Where(c => c.TenantId == tenantId && !c.IsDeleted);

        if (isVip.HasValue) query = query.Where(c => c.IsVip == isVip.Value);
        if (isActive.HasValue) query = query.Where(c => c.IsActive == isActive.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{searchTerm}%")
                                  || (c.Email != null && EF.Functions.ILike(c.Email, $"%{searchTerm}%")));

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CustomerCrmDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Email = c.Email,
                Phone = c.Phone,
                IsVip = c.IsVip,
                IsActive = c.IsActive,
                CurrentBalance = c.CurrentBalance,
                LastOrderDate = c.LastOrderDate
            })
            .AsNoTracking().ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<SupplierCrmDto> Items, int TotalCount)> GetSuppliersPagedAsync(
        Guid tenantId, bool? isActive, bool? isPreferred, string? searchTerm,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Suppliers.Where(s => s.TenantId == tenantId && !s.IsDeleted);

        if (isActive.HasValue) query = query.Where(s => s.IsActive == isActive.Value);
        if (isPreferred.HasValue) query = query.Where(s => s.IsPreferred == isPreferred.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(s => EF.Functions.ILike(s.Name, $"%{searchTerm}%"));

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => new SupplierCrmDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Email = s.Email,
                Phone = s.Phone,
                IsActive = s.IsActive,
                IsPreferred = s.IsPreferred,
                CurrentBalance = s.CurrentBalance
            })
            .AsNoTracking().ToListAsync(ct);

        return (items, totalCount);
    }
}
