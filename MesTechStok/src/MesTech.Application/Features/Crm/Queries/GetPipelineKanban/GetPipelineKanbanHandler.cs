using MediatR;
using MesTech.Application.DTOs.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Queries.GetPipelineKanban;

public sealed class GetPipelineKanbanHandler : IRequestHandler<GetPipelineKanbanQuery, KanbanBoardDto>
{
    private readonly ICrmDealRepository _dealRepo;
    private readonly IPipelineRepository _pipelineRepo;

    public GetPipelineKanbanHandler(ICrmDealRepository dealRepo, IPipelineRepository pipelineRepo)
        => (_dealRepo, _pipelineRepo) = (dealRepo, pipelineRepo);

    public async Task<KanbanBoardDto> Handle(GetPipelineKanbanQuery req, CancellationToken cancellationToken)
    {
        var pipeline = await _pipelineRepo.GetByIdWithStagesAsync(req.PipelineId, cancellationToken)
            ?? throw new InvalidOperationException($"Pipeline {req.PipelineId} not found.");

        var deals = await _dealRepo.GetByPipelineAsync(req.TenantId, req.PipelineId, DealStatus.Open, cancellationToken).ConfigureAwait(false);

        return new KanbanBoardDto
        {
            PipelineId = pipeline.Id,
            PipelineName = pipeline.Name,
            Stages = pipeline.Stages
                .OrderBy(s => s.Position)
                .Select(s => new KanbanStageDto
                {
                    StageId = s.Id, Name = s.Name, Color = s.Color,
                    Position = s.Position, Probability = s.Probability ?? 0,
                    Deals = deals.Where(d => d.StageId == s.Id)
                        .Select(d => new DealDto
                        {
                            Id = d.Id, Title = d.Title, Amount = d.Amount,
                            StageId = s.Id, StageName = s.Name,
                            ContactName = d.Contact?.FullName
                        }).ToList().AsReadOnly()
                }).ToList().AsReadOnly()
        };
    }
}
