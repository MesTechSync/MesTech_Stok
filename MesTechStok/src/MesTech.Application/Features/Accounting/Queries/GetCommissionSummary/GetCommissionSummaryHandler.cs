using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Enums;

namespace MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;

public sealed class GetCommissionSummaryHandler : IRequestHandler<GetCommissionSummaryQuery, CommissionSummaryDto>
{
    private readonly ICommissionRecordRepository _repository;

    public GetCommissionSummaryHandler(ICommissionRecordRepository repository)
        => _repository = repository;

    public async Task<CommissionSummaryDto> Handle(GetCommissionSummaryQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Get records for all platforms in the period
        var platforms = Enum.GetValues<PlatformType>().Select(p => p.ToString()).ToArray();
        var byPlatform = new List<PlatformCommissionDto>();
        decimal totalCommission = 0;
        decimal totalServiceFee = 0;

        foreach (var platform in platforms)
        {
            var records = await _repository.GetByPlatformAsync(request.TenantId, platform, request.From, request.To, cancellationToken).ConfigureAwait(false);
            if (records.Count == 0) continue;

            var platformTotal = records.Sum(r => r.CommissionAmount);
            var platformServiceFee = records.Sum(r => r.ServiceFee);
            var platformGross = records.Sum(r => r.GrossAmount);
            var avgRate = platformGross > 0 ? platformTotal / platformGross : 0;

            byPlatform.Add(new PlatformCommissionDto
            {
                Platform = platform,
                TotalGross = platformGross,
                TotalCommission = platformTotal,
                AverageRate = Math.Round(avgRate * 100, 2),
                RecordCount = records.Count
            });

            totalCommission += platformTotal;
            totalServiceFee += platformServiceFee;
        }

        return new CommissionSummaryDto
        {
            TotalCommission = totalCommission,
            TotalServiceFee = totalServiceFee,
            ByPlatform = byPlatform
        };
    }
}
