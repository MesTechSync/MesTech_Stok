using MediatR;

namespace MesTech.Application.Features.System.LaunchReadiness;

/// <summary>
/// Canliya cikis hazirlik raporu sorgusu.
/// 26 kriter kontrol eder ve gecme durumunu raporlar.
/// </summary>
public record GetLaunchReadinessQuery(Guid TenantId) : IRequest<LaunchReadinessDto>;

public record LaunchReadinessDto
{
    public int TotalCriteria { get; init; } = 26;
    public int PassedCriteria { get; init; }
    public int FailedCriteria => TotalCriteria - PassedCriteria;
    public int Percentage => TotalCriteria > 0 ? PassedCriteria * 100 / TotalCriteria : 0;
    public string Status => Percentage switch
    {
        >= 90 => "READY",
        >= 70 => "CLOSE",
        >= 40 => "IN_PROGRESS",
        _ => "EARLY"
    };
    public IReadOnlyList<LaunchCriterionDto> Criteria { get; init; } = [];
}

public record LaunchCriterionDto(
    int Number,
    string Name,
    string Category,
    bool Passed,
    string? Detail);
