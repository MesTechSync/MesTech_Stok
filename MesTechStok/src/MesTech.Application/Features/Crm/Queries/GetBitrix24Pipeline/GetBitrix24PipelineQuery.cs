using MesTech.Application.Behaviors;
using MediatR;

namespace MesTech.Application.Features.Crm.Queries.GetBitrix24Pipeline;

public record GetBitrix24PipelineQuery(Guid TenantId, string? StageFilter = null)
    : IRequest<Bitrix24PipelineResult>, ICacheableQuery
{
    public string CacheKey => $"B24Pipeline_{TenantId}_{StageFilter}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}

public sealed class Bitrix24PipelineResult
{
    public IReadOnlyList<PipelineStageDto> Stages { get; init; } = [];
    public int TotalDeals { get; init; }
    public decimal TotalValue { get; init; }
}

public sealed class PipelineStageDto
{
    public string StageId { get; init; } = string.Empty;
    public string StageName { get; init; } = string.Empty;
    public int DealCount { get; init; }
    public decimal TotalValue { get; init; }
}
