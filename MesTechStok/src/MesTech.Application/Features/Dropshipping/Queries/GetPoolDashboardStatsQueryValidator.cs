using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Queries;

public sealed class GetPoolDashboardStatsQueryValidator : AbstractValidator<GetPoolDashboardStatsQuery>
{
    public GetPoolDashboardStatsQueryValidator()
    {
        // Parameterless query — no validation rules required.
        // Validator exists to satisfy the FluentValidation pipeline convention.
    }
}
