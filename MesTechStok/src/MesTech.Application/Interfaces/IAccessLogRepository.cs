using MesTech.Domain.Entities;

namespace MesTech.Application.Interfaces;

public interface IAccessLogRepository
{
    Task<IReadOnlyList<AccessLog>> GetPagedAsync(
        Guid tenantId,
        DateTime? from,
        DateTime? to,
        Guid? userId,
        string? action,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task AddAsync(AccessLog log, CancellationToken ct = default);
}
