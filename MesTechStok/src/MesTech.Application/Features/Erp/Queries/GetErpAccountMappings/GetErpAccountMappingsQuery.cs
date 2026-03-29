using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Erp.Queries.GetErpAccountMappings;

/// <summary>
/// ERP hesap eslestirmeleri sorgusu.
/// G420: Avalonia ErpAccountMappingView icin backend query.
/// </summary>
public record GetErpAccountMappingsQuery(Guid TenantId)
    : IRequest<IReadOnlyList<ErpAccountMappingDto>>, ICacheableQuery
{
    public string CacheKey => $"ErpAccountMappings_{TenantId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public record ErpAccountMappingDto
{
    public Guid Id { get; init; }
    public string MesTechAccountCode { get; init; } = string.Empty;
    public string MesTechAccountName { get; init; } = string.Empty;
    public string MesTechAccountType { get; init; } = string.Empty;
    public string ErpAccountCode { get; init; } = string.Empty;
    public string ErpAccountName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? LastSyncAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
