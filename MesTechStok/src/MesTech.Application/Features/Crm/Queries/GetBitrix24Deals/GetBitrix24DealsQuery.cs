using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Crm.Queries.GetBitrix24Deals;

public record GetBitrix24DealsQuery(Guid TenantId, Guid? StageId = null, int Page = 1, int PageSize = 50)
    : IRequest<Bitrix24DealsResult>, ICacheableQuery
{
    public string CacheKey => $"B24Deals_{TenantId}_{StageId}_{Page}_{PageSize}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(1);
}

public sealed class Bitrix24DealsResult
{
    public IReadOnlyList<DealCardDto> Deals { get; init; } = [];
    public int TotalCount { get; init; }
}

public sealed class DealCardDto
{
    public Guid DealId { get; init; }
    public string Title { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public string Currency { get; init; } = "TRY";
    public string? ContactName { get; init; }
    public Guid StageId { get; init; }
    public string StageName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime? ExpectedCloseDate { get; init; }
    public DateTime CreatedAt { get; init; }
}
