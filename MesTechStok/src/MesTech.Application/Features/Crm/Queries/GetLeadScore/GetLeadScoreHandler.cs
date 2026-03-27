using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Crm.Queries.GetLeadScore;

public sealed class GetLeadScoreHandler
    : IRequestHandler<GetLeadScoreQuery, LeadScoreResult>
{
    private readonly ILeadRepository _leadRepo;

    public GetLeadScoreHandler(ILeadRepository leadRepo)
        => _leadRepo = leadRepo ?? throw new ArgumentNullException(nameof(leadRepo));

    public async Task<LeadScoreResult> Handle(
        GetLeadScoreQuery request, CancellationToken cancellationToken)
    {
        var lead = await _leadRepo.GetByIdAsync(request.LeadId, cancellationToken)
            .ConfigureAwait(false);

        if (lead is null)
            return new LeadScoreResult { LeadId = request.LeadId, Score = 0, ScoreLabel = "Not Found" };

        var score = lead.Score ?? 0;
        var label = score switch
        {
            >= 80 => "Hot",
            >= 50 => "Warm",
            >= 20 => "Cold",
            _ => "Unqualified"
        };

        return new LeadScoreResult
        {
            LeadId = lead.Id,
            Score = score,
            ScoreLabel = label,
            Reasoning = lead.ScoreReasoning
        };
    }
}
