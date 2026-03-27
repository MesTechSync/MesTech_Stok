using MediatR;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Crm.Queries.GetBitrix24Pipeline;

public sealed class GetBitrix24PipelineHandler
    : IRequestHandler<GetBitrix24PipelineQuery, Bitrix24PipelineResult>
{
    private readonly IPipelineRepository _pipelineRepo;
    private readonly ILogger<GetBitrix24PipelineHandler> _logger;

    public GetBitrix24PipelineHandler(IPipelineRepository pipelineRepo, ILogger<GetBitrix24PipelineHandler> logger)
    {
        _pipelineRepo = pipelineRepo;
        _logger = logger;
    }

    public async Task<Bitrix24PipelineResult> Handle(
        GetBitrix24PipelineQuery request, CancellationToken cancellationToken)
    {
        var pipelines = await _pipelineRepo.GetByTenantAsync(request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        var stages = pipelines
            .SelectMany(p => p.Stages)
            .GroupBy(s => new { s.Id, s.Name })
            .Select(g => new PipelineStageDto
            {
                StageId = g.Key.Id.ToString(),
                StageName = g.Key.Name,
                DealCount = g.Count(),
                TotalValue = 0m
            })
            .ToList();

        return new Bitrix24PipelineResult
        {
            Stages = stages,
            TotalDeals = stages.Sum(s => s.DealCount),
            TotalValue = stages.Sum(s => s.TotalValue)
        };
    }
}
